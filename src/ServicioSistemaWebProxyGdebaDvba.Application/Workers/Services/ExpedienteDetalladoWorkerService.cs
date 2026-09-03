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
    private readonly IRepository<SeguimientoExpediente> _seguimientoRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTemaExpediente> _configuracionDescubrimientoTemaRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTrataExpediente> _configuracionDescubrimientoTrataRepository;
    private readonly IRepository<TemaExpedienteTrata> _temaExpedienteTrataRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataHabilitadaVialidadRepository;
    private readonly IExpedienteService _expedienteService;
    private readonly ILogger<ExpedienteDetalladoWorkerService> _logger;

    public ExpedienteDetalladoWorkerService(
        IRepository<Expediente> expedienteRepository,
        IRepository<SeguimientoExpediente> seguimientoRepository,
        IRepository<ConfiguracionDescubrimientoTemaExpediente> configuracionDescubrimientoTemaRepository,
        IRepository<ConfiguracionDescubrimientoTrataExpediente> configuracionDescubrimientoTrataRepository,
        IRepository<TemaExpedienteTrata> temaExpedienteTrataRepository,
        IRepository<TrataHabilitadaVialidad> trataHabilitadaVialidadRepository,
        IExpedienteService expedienteService,
        ILogger<ExpedienteDetalladoWorkerService> logger)
    {
        _expedienteRepository = expedienteRepository;
        _seguimientoRepository = seguimientoRepository;
        _configuracionDescubrimientoTemaRepository = configuracionDescubrimientoTemaRepository;
        _configuracionDescubrimientoTrataRepository = configuracionDescubrimientoTrataRepository;
        _temaExpedienteTrataRepository = temaExpedienteTrataRepository;
        _trataHabilitadaVialidadRepository = trataHabilitadaVialidadRepository;
        _expedienteService = expedienteService;
        _logger = logger;
    }

    public async Task<DetallarExpedientesPendientesResult> DetallarPendientesAsync(int tamanoLote, OrigenInvocacionGdeba origen, Func<CancellationToken, Task<bool>>? cancelacionSolicitada, CancellationToken cancellationToken)
    {
        List<PendienteSeleccionado> pendientes = await this.SeleccionarPendientesAsync(Math.Max(1, tamanoLote), cancellationToken);

        int procesados = 0;
        int detallados = 0;
        int errores = 0;
        bool cancelada = false;
        foreach (PendienteSeleccionado pendiente in pendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // La cancelacion corta entre expedientes: el que esta en curso siempre termina y persiste completo.
            if (cancelacionSolicitada is not null && await cancelacionSolicitada(cancellationToken))
            {
                cancelada = true;
                break;
            }

            procesados++;
            string numeroExpediente = pendiente.NumeroGdebaCompleto;
            try
            {
                ObtenerExpedienteRecursoResult<ExpedienteCompletoDto> resultado = await _expedienteService.ObtenerCompletoAsync(
                    new ObtenerExpedienteRecursoRequest(numeroExpediente, ForceRefresh: pendiente.ForceRefresh, Origen: origen), cancellationToken);
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
        return new DetallarExpedientesPendientesResult(procesados, detallados, errores, pendientesRestantes, cancelada);
    }

    private async Task<List<PendienteSeleccionado>> SeleccionarPendientesAsync(int tamanoLote, CancellationToken cancellationToken)
    {
        List<PendienteSeleccionado> seleccion = new(tamanoLote);
        HashSet<string> numerosSeleccionados = new(StringComparer.OrdinalIgnoreCase);

        // Fase 0: expedientes seguidos por alguna persona cuya ultima consulta es anterior al dia de hoy — la actualizacion diaria por prioridad de usuario. ForceRefresh saltea la vigencia de la cache.
        DateTimeOffset inicioDelDia = new(DateTime.Today, DateTimeOffset.Now.Offset);
        IEnumerable<SeguimientoExpediente> seguimientosVencidos = await _seguimientoRepository.Query()
            .Include(nameof(SeguimientoExpediente.Expediente))
            .Where(x => x.Expediente.HistorialCacheControl == null || x.Expediente.HistorialCacheControl.FechaUltimaConsultaGdeba == null || x.Expediente.HistorialCacheControl.FechaUltimaConsultaGdeba < inicioDelDia)
            .SelectAsync(cancellationToken);
        foreach (Expediente seguido in seguimientosVencidos
            .Select(x => x.Expediente)
            .DistinctBy(x => x.Id)
            .OrderByDescending(x => (long)x.GdebaAnio * 100000000L + x.GdebaNumero))
        {
            if (seleccion.Count >= tamanoLote)
            {
                break;
            }

            if (numerosSeleccionados.Add(seguido.GdebaNumeroCompleto))
            {
                seleccion.Add(new PendienteSeleccionado(seguido.GdebaNumeroCompleto, ForceRefresh: true));
            }
        }

        // Fases siguientes: nunca consultados por prioridad de trata (la misma configuracion del descubrimiento) y relleno general, siempre del caratulado mas nuevo al mas viejo; anio y numero GDEBA reflejan el orden de caratulacion porque la fecha explicita recien llega con el detalle.
        Dictionary<Guid, int> prioridadPorTrataId = await this.CargarPrioridadesPorTrataAsync(cancellationToken);
        foreach (IGrouping<int, Guid> grupo in prioridadPorTrataId.GroupBy(x => x.Value, x => x.Key).OrderBy(x => x.Key))
        {
            if (seleccion.Count >= tamanoLote)
            {
                break;
            }

            Guid[] tratasDelGrupo = grupo.ToArray();
            ExpedienteDetalladoWorkerService.AgregarPendientes(seleccion, numerosSeleccionados, tamanoLote, await this.ConsultarPendientesAsync(
                x => x.TrataId.HasValue && tratasDelGrupo.Contains(x.TrataId.Value),
                tamanoLote - seleccion.Count + numerosSeleccionados.Count,
                cancellationToken));
        }

        if (seleccion.Count < tamanoLote)
        {
            Guid[] tratasPriorizadas = prioridadPorTrataId.Keys.ToArray();
            ExpedienteDetalladoWorkerService.AgregarPendientes(seleccion, numerosSeleccionados, tamanoLote, await this.ConsultarPendientesAsync(
                x => !x.TrataId.HasValue || !tratasPriorizadas.Contains(x.TrataId.Value),
                tamanoLote - seleccion.Count + numerosSeleccionados.Count,
                cancellationToken));
        }

        return seleccion;
    }

    private static void AgregarPendientes(List<PendienteSeleccionado> seleccion, HashSet<string> numerosSeleccionados, int tamanoLote, IEnumerable<string> numeros)
    {
        foreach (string numero in numeros)
        {
            if (seleccion.Count >= tamanoLote)
            {
                return;
            }

            if (numerosSeleccionados.Add(numero))
            {
                seleccion.Add(new PendienteSeleccionado(numero, ForceRefresh: false));
            }
        }
    }

    private async Task<IEnumerable<string>> ConsultarPendientesAsync(Expression<Func<Expediente, bool>> filtroTrata, int cantidad, CancellationToken cancellationToken)
    {
        // El orden se expresa en una sola clave (anio y numero combinados) porque el ThenBy del wrapper de URF no llega al SQL y dejaria el orden dentro del anio indefinido.
        return (await _expedienteRepository.Query()
            .Where(x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null)
            .Where(filtroTrata)
            .OrderByDescending(x => (long)x.GdebaAnio * 100000000L + x.GdebaNumero)
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

    private sealed record PendienteSeleccionado(string NumeroGdebaCompleto, bool ForceRefresh);
}
