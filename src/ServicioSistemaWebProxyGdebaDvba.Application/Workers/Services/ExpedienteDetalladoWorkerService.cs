using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

public sealed class ExpedienteDetalladoWorkerService : IExpedienteDetalladoWorkerService
{
    // Operacion solicitada con la que quedan auditadas las consultas del worker (antes figuraban como ObtenerCompleto).
    private const string OperacionDetalleProgramado = "DetallarExpedienteProgramado";

    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<SeguimientoExpediente> _seguimientoRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTemaExpediente> _configuracionDescubrimientoTemaRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTrataExpediente> _configuracionDescubrimientoTrataRepository;
    private readonly IRepository<TemaExpedienteTrata> _temaExpedienteTrataRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataHabilitadaVialidadRepository;
    private readonly IExpedienteService _expedienteService;
    private readonly ISensorConexionGdeba _sensorConexion;
    private readonly ILogger<ExpedienteDetalladoWorkerService> _logger;

    public ExpedienteDetalladoWorkerService(
        IRepository<Expediente> expedienteRepository,
        IRepository<SeguimientoExpediente> seguimientoRepository,
        IRepository<ConfiguracionDescubrimientoTemaExpediente> configuracionDescubrimientoTemaRepository,
        IRepository<ConfiguracionDescubrimientoTrataExpediente> configuracionDescubrimientoTrataRepository,
        IRepository<TemaExpedienteTrata> temaExpedienteTrataRepository,
        IRepository<TrataHabilitadaVialidad> trataHabilitadaVialidadRepository,
        IExpedienteService expedienteService,
        ISensorConexionGdeba sensorConexion,
        ILogger<ExpedienteDetalladoWorkerService> logger)
    {
        _expedienteRepository = expedienteRepository;
        _seguimientoRepository = seguimientoRepository;
        _configuracionDescubrimientoTemaRepository = configuracionDescubrimientoTemaRepository;
        _configuracionDescubrimientoTrataRepository = configuracionDescubrimientoTrataRepository;
        _temaExpedienteTrataRepository = temaExpedienteTrataRepository;
        _trataHabilitadaVialidadRepository = trataHabilitadaVialidadRepository;
        _expedienteService = expedienteService;
        _sensorConexion = sensorConexion;
        _logger = logger;
    }

    public async Task<DetallarExpedientesPendientesResult> DetallarPendientesAsync(int tamanoLote, OrigenInvocacionGdeba origen, Func<CancellationToken, Task<bool>>? cancelacionSolicitada, CancellationToken cancellationToken)
    {
        List<PendienteSeleccionado> pendientes = await this.SeleccionarPendientesAsync(Math.Max(1, tamanoLote), cancellationToken);

        int procesados = 0;
        int detallados = 0;
        int errores = 0;
        int refrescados = 0;
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

            string numeroExpediente = pendiente.NumeroGdebaCompleto;
            // Con GDEBA marcado como no accesible el servicio responderia desde la copia local y marcaria el expediente como consultado:
            // se corta antes de tocarlo y se deja en marcha la sonda de reconexion, que restablece el sensor cuando GDEBA vuelve.
            if (!_sensorConexion.Accesible)
            {
                _sensorConexion.SondearSiCorresponde(numeroExpediente);
                throw new GdebaNoAccesibleException("GDEBA figura como no accesible desde este servidor.");
            }

            procesados++;
            try
            {
                // Detalle e historial por separado, sin armar el DTO completo: ese mapeo (una carga mas del expediente, tipos de documento,
                // descripciones) existe para la pantalla y el worker solo necesita saber si ambas consultas consolidaron.
                ConsultarExpedienteDetalladoResult detalle = await _expedienteService.ConsultarDetalleAsync(
                    new ConsultarExpedienteDetalladoRequest(numeroExpediente, pendiente.ForceRefresh, OperacionDetalleProgramado, origen), cancellationToken);
                ConsultarMovimientosExpedienteResult historial = await _expedienteService.ConsultarMovimientosAsync(
                    new ConsultarMovimientosExpedienteRequest(numeroExpediente, pendiente.ForceRefresh, OperacionDetalleProgramado, origen), cancellationToken);
                if (detalle.Expediente is not null && historial.Exitoso)
                {
                    detallados++;
                    if (pendiente.EsRefrescoPorModificacion) refrescados++;
                }
                else
                {
                    errores++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (GdebaNoAccesibleException.EsNoAccesible(exception))
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
            .Where(ExpedienteDetalladoWorkerService.NuncaDetallado)
            .CountAsync(cancellationToken);
        int refrescosPendientes = await _expedienteRepository.Query()
            .Where(ExpedienteDetalladoWorkerService.ModificadoDespuesDelUltimoDetalle)
            .CountAsync(cancellationToken);
        return new DetallarExpedientesPendientesResult(procesados, detallados, errores, pendientesRestantes, refrescados, refrescosPendientes, cancelada);
    }

    // Nunca detallado: el historial de pases no se consulto a GDEBA ni una vez.
    private static readonly Expression<Func<Expediente, bool>> NuncaDetallado =
        x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null;

    // Ya detallado, pero el descubrimiento informo una modificacion en GDEBA posterior a la ultima consulta del historial: hay cambios que
    // la copia local no tiene. Validado en octubre de 2026 contra 14 mil expedientes: la fecha informada refleja todo pase o documento.
    private static readonly Expression<Func<Expediente, bool>> ModificadoDespuesDelUltimoDetalle =
        x => x.HistorialCacheControl != null && x.HistorialCacheControl.FechaUltimaConsultaGdeba != null &&
            x.FechaModificacionGdeba != null && x.FechaModificacionGdeba > x.HistorialCacheControl.FechaUltimaConsultaGdeba;

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
                seleccion.Add(new PendienteSeleccionado(seguido.GdebaNumeroCompleto, ForceRefresh: true, EsRefrescoPorModificacion: false));
            }
        }

