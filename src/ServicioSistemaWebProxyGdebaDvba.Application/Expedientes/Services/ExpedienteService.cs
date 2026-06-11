using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Security;
using ServicioSistemaWebProxyGdebaDvba.Application.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

public sealed class ExpedienteService : IExpedienteService
{
    private const string OperacionDetalle = "consultarExpedienteDetallado";
    private const string OperacionHistorial = "buscarHistorialPasesExpediente";
    private const string OperacionExpediente = "expediente";

    private readonly IExpedienteCacheReadStore _expedienteCacheReadStore;
    private readonly IGdebaExpedienteGateway _gdebaExpedienteGateway;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;
    private readonly ITrackableRepository<Expediente> _expedienteRepository;
    private readonly ITrackableRepository<TrataGdeba> _trataRepository;
    private readonly ITrackableRepository<DocumentoGdeba> _documentoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpedienteService> _logger;

    public ExpedienteService( IExpedienteCacheReadStore expedienteCacheReadStore, IGdebaExpedienteGateway gdebaExpedienteGateway, IGdebaExecutionContext gdebaExecutionContext,
                              IAuditoriaService auditoriaService, ICurrentApplicationAccessor currentApplicationAccessor, 
                              ITrackableRepository<Expediente> expedienteRepository,
                              ITrackableRepository<TrataGdeba> trataRepository, 
                              ITrackableRepository<DocumentoGdeba> documentoRepository, 
                              IUnitOfWork unitOfWork, ILogger<ExpedienteService> logger)
    {
        _expedienteCacheReadStore = expedienteCacheReadStore;
        _gdebaExpedienteGateway = gdebaExpedienteGateway;
        _gdebaExecutionContext = gdebaExecutionContext;
        _auditoriaService = auditoriaService;
        _currentApplicationAccessor = currentApplicationAccessor;
        _expedienteRepository = expedienteRepository;
        _trataRepository = trataRepository;
        _documentoRepository = documentoRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    #region Metodos publicos del servicio

    /// <summary>
    /// Consulta el detalle de un expediente aplicando politica de cache local y fallback ante falta de respuesta de GDEBA.
    /// </summary>
    /// <param name="request">Datos de la solicitud, incluyendo numero de expediente y opcion de refresco forzado.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Resultado detallado del expediente, fuente utilizada y fecha de cache cuando corresponda.</returns>
    public async Task<ConsultarExpedienteDetalladoResult> ConsultarDetalleAsync(
        ConsultarExpedienteDetalladoRequest request,
        CancellationToken cancellationToken)
    {
        // Normaliza el numero completo y busca una copia local para evaluar la politica de cache.
        var numero = NumeroGdebaCompleto.Create(request.NumeroGdebaCompleto);
        var resolvedAt = DateTimeOffset.Now;

        //lee datos de cache para evaluar si se puede responder desde cache o si es necesario consultar a GDEBA. Esta consulta no bloquea la respuesta y se vuelve a realizar si se necesita refrescar desde GDEBA para obtener la entidad completa y actualizada.
        var expediente = await _expedienteCacheReadStore.BuscarExpedienteParaDetalleAsync(numero.Valor, cancellationToken);
        ExpedienteDetalladoDto? expedienteDto;
        FuenteRespuesta fuente;
        bool exitoso;
        DateTimeOffset? cachedAt = null;

        if (!request.ForceRefresh && expediente?.PuedeResponderDetalleDesdeCache(resolvedAt) == true)
        {
            // Responde desde cache cuando el expediente esta completo y dentro de su vigencia.
            expedienteDto = Mapear(expediente!);
            fuente = FuenteRespuesta.Cache;
            exitoso = true;
            cachedAt = expediente!.CacheControl?.FechaUltimaActualizacionLocal;
        }
        else
        {
            // Consulta GDEBA cuando no hay cache vigente o cuando la solicitud exige refresco.
            GdebaExpedienteDetalladoDto? detalle;
            try
            {
                detalle = await _gdebaExpedienteGateway.ConsultarExpedienteDetalladoAsync(numero, cancellationToken);
            }
            catch (GdebaOperationException)
            {
                await RegistrarFalloGdebaAsync(OperacionDetalle, numero.Valor, resolvedAt, cancellationToken);
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await RegistrarFalloGdebaAsync(OperacionDetalle, numero.Valor, resolvedAt, cancellationToken);
                throw new GdebaOperationException(OperacionDetalle, $"No se pudo ejecutar la operacion GDEBA: {ex.Message}", innerException: ex);
            }
            if (detalle is null)
            {
                if (expediente is not null)
                {
                    // Conserva la ultima copia local como fallback y deja marcado el error de refresco.
                    MarcarDetalleConsultadoConError(expediente, resolvedAt, resolvedAt, "GDEBA no devolvio detalle del expediente.");
                    RegistrarCambiosExpediente(expediente, esNuevo: false);

                    expedienteDto = Mapear(expediente);
                    fuente = FuenteRespuesta.FallbackCache;
                    exitoso = true;
                    cachedAt = expediente.CacheControl?.FechaUltimaActualizacionLocal;
                }
                else
                {
                    expedienteDto = null;
                    fuente = FuenteRespuesta.Gdeba;
                    exitoso = false;
                }
            }
            else
            {
                // Consolida la cache en forma local. La mensajeria queda reservada para una etapa posterior
                // con worker, monitoreo y politica de reintentos operativa.
                await ConsolidarDetalleEnCacheAsync(detalle, resolvedAt, cancellationToken);

                expedienteDto = MapearRespuestaLiviana(detalle);
                fuente = FuenteRespuesta.Gdeba;
                exitoso = true;
            }
        }

        // Centraliza la auditoria para todos los caminos funcionales de la consulta detallada.
        await RegistrarAuditoriaAsync(OperacionDetalle, numero.Valor, fuente, exitoso, resolvedAt, cancellationToken);

        // Confirma en una sola transaccion los cambios de dominio y la auditoria funcional.
        await ConfirmarCambiosAsync("ConsultarExpedienteDetallado", numero.Valor, cancellationToken);

        return new ConsultarExpedienteDetalladoResult(expedienteDto, fuente, resolvedAt, cachedAt);
    }

    /// <summary>
    /// Consulta los movimientos o pases de un expediente aplicando cache local bajo demanda.
    /// </summary>
    /// <param name="request">Datos de la solicitud, incluyendo numero de expediente y opcion de refresco forzado.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Resultado con movimientos del expediente, fuente utilizada y fecha de cache cuando corresponda.</returns>
    public async Task<ConsultarMovimientosExpedienteResult> ConsultarMovimientosAsync(
        ConsultarMovimientosExpedienteRequest request,
        CancellationToken cancellationToken)
    {
        // Normaliza el numero completo y busca la copia local para evaluar la cache de movimientos.
        var numero = NumeroGdebaCompleto.Create(request.NumeroGdebaCompleto);
        var resolvedAt = DateTimeOffset.Now;
        var expediente = await _expedienteCacheReadStore.BuscarExpedienteParaMovimientosAsync(numero.Valor, cancellationToken);
        var expedienteEsNuevo = false;
        var expedienteModificado = false;

        if (expediente?.CacheControl is null)
        {
            var detalle = await ConsultarDetalleAsync(new ConsultarExpedienteDetalladoRequest(numero.Valor), cancellationToken);

            if (detalle.Expediente is null)
            {
                return new ConsultarMovimientosExpedienteResult(numero.Valor, Array.Empty<MovimientoExpedienteDto>(), detalle.Fuente, Exitoso: false, resolvedAt, detalle.CachedAt);
            }

            expediente = await _expedienteCacheReadStore.BuscarExpedienteParaMovimientosAsync(numero.Valor, cancellationToken);
        }

        IReadOnlyCollection<MovimientoExpedienteDto> movimientos;
        FuenteRespuesta fuente;
        bool exitoso;
        DateTimeOffset? cachedAt = null;

        if (!request.ForceRefresh && expediente?.PuedeResponderMovimientosDesdeCache(resolvedAt) == true)
        {
            // Responde desde cache cuando los movimientos fueron sincronizados y siguen vigentes.
            movimientos = MapearMovimientos(expediente);
            fuente = FuenteRespuesta.Cache;
            exitoso = true;
            cachedAt = expediente.HistorialCacheControl?.FechaUltimaActualizacionLocal;
        }
        else
        {
            // Consulta GDEBA solo cuando no hay movimientos vigentes o cuando se fuerza el refresco.
            GdebaHistorialExpedienteDto? historialGdeba;
            try
            {
                historialGdeba = await _gdebaExpedienteGateway.BuscarHistorialPasesExpedienteAsync(numero, cancellationToken);
            }
            catch (GdebaOperationException)
            {
                await RegistrarFalloGdebaAsync(OperacionHistorial, numero.Valor, resolvedAt, cancellationToken);
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await RegistrarFalloGdebaAsync(OperacionHistorial, numero.Valor, resolvedAt, cancellationToken);
                throw new GdebaOperationException(OperacionHistorial, $"No se pudo ejecutar la operacion GDEBA: {ex.Message}", innerException: ex);
            }

            if (historialGdeba is null)
            {
                if (expediente is not null)
                {
                    // Conserva los movimientos locales como fallback ante una falta de respuesta externa.
                    MarcarHistorialConsultadoConError(expediente, resolvedAt, resolvedAt, "GDEBA no devolvio movimientos del expediente.");
                    expedienteModificado = true;

                    movimientos = MapearMovimientos(expediente);
                    fuente = FuenteRespuesta.FallbackCache;
                    exitoso = true;
                    cachedAt = expediente.HistorialCacheControl?.FechaUltimaActualizacionLocal;
                }
                else
                {
                    movimientos = Array.Empty<MovimientoExpedienteDto>();
                    fuente = FuenteRespuesta.Gdeba;
                    exitoso = false;
                }
            }
            else
            {
                // Crea o actualiza el expediente y consolida todas las colecciones del historial recibido.
                if (expediente is null)
                {
                    expediente = CrearExpediente(numero);
                    expedienteEsNuevo = true;
                }

                var documentos = await ResolverDocumentosAsync(historialGdeba.DocumentosVinculados, cancellationToken);

                ConsolidarDocumentos(expediente, historialGdeba.DocumentosVinculados, documentos, FuenteDeteccionGdeba.BuscarHistorialPasesExpediente, resolvedAt);

                ConsolidarRelaciones(expediente, historialGdeba.Relaciones, FuenteDeteccionGdeba.BuscarHistorialPasesExpediente, resolvedAt);

                var movimientosDetectados = historialGdeba.Movimientos
                    .Select(x => new MovimientoExpedienteDetectado(x.Orden, x.FechaOperacion, x.EstadoOrigen, x.EstadoDestino, x.UsuarioOrigen, x.UsuarioDestino, x.Motivo, x.ReparticionOrigen, x.ReparticionDestino))
                    .ToArray();
                var ultimoMovimiento = expediente.ConsolidarMovimientosDetectados(movimientosDetectados);
                var ultimoMovimientoGdeba = ResolverUltimoMovimiento(historialGdeba.Movimientos);
                if (ultimoMovimientoGdeba is not null)
                {
                    expediente.ActualizarDestinoActualDesdeHistorial(ultimoMovimientoGdeba.ReparticionDestino, ultimoMovimientoGdeba.SectorDestino);
                }

                // Marca la cache de movimientos con vigencia diaria para la consulta bajo demanda.
                MarcarHistorialConsultadoCorrectamente(
                    expediente, resolvedAt, resolvedAt, CalcularVencimientoDiario(resolvedAt), ultimoMovimiento, estaCompleto: true, tieneDatosParciales: false);

                movimientos = MapearMovimientos(expediente);
                fuente = FuenteRespuesta.Gdeba;
                exitoso = true;
                expedienteModificado = true;
            }
        }

        // Registra auditoria funcional una sola vez, independientemente de la fuente de respuesta.
        if (expediente is not null && expedienteModificado)
        {
            RegistrarCambiosExpediente(expediente, expedienteEsNuevo);
        }

        await RegistrarAuditoriaAsync(OperacionHistorial, numero.Valor, fuente, exitoso, resolvedAt, cancellationToken);

        // Persiste movimientos/cache y auditoria en una unica unidad de trabajo.
        await ConfirmarCambiosAsync("ConsultarMovimientosExpediente", numero.Valor, cancellationToken);

        return new ConsultarMovimientosExpedienteResult(numero.Valor, movimientos, fuente, exitoso, resolvedAt, cachedAt);
    }

    public async Task<ObtenerExpedienteRecursoResult<CabeceraExpedienteDto>> ObtenerCabeceraAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ConsultarDetalleAsync(new ConsultarExpedienteDetalladoRequest(request.NumeroGdebaCompleto, request.ForceRefresh), cancellationToken);

        var cabecera = result.Expediente is null ? null : MapearCabecera(result.Expediente);
        return CrearResultadoRecurso(request.NumeroGdebaCompleto, cabecera, result.Fuente, cabecera is not null, result.ResolvedAt, result.CachedAt);
    }

