using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

/// <summary>
/// Alimenta el catalogo de reparticiones y sectores con cada historial de pases: GDEBA informa codigo y descripcion de origen y destino
/// en cada pase, asi que el catalogo se completa solo, sin llamadas adicionales, a medida que se consultan expedientes.
/// </summary>
public sealed class CatalogoReparticionesGdebaService : ICatalogoReparticionesGdebaService
{
    private readonly ITrackableRepository<ReparticionGdeba> _reparticionRepository;
    private readonly ITrackableRepository<SectorGdeba> _sectorRepository;

    public CatalogoReparticionesGdebaService(ITrackableRepository<ReparticionGdeba> reparticionRepository, ITrackableRepository<SectorGdeba> sectorRepository)
    {
        _reparticionRepository = reparticionRepository;
        _sectorRepository = sectorRepository;
    }

    public async Task RegistrarDesdeMovimientosAsync(IReadOnlyCollection<GdebaMovimientoExpedienteDto> movimientos, CancellationToken cancellationToken)
    {
        // Un mismo codigo aparece en muchos pases del historial: se consolida por codigo y la ultima descripcion informada manda.
        Dictionary<string, string?> reparticiones = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<(string Reparticion, string Sector), string?> sectores = new();
        foreach (GdebaMovimientoExpedienteDto movimiento in movimientos)
        {
            CatalogoReparticionesGdebaService.Acumular(reparticiones, movimiento.ReparticionOrigen, movimiento.ReparticionOrigenDescripcion);
            CatalogoReparticionesGdebaService.Acumular(reparticiones, movimiento.ReparticionDestino, movimiento.ReparticionDestinoDescripcion);
            CatalogoReparticionesGdebaService.Acumular(sectores, movimiento.ReparticionOrigen, movimiento.SectorOrigen, movimiento.SectorOrigenDescripcion);
            CatalogoReparticionesGdebaService.Acumular(sectores, movimiento.ReparticionDestino, movimiento.SectorDestino, movimiento.SectorDestinoDescripcion);
        }

        if (reparticiones.Count > 0)
        {
            await this.RegistrarReparticionesAsync(reparticiones, cancellationToken);
        }

        if (sectores.Count > 0)
        {
            await this.RegistrarSectoresAsync(sectores, cancellationToken);
        }
    }

    public async Task<CabeceraExpedienteDto> CompletarDescripcionesAsync(CabeceraExpedienteDto cabecera, CancellationToken cancellationToken)
    {
        string? reparticionCaratuladora = CatalogoReparticionesGdebaService.ReparticionDelNumero(cabecera.NumeroGdebaCompleto);
        string? reparticionActual = string.IsNullOrWhiteSpace(cabecera.ReparticionActual) ? null : cabecera.ReparticionActual.Trim().ToUpperInvariant();
        string[] codigos = new[] { reparticionCaratuladora, reparticionActual }.Where(x => x is not null).Distinct()!.ToArray()!;
        Dictionary<string, string?> descripciones = codigos.Length == 0
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : (await _reparticionRepository.Query().Where(x => codigos.Contains(x.Codigo)).SelectAsync(cancellationToken))
                .ToDictionary(x => x.Codigo, x => x.Descripcion, StringComparer.OrdinalIgnoreCase);

        string? sectorDescripcion = null;
        if (reparticionActual is not null && !string.IsNullOrWhiteSpace(cabecera.SectorDestino))
        {
            string sector = cabecera.SectorDestino.Trim().ToUpperInvariant();
            sectorDescripcion = (await _sectorRepository.Query()
                    .Where(x => x.CodigoReparticion == reparticionActual && x.Codigo == sector)
                    .SelectAsync(cancellationToken))
                .FirstOrDefault()?.Descripcion;
        }

        return cabecera with
        {
            ReparticionCaratuladora = reparticionCaratuladora,
            ReparticionCaratuladoraDescripcion = reparticionCaratuladora is not null && descripciones.TryGetValue(reparticionCaratuladora, out string? caratuladora) ? caratuladora : null,
            ReparticionActualDescripcion = reparticionActual is not null && descripciones.TryGetValue(reparticionActual, out string? actual) ? actual : null,
            SectorDestinoDescripcion = sectorDescripcion
        };
    }

    // La reparticion caratuladora es la del numero GDEBA; un numero que no parsea no es un error de esta consulta.
    private static string? ReparticionDelNumero(string numeroGdebaCompleto)
    {
        try
        {
            return NumeroGdebaCompleto.Create(numeroGdebaCompleto).Reparticion;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private async Task RegistrarReparticionesAsync(Dictionary<string, string?> reparticiones, CancellationToken cancellationToken)
    {
        string[] codigos = reparticiones.Keys.ToArray();
        Dictionary<string, ReparticionGdeba> existentes = (await _reparticionRepository.Query()
                .Where(x => codigos.Contains(x.Codigo))
                .SelectAsync(cancellationToken))
            .ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);

        foreach ((string codigo, string? descripcion) in reparticiones)
        {
            if (existentes.TryGetValue(codigo, out ReparticionGdeba? existente))
            {
                // Solo se escribe si GDEBA informo una descripcion distinta; la fila de referencia no se toca en vano.
                if (existente.ActualizarDescripcion(descripcion))
                {
                    _reparticionRepository.Update(existente);
                    _reparticionRepository.ApplyChanges(existente);
                }

                continue;
            }

            _reparticionRepository.Insert(new ReparticionGdeba(codigo, descripcion));
        }
    }

    private async Task RegistrarSectoresAsync(Dictionary<(string Reparticion, string Sector), string?> sectores, CancellationToken cancellationToken)
    {
        string[] codigosReparticion = sectores.Keys.Select(x => x.Reparticion).Distinct().ToArray();
        Dictionary<(string, string), SectorGdeba> existentes = (await _sectorRepository.Query()
                .Where(x => codigosReparticion.Contains(x.CodigoReparticion))
                .SelectAsync(cancellationToken))
            .ToDictionary(x => (x.CodigoReparticion, x.Codigo));

        foreach (((string reparticion, string sector), string? descripcion) in sectores)
        {
            if (existentes.TryGetValue((reparticion, sector), out SectorGdeba? existente))
            {
                if (existente.ActualizarDescripcion(descripcion))
                {
                    _sectorRepository.Update(existente);
                    _sectorRepository.ApplyChanges(existente);
                }

                continue;
            }

            _sectorRepository.Insert(new SectorGdeba(reparticion, sector, descripcion));
        }
    }

    private static void Acumular(Dictionary<string, string?> reparticiones, string? codigo, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return;

        string clave = codigo.Trim().ToUpperInvariant();
        string? descripcionInformada = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        if (!reparticiones.TryGetValue(clave, out string? actual) || descripcionInformada is not null)
        {
            reparticiones[clave] = descripcionInformada ?? actual;
        }
    }

    private static void Acumular(Dictionary<(string Reparticion, string Sector), string?> sectores, string? codigoReparticion, string? codigoSector, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(codigoReparticion) || string.IsNullOrWhiteSpace(codigoSector)) return;

        (string, string) clave = (codigoReparticion.Trim().ToUpperInvariant(), codigoSector.Trim().ToUpperInvariant());
        string? descripcionInformada = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        if (!sectores.TryGetValue(clave, out string? actual) || descripcionInformada is not null)
        {
            sectores[clave] = descripcionInformada ?? actual;
        }
    }
}
