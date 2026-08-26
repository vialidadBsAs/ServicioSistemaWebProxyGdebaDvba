using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

public sealed class ExpedienteDetalladoWorkerService : IExpedienteDetalladoWorkerService
{
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTemaExpediente> _configuracionDescubrimientoTemaRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTrataExpediente> _configuracionDescubrimientoTrataRepository;
    private readonly IRepository<TemaExpedienteTrata> _temaExpedienteTrataRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataHabilitadaVialidadRepository;
    private readonly IExpedienteService _expedienteService;
    private readonly ILogger<ExpedienteDetalladoWorkerService> _logger;

    public ExpedienteDetalladoWorkerService(
        IRepository<Expediente> expedienteRepository,
        IRepository<ConfiguracionDescubrimientoTemaExpediente> configuracionDescubrimientoTemaRepository,
        IRepository<ConfiguracionDescubrimientoTrataExpediente> configuracionDescubrimientoTrataRepository,
        IRepository<TemaExpedienteTrata> temaExpedienteTrataRepository,
        IRepository<TrataHabilitadaVialidad> trataHabilitadaVialidadRepository,
        IExpedienteService expedienteService,
        ILogger<ExpedienteDetalladoWorkerService> logger)
    {
        _expedienteRepository = expedienteRepository;
        _configuracionDescubrimientoTemaRepository = configuracionDescubrimientoTemaRepository;
        _configuracionDescubrimientoTrataRepository = configuracionDescubrimientoTrataRepository;
        _temaExpedienteTrataRepository = temaExpedienteTrataRepository;
        _trataHabilitadaVialidadRepository = trataHabilitadaVialidadRepository;
        _expedienteService = expedienteService;
        _logger = logger;
    }

    public async Task<DetallarExpedientesPendientesResult> DetallarPendientesAsync(int tamanoLote, OrigenInvocacionGdeba origen, CancellationToken cancellationToken)
    {
        string[] numerosPendientes = await this.SeleccionarPendientesAsync(Math.Max(1, tamanoLote), cancellationToken);

        int detallados = 0;
        int errores = 0;
        foreach (string numeroExpediente in numerosPendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                ObtenerExpedienteRecursoResult<ExpedienteCompletoDto> resultado = await _expedienteService.ObtenerCompletoAsync(
                    new ObtenerExpedienteRecursoRequest(numeroExpediente, ForceRefresh: false, Origen: origen), cancellationToken);
                if (resultado.Exitoso)
                {
                    detallados++;
                }
                else
                {
                    errores++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                errores++;
                _logger.LogError(exception, "Fallo el detalle programado del expediente {NumeroExpediente}.", numeroExpediente);
            }
        }

        int pendientesRestantes = await _expedienteRepository.Query()
            .Where(x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null)
            .CountAsync(cancellationToken);
        return new DetallarExpedientesPendientesResult(numerosPendientes.Length, detallados, errores, pendientesRestantes);
    }

    private async Task<string[]> SeleccionarPendientesAsync(int tamanoLote, CancellationToken cancellationToken)
    {
        // El lote se llena por prioridad de trata (la misma configuracion del descubrimiento) y, dentro de cada grupo, del caratulado mas nuevo al mas viejo; anio y numero GDEBA reflejan el orden de caratulacion porque la fecha explicita recien llega con el detalle.
        Dictionary<Guid, int> prioridadPorTrataId = await this.CargarPrioridadesPorTrataAsync(cancellationToken);
        List<string> numerosPendientes = new(tamanoLote);
        foreach (IGrouping<int, Guid> grupo in prioridadPorTrataId.GroupBy(x => x.Value, x => x.Key).OrderBy(x => x.Key))
        {
            if (numerosPendientes.Count >= tamanoLote)
            {
                break;
            }

            Guid[] tratasDelGrupo = grupo.ToArray();
            numerosPendientes.AddRange(await this.ConsultarPendientesAsync(
                x => x.TrataId.HasValue && tratasDelGrupo.Contains(x.TrataId.Value),
                tamanoLote - numerosPendientes.Count,
                cancellationToken));
        }

        if (numerosPendientes.Count < tamanoLote)
        {
            Guid[] tratasPriorizadas = prioridadPorTrataId.Keys.ToArray();
            numerosPendientes.AddRange(await this.ConsultarPendientesAsync(
                x => !x.TrataId.HasValue || !tratasPriorizadas.Contains(x.TrataId.Value),
                tamanoLote - numerosPendientes.Count,
                cancellationToken));
        }

        return numerosPendientes.ToArray();
    }