    public async Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<DocumentoExpedienteDto>>> ObtenerDocumentosAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken)
    {
        var historial = await ConsultarMovimientosAsync(new ConsultarMovimientosExpedienteRequest(request.NumeroGdebaCompleto, request.ForceRefresh), cancellationToken);
        var expediente = await BuscarExpedienteCompletoAsync(request.NumeroGdebaCompleto, cancellationToken);
        var documentos = expediente is null
            ? null
            : Mapear(expediente).Documentos;

        return CrearResultadoRecurso(request.NumeroGdebaCompleto, documentos, historial.Source, historial.Exitoso && documentos is not null, historial.ResolvedAt, historial.CachedAt);
    }

    public async Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<ArchivoAdjuntoExpedienteDto>>> ObtenerAdjuntosAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken)
    {
        var detalle = await ConsultarDetalleAsync(new ConsultarExpedienteDetalladoRequest(request.NumeroGdebaCompleto, request.ForceRefresh), cancellationToken);
        var expediente = await BuscarExpedienteCompletoAsync(request.NumeroGdebaCompleto, cancellationToken);
        var adjuntos = expediente is null
            ? null
            : Mapear(expediente).ArchivosAdjuntos;

        return CrearResultadoRecurso(request.NumeroGdebaCompleto, adjuntos, detalle.Fuente, detalle.Expediente is not null && adjuntos is not null, detalle.ResolvedAt, detalle.CachedAt);
    }

    public async Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<MovimientoExpedienteDto>>> ObtenerPasesAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken)
    {
        var historial = await ConsultarMovimientosAsync(new ConsultarMovimientosExpedienteRequest(request.NumeroGdebaCompleto, request.ForceRefresh), cancellationToken);

        return CrearResultadoRecurso(request.NumeroGdebaCompleto, historial.Movimientos, historial.Source, historial.Exitoso, historial.ResolvedAt, historial.CachedAt);
    }

    public async Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<RelacionExpedienteDto>>> ObtenerRelacionesAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken)
    {
        var historial = await ConsultarMovimientosAsync(new ConsultarMovimientosExpedienteRequest(request.NumeroGdebaCompleto, request.ForceRefresh), cancellationToken);
        var expediente = await BuscarExpedienteCompletoAsync(request.NumeroGdebaCompleto, cancellationToken);
        var relaciones = expediente is null
            ? null
            : Mapear(expediente).Relaciones;

        return CrearResultadoRecurso(request.NumeroGdebaCompleto, relaciones, historial.Source, historial.Exitoso && relaciones is not null, historial.ResolvedAt, historial.CachedAt);
    }

    public async Task<ObtenerExpedienteRecursoResult<ExpedienteCompletoDto>> ObtenerCompletoAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken)
    {
        var detalle = await ConsultarDetalleAsync(new ConsultarExpedienteDetalladoRequest(request.NumeroGdebaCompleto, request.ForceRefresh), cancellationToken);
        var historial = await ConsultarMovimientosAsync(new ConsultarMovimientosExpedienteRequest(request.NumeroGdebaCompleto, request.ForceRefresh), cancellationToken);
        var expediente = await BuscarExpedienteCompletoAsync(request.NumeroGdebaCompleto, cancellationToken);

        ExpedienteCompletoDto? completo = null;
        if (expediente is not null)
        {
            var expedienteDto = Mapear(expediente);
            completo = new ExpedienteCompletoDto(
                MapearCabecera(expedienteDto), expedienteDto.Documentos, expedienteDto.ArchivosAdjuntos, MapearMovimientos(expediente), expedienteDto.Relaciones);
        }

        return CrearResultadoRecurso(
            request.NumeroGdebaCompleto, completo, CombinarFuente(detalle.Fuente, historial.Source), detalle.Expediente is not null && historial.Exitoso && completo is not null, detalle.ResolvedAt > historial.ResolvedAt ? detalle.ResolvedAt : historial.ResolvedAt, Max(detalle.CachedAt, historial.CachedAt));
    }

    /// <summary>
    /// Procesa la cache completa del detalle de expediente recibido por mensajeria.
    /// </summary>
    /// <param name="detalle">Respuesta detallada normalizada recibida desde GDEBA.</param>
    /// <param name="fechaConsulta">Fecha en que se obtuvo la respuesta desde GDEBA.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Tarea asincronica de procesamiento de cache.</returns>
    public async Task ConsolidarDetalleEnCacheAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken)
    {
        var numero = NumeroGdebaCompleto.Create(detalle.NumeroGdebaCompleto);
        var expediente = await _expedienteCacheReadStore.BuscarExpedienteParaDetalleAsync(numero.Valor, cancellationToken);
        var expedienteEsNuevo = expediente is null;
        expediente ??= CrearExpediente(numero);
        var trata = await ResolverTrataAsync(detalle.CodigoTrata, detalle.DescripcionTrata, cancellationToken);
        var documentos = await ResolverDocumentosAsync(detalle.Documentos, cancellationToken);

        ConsolidarCabecera(expediente, detalle, trata?.Id);
        ConsolidarDocumentos(expediente, detalle.Documentos, documentos, FuenteDeteccionGdeba.ConsultarExpedienteDetallado, fechaConsulta);
        ConsolidarAdjuntos(expediente, detalle.ArchivosAdjuntos, fechaConsulta);
        ConsolidarRelaciones(expediente, detalle.Relaciones, FuenteDeteccionGdeba.ConsultarExpedienteDetallado, fechaConsulta);

        MarcarDetalleConsultadoCorrectamente(expediente, fechaConsulta, fechaConsulta, CalcularVencimientoDiario(fechaConsulta), estaCompleto: true, tieneDatosParciales: false);

        RegistrarCambiosExpediente(expediente, expedienteEsNuevo);

        await ConfirmarCambiosAsync("ConsolidarDetalleEnCache", numero.Valor, cancellationToken);

        _expedienteRepository.AcceptChanges(expediente);
    }

    /// <summary>
    /// Consulta un expediente directamente contra GDEBA sin leer ni actualizar cache local.
    /// </summary>
    /// <param name="request">Datos necesarios para identificar el expediente solicitado.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Resultado directo de GDEBA, sin fecha de cache local.</returns>
    public async Task<ConsultarExpedienteSinCacheResult> ConsultarExpedienteSinCacheAsync(
        ConsultarExpedienteSinCacheRequest request,
        CancellationToken cancellationToken)
    {
        var numeroGdebaCompleto = NumeroGdebaCompleto.Create(request.NumeroGdebaCompleto);
        var expediente = await _gdebaExpedienteGateway.BuscarExpedienteAsync(numeroGdebaCompleto, cancellationToken);
        var resolvedAt = DateTimeOffset.Now;

        await RegistrarAuditoriaAsync(OperacionExpediente, numeroGdebaCompleto.Valor, FuenteRespuesta.Gdeba, expediente is not null, resolvedAt, cancellationToken);

        await ConfirmarCambiosAsync("ConsultarExpedienteSinCache", numeroGdebaCompleto.Valor, cancellationToken);

        return new ConsultarExpedienteSinCacheResult(expediente, FuenteRespuesta.Gdeba, resolvedAt);
    }

    #endregion

    #region Metodos privados del servicio

    /// <summary>
    /// Crea un expediente local nuevo.
    /// </summary>
    /// <param name="numero">Numero GDEBA completo validado.</param>
    /// <returns>Nueva entidad de expediente aun no registrada en el repositorio.</returns>
    private Expediente CrearExpediente(NumeroGdebaCompleto numero)
    {
        return new Expediente(numero.Valor);
    }

    private void RegistrarCambiosExpediente(Expediente expediente, bool esNuevo)
    {
        if (esNuevo)
        {
            _expedienteRepository.Insert(expediente);
        }
        else
        {
            _expedienteRepository.Update(expediente);
        }

        _expedienteRepository.ApplyChanges(expediente);
    }

    private void MarcarDetalleConsultadoCorrectamente(
        Expediente expediente,
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        DateTimeOffset? fechaVencimiento,
        bool estaCompleto,
        bool tieneDatosParciales)
    {
        expediente.MarcarDetalleConsultadoCorrectamente(fechaConsulta, fechaActualizacionLocal, fechaVencimiento, estaCompleto, tieneDatosParciales);

    }

    private void MarcarDetalleConsultadoConError(
        Expediente expediente,
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        string? error)
    {
        expediente.MarcarDetalleConsultadoConError(fechaConsulta, fechaActualizacionLocal, error);

    }

    private void MarcarHistorialConsultadoCorrectamente(
        Expediente expediente,
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        DateTimeOffset? fechaVencimiento,
        MovimientoExpediente? ultimoMovimientoDetectado,
        bool estaCompleto,
        bool tieneDatosParciales)
    {
        expediente.MarcarHistorialConsultadoCorrectamente(fechaConsulta, fechaActualizacionLocal, fechaVencimiento, ultimoMovimientoDetectado, estaCompleto, tieneDatosParciales);

    }

    private void MarcarHistorialConsultadoConError(
        Expediente expediente,
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        string? error)
    {
        expediente.MarcarHistorialConsultadoConError(fechaConsulta, fechaActualizacionLocal, error);

    }

    /// <summary>
    /// Consolida los datos de cabecera recibidos desde consultarExpedienteDetallado.
    /// </summary>
    /// <param name="expediente">Expediente local que sera actualizado.</param>
    /// <param name="detalle">Detalle recibido desde GDEBA.</param>
    /// <param name="trataId">Identificador local de la trata resuelta.</param>
    private static void ConsolidarCabecera(
        Expediente expediente,
        GdebaExpedienteDetalladoDto detalle,
        Guid? trataId)
    {
        expediente.AplicarCabeceraDetallada(
            trataId, detalle.Estado, detalle.SistemaOrigen, detalle.DescripcionTramite, detalle.FechaCaratulacion, detalle.UsuarioCaratulador, detalle.UsuarioDestino, expediente.SectorDestino, expediente.ReparticionActual);
    }

    /// <summary>
    /// Consolida los documentos vinculados al expediente.
    /// </summary>
    /// <param name="expediente">Expediente local que sera actualizado.</param>
    /// <param name="documentosDto">Documentos recibidos desde GDEBA.</param>
    /// <param name="documentos">Documentos locales resueltos por numero de actuacion.</param>
    /// <param name="fechaDeteccion">Fecha de deteccion de la respuesta.</param>
    private static void ConsolidarDocumentos(
        Expediente expediente,
        IReadOnlyCollection<GdebaDocumentoExpedienteDto> documentosDto,
        IReadOnlyDictionary<string, DocumentoGdeba> documentos,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        foreach (var documentoDto in documentosDto)
        {
            var numeroDocumento = NumeroGdebaCompleto.Create(documentoDto.NumeroActuacionCompleto);
            var documento = documentos[numeroDocumento.Valor];

            expediente.RegistrarDocumentoDetectado(
                documento, documentoDto.FechaVinculacion, documentoDto.OrdenRespuesta, documentoDto.UsuarioAsociacion, documentoDto.UsuarioGenerador, fuenteDeteccion, fechaDeteccion);
        }
    }

    /// <summary>
    /// Consolida los archivos adjuntos informados por GDEBA.
    /// </summary>
    /// <param name="expediente">Expediente local que sera actualizado.</param>
    /// <param name="archivosAdjuntos">Archivos adjuntos recibidos.</param>
    /// <param name="fechaDeteccion">Fecha de deteccion de la respuesta.</param>
    private static void ConsolidarAdjuntos(
        Expediente expediente,
        IReadOnlyCollection<string> archivosAdjuntos,
        DateTimeOffset fechaDeteccion)
    {
        foreach (var archivo in archivosAdjuntos)
        {
            expediente.RegistrarArchivoAdjuntoDetectado(archivo, FuenteDeteccionGdeba.ConsultarExpedienteDetallado, fechaDeteccion);
        }
    }

    /// <summary>
    /// Consolida las relaciones entre expedientes recibidas desde GDEBA.
    /// </summary>
    /// <param name="expediente">Expediente local que sera actualizado.</param>
    /// <param name="relaciones">Relaciones recibidas.</param>
    /// <param name="fechaDeteccion">Fecha de deteccion de la respuesta.</param>
    private static void ConsolidarRelaciones(
        Expediente expediente,
        IReadOnlyCollection<GdebaRelacionExpedienteDto> relaciones,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        foreach (var relacion in relaciones)
        {
            expediente.RegistrarRelacionDetectada(
                relacion.NumeroExpedienteRelacionado, ParsearTipoRelacion(relacion.TipoRelacion), expedienteRelacionadoId: null, relacion.CodigoTrata, relacion.DescripcionTrata, relacion.FechaRelacion, relacion.UsuarioRelacion, relacion.EsCabecera, fuenteDeteccion, fechaDeteccion);
        }
    }

    /// <summary>
    /// Resuelve una trata por codigo y actualiza su informacion descriptiva cuando viene informada por GDEBA.
    /// </summary>
    /// <param name="codigoTrata">Codigo de trata informado en la respuesta externa.</param>
    /// <param name="descripcionTrata">Descripcion de la trata informada en la respuesta externa.</param>
    /// <returns>Entidad de trata local cuando el codigo esta informado; en caso contrario, null.</returns>
    private async Task<TrataGdeba?> ResolverTrataAsync(
        string? codigoTrata,
        string? descripcionTrata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoTrata))
        {
            return null;
        }

        var codigo = codigoTrata.Trim();
        var trata = await _expedienteCacheReadStore.BuscarTrataPorCodigoAsync(codigo, cancellationToken);
        var trataEsNueva = trata is null;
        trata ??= new TrataGdeba(codigo);

        trata.ActualizarDatos(
            descripcionTrata, acronimoGedo: null, esAutomatica: null, esTrataManual: null, estado: null, idTrataGdeba: null, tipoReservaDescripcion: null, tipoReservaId: null, tipoReservaDescripcionTipoReserva: null);

        if (trataEsNueva)
        {
            _trataRepository.Insert(trata);
        }
        else
        {
            _trataRepository.Update(trata);
        }

        return trata;
    }

    /// <summary>
    /// Resuelve en bloque los documentos recibidos por numero de actuacion.
    /// </summary>
    /// <param name="documentosDto">Documentos recibidos desde GDEBA.</param>
    /// <returns>Documentos locales existentes o creados, indexados por numero de actuacion.</returns>
    private async Task<IReadOnlyDictionary<string, DocumentoGdeba>> ResolverDocumentosAsync(
        IReadOnlyCollection<GdebaDocumentoExpedienteDto> documentosDto,
        CancellationToken cancellationToken)
    {
        var documentosNormalizados = documentosDto
           .Select(x => new
            {
                Dto = x,
                Numero = NumeroGdebaCompleto.Create(x.NumeroActuacionCompleto).Valor
            })
           .ToArray();

        var documentos = await _expedienteCacheReadStore.BuscarDocumentosPorNumeroActuacionAsync(documentosNormalizados.Select(x => x.Numero), cancellationToken);

        var documentosResueltos = new Dictionary<string, DocumentoGdeba>(documentos, StringComparer.OrdinalIgnoreCase);

        foreach (var grupo in documentosNormalizados.GroupBy(x => x.Numero, StringComparer.OrdinalIgnoreCase))
        {
            var documentoEsNuevo = !documentosResueltos.TryGetValue(grupo.Key, out var documento);
            documento ??= new DocumentoGdeba(grupo.Key);

            foreach (var item in grupo)
            {
                documento.RegistrarMetadataParcial(item.Dto.TipoDocumentoCodigo, item.Dto.Referencia, item.Dto.FechaCreacion);
            }

            if (documentoEsNuevo)
            {
                _documentoRepository.Insert(documento);
                documentosResueltos[grupo.Key] = documento;
            }
            else
            {
                _documentoRepository.Update(documento);
            }
        }

        return documentosResueltos;
    }

    /// <summary>
    /// Convierte el texto de relacion recibido desde GDEBA al enumerativo local.
    /// </summary>
    /// <param name="value">Valor textual informado por el servicio externo.</param>
    /// <returns>Tipo de relacion reconocido o Asociado como valor por defecto.</returns>
    private static TipoRelacionExpediente ParsearTipoRelacion(string value)
    {
        return Enum.TryParse<TipoRelacionExpediente>(value, ignoreCase: true, out var tipoRelacion)
            ? tipoRelacion
            : TipoRelacionExpediente.Asociado;
    }

    /// <summary>
    /// Proyecta la entidad local de expediente detallado al contrato de salida de aplicacion.
    /// </summary>
    /// <param name="expediente">Expediente local que se desea exponer como DTO.</param>
    /// <returns>DTO detallado del expediente.</returns>
    private static ExpedienteDetalladoDto Mapear(Expediente expediente)
    {
        return new ExpedienteDetalladoDto(
            expediente.GdebaNumeroCompleto, expediente.Trata?.Codigo, expediente.Trata?.Descripcion, expediente.EstadoActual, expediente.SistemaOrigen, expediente.DescripcionTramite, expediente.FechaCaratulacion, expediente.UsuarioCaratulador, expediente.UsuarioDestino, expediente.SectorDestino, expediente.ReparticionActual, expediente.Documentos.OrderByDescending(x => x.OrdenRespuesta ?? 0).ThenByDescending(x => x.FechaVinculacion ?? x.Documento.FechaCreacion ?? DateTimeOffset.MinValue).ThenByDescending(x => x.Documento.NumeroActuacionCompleto).Select(x => new DocumentoExpedienteDto(x.Documento.NumeroActuacionCompleto, x.Documento.TipoDocumentoCodigo, x.Documento.Referencia, x.Documento.FechaCreacion, x.FechaVinculacion, x.UsuarioAsociacion, x.UsuarioGenerador, x.OrdenRespuesta)).ToArray(), expediente.ArchivosAdjuntos.Select(x => new ArchivoAdjuntoExpedienteDto(x.NombreArchivo)).ToArray(), expediente.Relaciones.Select(x => new RelacionExpedienteDto(x.NumeroExpedienteRelacionado, x.TipoRelacion.ToString(), x.EsCabecera, x.CodigoTrataRelacionado, x.DescripcionTrataRelacionado, x.FechaRelacion, x.UsuarioRelacion)).ToArray());
    }

    /// <summary>
    /// Proyecta una respuesta GDEBA detallada a una respuesta liviana para no bloquear por colecciones pesadas.
    /// </summary>
    /// <param name="detalle">Respuesta normalizada recibida desde GDEBA.</param>
    /// <returns>DTO de expediente con cabecera inmediata y colecciones vacias.</returns>
    private static ExpedienteDetalladoDto MapearRespuestaLiviana(GdebaExpedienteDetalladoDto detalle)
    {
        return new ExpedienteDetalladoDto(
            detalle.NumeroGdebaCompleto, detalle.CodigoTrata, detalle.DescripcionTrata, detalle.Estado, detalle.SistemaOrigen, detalle.DescripcionTramite, detalle.FechaCaratulacion, detalle.UsuarioCaratulador, detalle.UsuarioDestino, SectorDestino: null, ReparticionActual: null, Array.Empty<DocumentoExpedienteDto>(), Array.Empty<ArchivoAdjuntoExpedienteDto>(), Array.Empty<RelacionExpedienteDto>());
    }

    /// <summary>
    /// Proyecta los movimientos locales del expediente al contrato de salida de aplicacion.
    /// </summary>
    /// <param name="expediente">Expediente local que contiene los movimientos.</param>
    /// <returns>Coleccion ordenada de movimientos del expediente.</returns>
    private static IReadOnlyCollection<MovimientoExpedienteDto> MapearMovimientos(Expediente expediente)
    {
        return expediente.Movimientos
            .OrderBy(x => x.Orden)
            .Select(x => new MovimientoExpedienteDto(x.Orden, x.FechaOperacion, x.EstadoOrigen, x.EstadoDestino, x.UsuarioOrigen, x.UsuarioDestino, x.Motivo, x.ReparticionOrigen, x.ReparticionDestino, x.EsUltimoConocido))
            .ToArray();
    }

    private async Task<Expediente?> BuscarExpedienteCompletoAsync(
        string numeroGdebaCompleto,
        CancellationToken cancellationToken)
    {
        var numero = NumeroGdebaCompleto.Create(numeroGdebaCompleto);
        return await _expedienteCacheReadStore.BuscarExpedienteCompletoAsync(numero.Valor, cancellationToken);
    }

    private static CabeceraExpedienteDto MapearCabecera(ExpedienteDetalladoDto expediente)
    {
        return new CabeceraExpedienteDto(
            expediente.NumeroGdebaCompleto, expediente.CodigoTrata, expediente.DescripcionTrata, expediente.Estado, expediente.SistemaOrigen, expediente.DescripcionTramite, expediente.FechaCaratulacion, expediente.UsuarioCaratulador, expediente.UsuarioDestino, expediente.SectorDestino, expediente.ReparticionActual);
    }

    private static GdebaMovimientoExpedienteDto? ResolverUltimoMovimiento(
        IReadOnlyCollection<GdebaMovimientoExpedienteDto> movimientos)
    {
        if (movimientos.Count == 0)
        {
            return null;
        }

        return movimientos.Any(x => x.FechaOperacion.HasValue)
            ? movimientos
                .Where(x => x.FechaOperacion.HasValue)
                .MaxBy(x => x.FechaOperacion)
            : movimientos.MinBy(x => x.Orden);
    }

    private static ObtenerExpedienteRecursoResult<T> CrearResultadoRecurso<T>(
        string numeroGdebaCompleto,
        T? datos,
        FuenteRespuesta fuente,
        bool exitoso,
        DateTimeOffset resolvedAt,
        DateTimeOffset? cachedAt)
    {
        return new ObtenerExpedienteRecursoResult<T>(NumeroGdebaCompleto.Create(numeroGdebaCompleto).Valor, datos, fuente, exitoso, resolvedAt, cachedAt);
    }

    private static FuenteRespuesta CombinarFuente(
        FuenteRespuesta detalle,
        FuenteRespuesta historial)
    {
        if (detalle == FuenteRespuesta.Gdeba || historial == FuenteRespuesta.Gdeba)
        {
            return FuenteRespuesta.Gdeba;
        }

        if (detalle == FuenteRespuesta.FallbackCache || historial == FuenteRespuesta.FallbackCache)
        {
            return FuenteRespuesta.FallbackCache;
        }

        return FuenteRespuesta.Cache;
    }

    private static DateTimeOffset? Max(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return left > right ? left : right;
    }

    private static DateTimeOffset CalcularVencimientoDiario(DateTimeOffset fechaConsulta)
    {
        return fechaConsulta.AddDays(1);
    }

    /// <summary>
    /// Registra la auditoria funcional de una operacion realizada por una aplicacion consumidora.
    /// </summary>
    /// <param name="operacion">Nombre funcional de la operacion auditada.</param>
    /// <param name="recurso">Identificador del recurso consultado o afectado.</param>
    /// <param name="fuente">Fuente desde la que se resolvio la respuesta.</param>
    /// <param name="exitoso">Indica si la operacion funcional obtuvo una respuesta valida.</param>
    /// <param name="fecha">Fecha de resolucion de la operacion.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Tarea asincronica de registro de auditoria.</returns>
    private Task RegistrarAuditoriaAsync(
        string operacion,
        string recurso,
        FuenteRespuesta fuente,
        bool exitoso,
        DateTimeOffset fecha,
        CancellationToken cancellationToken)
    {
        return _auditoriaService.RegistrarAsync(
            new RegistrarAuditoriaRequest(_currentApplicationAccessor.Current.ApplicationId, operacion, recurso, _gdebaExecutionContext.Ambiente, fuente, exitoso, fecha), cancellationToken);
    }

    private async Task RegistrarFalloGdebaAsync(
        string operacion,
        string recurso,
        DateTimeOffset fecha,
        CancellationToken cancellationToken)
    {
        await RegistrarAuditoriaAsync(operacion, recurso, FuenteRespuesta.Gdeba, exitoso: false, fecha, cancellationToken);

        await ConfirmarCambiosAsync($"AuditarFallo{operacion}", recurso, cancellationToken);
    }

    /// <summary>
    /// Confirma los cambios pendientes de la unidad de trabajo y agrega contexto funcional ante errores de persistencia.
    /// </summary>
    /// <param name="operacion">Operacion funcional que intenta persistir cambios.</param>
    /// <param name="recurso">Recurso principal asociado a la operacion.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Tarea asincronica de persistencia.</returns>
    private async Task ConfirmarCambiosAsync(
        string operacion,
        string recurso,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al persistir cambios de la operacion {Operacion} para el recurso {Recurso}.", operacion, recurso);

            throw new InvalidOperationException($"No se pudieron persistir los cambios de la operacion '{operacion}' para el recurso '{recurso}'.", ex);
        }
    }

    #endregion
}