        // Las fases siguientes recorren las tratas por prioridad (la misma configuracion del descubrimiento) y al final el resto.
        Dictionary<Guid, int> prioridadPorTrataId = await this.CargarPrioridadesPorTrataAsync(cancellationToken);
        Guid[] tratasPriorizadas = prioridadPorTrataId.Keys.ToArray();
        List<Expression<Func<Expediente, bool>>> filtrosPorPrioridad = prioridadPorTrataId
            .GroupBy(x => x.Value, x => x.Key)
            .OrderBy(x => x.Key)
            .Select(grupo => grupo.ToArray())
            .Select(tratasDelGrupo => (Expression<Func<Expediente, bool>>)(x => x.TrataId.HasValue && tratasDelGrupo.Contains(x.TrataId.Value)))
            .Append(x => !x.TrataId.HasValue || !tratasPriorizadas.Contains(x.TrataId.Value))
            .ToList();

        // Fase 1: refresco por evidencia. Ya detallados con modificacion en GDEBA posterior a su ultimo detalle, la mas reciente primero
        // dentro de cada prioridad. Va antes del drenaje para que lo modificado del dia nunca espere detras de lo nunca detallado.
        // ForceRefresh porque la vigencia por calendario puede seguir vigente.
        foreach (Expression<Func<Expediente, bool>> filtroTrata in filtrosPorPrioridad)
        {
            if (seleccion.Count >= tamanoLote)
            {
                break;
            }

            ExpedienteDetalladoWorkerService.AgregarPendientes(seleccion, numerosSeleccionados, tamanoLote, esRefrescoPorModificacion: true,
                await this.ConsultarModificadosAsync(filtroTrata, tamanoLote - seleccion.Count + numerosSeleccionados.Count, cancellationToken));
        }

        // Fase 2: drenaje de nunca detallados con lo que queda del lote, siempre del caratulado mas nuevo al mas viejo; anio y numero GDEBA
        // reflejan el orden de caratulacion porque la fecha explicita recien llega con el detalle.
        foreach (Expression<Func<Expediente, bool>> filtroTrata in filtrosPorPrioridad)
        {
            if (seleccion.Count >= tamanoLote)
            {
                break;
            }

            ExpedienteDetalladoWorkerService.AgregarPendientes(seleccion, numerosSeleccionados, tamanoLote, esRefrescoPorModificacion: false,
                await this.ConsultarNuncaDetalladosAsync(filtroTrata, tamanoLote - seleccion.Count + numerosSeleccionados.Count, cancellationToken));
        }

        return seleccion;
    }

    private static void AgregarPendientes(List<PendienteSeleccionado> seleccion, HashSet<string> numerosSeleccionados, int tamanoLote, bool esRefrescoPorModificacion, IEnumerable<string> numeros)
    {
        foreach (string numero in numeros)
        {
            if (seleccion.Count >= tamanoLote)
            {
                return;
            }

            if (numerosSeleccionados.Add(numero))
            {
                seleccion.Add(new PendienteSeleccionado(numero, ForceRefresh: esRefrescoPorModificacion, esRefrescoPorModificacion));
            }
        }
    }

    private async Task<IEnumerable<string>> ConsultarModificadosAsync(Expression<Func<Expediente, bool>> filtroTrata, int cantidad, CancellationToken cancellationToken)
    {
        return (await _expedienteRepository.Query()
            .Where(ExpedienteDetalladoWorkerService.ModificadoDespuesDelUltimoDetalle)
            .Where(filtroTrata)
            .OrderByDescending(x => x.FechaModificacionGdeba)
            .Take(cantidad)
            .SelectAsync(cancellationToken))
            .Select(x => x.GdebaNumeroCompleto);
    }

    private async Task<IEnumerable<string>> ConsultarNuncaDetalladosAsync(Expression<Func<Expediente, bool>> filtroTrata, int cantidad, CancellationToken cancellationToken)
    {
        // El orden se expresa en una sola clave (anio y numero combinados) porque el ThenBy del wrapper de URF no llega al SQL y dejaria el orden dentro del anio indefinido.
        return (await _expedienteRepository.Query()
            .Where(ExpedienteDetalladoWorkerService.NuncaDetallado)
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

    private sealed record PendienteSeleccionado(string NumeroGdebaCompleto, bool ForceRefresh, bool EsRefrescoPorModificacion);
}