    private async Task<IEnumerable<string>> ConsultarPendientesAsync(Expression<Func<Expediente, bool>> filtroTrata, int cantidad, CancellationToken cancellationToken)
    {
        return (await _expedienteRepository.Query()
            .Where(x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null)
            .Where(filtroTrata)
            .OrderByDescending(x => x.GdebaAnio)
            .ThenByDescending(x => x.GdebaNumero)
            .Take(cantidad)
            .SelectAsync(cancellationToken))
            .Select(x => x.GdebaNumeroCompleto);
    }

    private async Task<Dictionary<Guid, int>> CargarPrioridadesPorTrataAsync(CancellationToken cancellationToken)
    {
        Dictionary<Guid, int> prioridadPorTrataId = new();
        IEnumerable<ConfiguracionDescubrimientoTemaExpediente> configuracionesTemas = await _configuracionDescubrimientoTemaRepository.Query()
            .Where(x => x.Habilitado)
            .SelectAsync(cancellationToken);
        Dictionary<Guid, int> prioridadPorTemaId = configuracionesTemas.ToDictionary(x => x.TemaExpedienteId, x => x.Prioridad);
        if (prioridadPorTemaId.Count > 0)
        {
            Guid[] idsTemas = prioridadPorTemaId.Keys.ToArray();
            IEnumerable<TemaExpedienteTrata> asignaciones = await _temaExpedienteTrataRepository.Query()
                .Where(x => idsTemas.Contains(x.TemaExpedienteId))
                .SelectAsync(cancellationToken);
            foreach (TemaExpedienteTrata asignacion in asignaciones)
            {
                ExpedienteDetalladoWorkerService.RegistrarPrioridad(prioridadPorTrataId, asignacion.TrataHabilitadaVialidadId, prioridadPorTemaId[asignacion.TemaExpedienteId]);
            }
        }

        IEnumerable<ConfiguracionDescubrimientoTrataExpediente> configuracionesTratas = await _configuracionDescubrimientoTrataRepository.Query()
            .Where(x => x.Habilitada)
            .SelectAsync(cancellationToken);
        string[] codigosConfigurados = configuracionesTratas
            .Select(x => x.CodigoTrata.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (codigosConfigurados.Length > 0)
        {
            Dictionary<string, TrataHabilitadaVialidad> tratasPorCodigo = (await _trataHabilitadaVialidadRepository.Query()
                    .Where(x => codigosConfigurados.Contains(x.CodigoTrata))
                    .SelectAsync(cancellationToken))
                .GroupBy(x => x.CodigoTrata.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, TrataHabilitadaVialidad.ElegirRepresentantePorCodigo, StringComparer.OrdinalIgnoreCase);
            foreach (ConfiguracionDescubrimientoTrataExpediente configuracionTrata in configuracionesTratas)
            {
                if (tratasPorCodigo.TryGetValue(configuracionTrata.CodigoTrata.Trim().ToUpperInvariant(), out TrataHabilitadaVialidad? trata))
                {
                    ExpedienteDetalladoWorkerService.RegistrarPrioridad(prioridadPorTrataId, trata.Id, configuracionTrata.Prioridad);
                }
            }
        }

        return prioridadPorTrataId;
    }

    private static void RegistrarPrioridad(Dictionary<Guid, int> prioridadPorTrataId, Guid trataId, int prioridad)
    {
        prioridadPorTrataId[trataId] = prioridadPorTrataId.TryGetValue(trataId, out int actual)
            ? Math.Min(actual, prioridad)
            : prioridad;
    }
}
