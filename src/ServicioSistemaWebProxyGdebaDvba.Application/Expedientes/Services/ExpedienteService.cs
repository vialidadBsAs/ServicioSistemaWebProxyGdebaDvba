using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Messaging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Security;
using ServicioSistemaWebProxyGdebaDvba.Application.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

public sealed class ExpedienteService : IExpedienteService
{
    private static readonly TimeSpan DefaultDetalleTtl = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan DefaultHistorialTtl = TimeSpan.FromDays(1);

    private readonly IExpedienteCacheReadStore _expedienteCacheReadStore;
    private readonly IGdebaExpedienteGateway _gdebaExpedienteGateway;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IExpedienteDetalleCacheDispatcher _expedienteDetalleCacheDispatcher;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IRepository<TrataGdeba> _trataRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpedienteService(
        IExpedienteCacheReadStore expedienteCacheReadStore,
        IGdebaExpedienteGateway gdebaExpedienteGateway,
        IGdebaExecutionContext gdebaExecutionContext,
        IAuditoriaService auditoriaService,
        IExpedienteDetalleCacheDispatcher expedienteDetalleCacheDispatcher,
        ICurrentApplicationAccessor currentApplicationAccessor,
        IRepository<Expediente> expedienteRepository,
        IRepository<DocumentoGdeba> documentoRepository,
        IRepository<TrataGdeba> trataRepository,
        IUnitOfWork unitOfWork)
    {
        _expedienteCacheReadStore = expedienteCacheReadStore;
        _gdebaExpedienteGateway = gdebaExpedienteGateway;
        _gdebaExecutionContext = gdebaExecutionContext;
        _auditoriaService = auditoriaService;
        _expedienteDetalleCacheDispatcher = expedienteDetalleCacheDispatcher;
        _currentApplicationAccessor = currentApplicationAccessor;
        _expedienteRepository = expedienteRepository;
        _documentoRepository = documentoRepository;
        _trataRepository = trataRepository;
        _unitOfWork = unitOfWork;
    }

    #region Metodos publicos del servicio

    /// <summary>
    /// Consulta la informacion basica de un expediente directamente contra el servicio autorizado de GDEBA.
    /// </summary>
    /// <param name="request">Datos necesarios para identificar el expediente solicitado.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Resultado de la consulta, incluyendo fuente de respuesta y fecha de resolucion.</returns>
    public async Task<ConsultarExpedienteResult> ConsultarAsync(
        ConsultarExpedienteRequest request,
        CancellationToken cancellationToken)
    {
        // Normaliza y valida el numero antes de enviarlo a cualquier integracion externa.
        var numeroGdebaCompleto = NumeroGdebaCompleto.Create(request.NumeroGdebaCompleto);

        // Consulta el servicio GDEBA sin aplicar cache local para esta operacion basica.
        var expediente = await _gdebaExpedienteGateway.BuscarExpedienteAsync(numeroGdebaCompleto, cancellationToken);
        var resolvedAt = DateTimeOffset.UtcNow;

        // Registra la auditoria funcional de la consulta realizada por la aplicacion consumidora.
        await RegistrarAuditoriaAsync(
            "ConsultarExpediente",
            numeroGdebaCompleto.Valor,
            FuenteRespuesta.Gdeba,
            expediente is not null,
            resolvedAt,
            cancellationToken);

        // Persiste en una unica unidad de trabajo los cambios acumulados por la operacion.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConsultarExpedienteResult(expediente, FuenteRespuesta.Gdeba, resolvedAt, CachedAt: null);
    }

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
        var resolvedAt = DateTimeOffset.UtcNow;
        var expediente = BuscarExpedienteLocal(numero.Valor);
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
            var detalle = await _gdebaExpedienteGateway.ConsultarExpedienteDetalladoAsync(numero, cancellationToken);
            if (detalle is null)
            {
                if (expediente is not null)
                {
                    // Conserva la ultima copia local como fallback y deja marcado el error de refresco.
                    expediente.MarcarDetalleConsultadoConError(
                        resolvedAt,
                        resolvedAt,
                        "GDEBA no devolvio detalle del expediente.");

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
                // Publica el procesamiento pesado de cache para no bloquear la respuesta del endpoint.
                await _expedienteDetalleCacheDispatcher.PublicarCacheDetalleAsync(
                    detalle,
                    resolvedAt,
                    cancellationToken);

                expedienteDto = MapearRespuestaLiviana(detalle);
                fuente = FuenteRespuesta.Gdeba;
                exitoso = true;
            }
        }

        // Centraliza la auditoria para todos los caminos funcionales de la consulta detallada.
        await RegistrarAuditoriaAsync(
            "ConsultarExpedienteDetallado",
            numero.Valor,
            fuente,
            exitoso,
            resolvedAt,
            cancellationToken);

        // Confirma en una sola transaccion los cambios de dominio y la auditoria funcional.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConsultarExpedienteDetalladoResult(
            expedienteDto,
            fuente,
            resolvedAt,
            cachedAt);
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
        var resolvedAt = DateTimeOffset.UtcNow;
        var expediente = BuscarExpedienteLocal(numero.Valor);
        IReadOnlyCollection<MovimientoExpedienteDto> movimientos;
        FuenteRespuesta fuente;
        bool exitoso;
        DateTimeOffset? cachedAt = null;

        if (!request.ForceRefresh && expediente?.PuedeResponderMovimientosDesdeCache(resolvedAt) == true)
        {
            // Responde desde cache cuando el historial fue sincronizado y sigue vigente.
            movimientos = MapearMovimientos(expediente);
            fuente = FuenteRespuesta.Cache;
            exitoso = true;
            cachedAt = expediente.HistorialCacheControl?.FechaUltimaActualizacionLocal;
        }
        else
        {
            // Consulta GDEBA solo cuando no hay movimientos vigentes o cuando se fuerza el refresco.
            var historial = await _gdebaExpedienteGateway.BuscarHistorialPasesExpedienteAsync(
                numero,
                cancellationToken);

            if (historial is null)
            {
                if (expediente is not null)
                {
                    // Conserva los movimientos locales como fallback ante una falta de respuesta externa.
                    expediente.MarcarHistorialConsultadoConError(
                        resolvedAt,
                        resolvedAt,
                        "GDEBA no devolvio historial de pases del expediente.");

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
                // Crea o actualiza el expediente y consolida los movimientos recibidos.
                expediente ??= CrearExpediente(numero);
                var ultimoMovimientoId = ConsolidarMovimientos(expediente, historial);

                // Marca la cache de historial con vigencia diaria para la consulta bajo demanda.
                expediente.MarcarHistorialConsultadoCorrectamente(
                    resolvedAt,
                    resolvedAt,
                    resolvedAt.Add(DefaultHistorialTtl),
                    ultimoMovimientoId,
                    estaCompleto: true,
                    tieneDatosParciales: false);

                movimientos = MapearMovimientos(expediente);
                fuente = FuenteRespuesta.Gdeba;
                exitoso = true;
            }
        }

        // Registra auditoria funcional una sola vez, independientemente de la fuente de respuesta.
        await RegistrarAuditoriaAsync(
            "ConsultarMovimientosExpediente",
            numero.Valor,
            fuente,
            exitoso,
            resolvedAt,
            cancellationToken);

        // Persiste movimientos/cache y auditoria en una unica unidad de trabajo.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConsultarMovimientosExpedienteResult(
            numero.Valor,
            movimientos,
            fuente,
            exitoso,
            resolvedAt,
            cachedAt);
    }

    /// <summary>
    /// Procesa la cache completa del detalle de expediente recibido por mensajeria.
    /// </summary>
    /// <param name="detalle">Respuesta detallada normalizada recibida desde GDEBA.</param>
    /// <param name="fechaConsulta">Fecha en que se obtuvo la respuesta desde GDEBA.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Tarea asincronica de procesamiento de cache.</returns>
    public Task ProcesarCacheDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken)
    {
        var numero = NumeroGdebaCompleto.Create(detalle.NumeroGdebaCompleto);
        var expediente = _expedienteCacheReadStore.BuscarExpedienteParaDetalle(numero.Valor) ??
            CrearExpediente(numero);
        var trata = ResolverTrata(detalle.CodigoTrata, detalle.DescripcionTrata);
        var documentos = ResolverDocumentos(detalle.Documentos);

        ConsolidarCabecera(expediente, detalle, trata?.Id);
        ConsolidarDocumentos(expediente, detalle.Documentos, documentos, fechaConsulta);
        ConsolidarAdjuntos(expediente, detalle.ArchivosAdjuntos, fechaConsulta);
        ConsolidarRelaciones(expediente, detalle.Relaciones, fechaConsulta);

        expediente.MarcarDetalleConsultadoCorrectamente(
            fechaConsulta,
            fechaConsulta,
            fechaConsulta.Add(DefaultDetalleTtl),
            estaCompleto: true,
            tieneDatosParciales: false);

        return _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Metodos privados del servicio

    /// <summary>
    /// Busca en la base local un expediente previamente cacheado por su numero GDEBA completo.
    /// </summary>
    /// <param name="numeroGdebaCompleto">Numero completo del expediente en formato GDEBA.</param>
    /// <returns>Entidad de expediente local cuando existe; en caso contrario, null.</returns>
    private Expediente? BuscarExpedienteLocal(string numeroGdebaCompleto)
    {
        return _expedienteRepository
            .Queryable()
            .FirstOrDefault(x => x.GdebaNumeroCompleto == numeroGdebaCompleto);
    }

    /// <summary>
    /// Crea un expediente local nuevo y lo registra en el repositorio para su posterior persistencia.
    /// </summary>
    /// <param name="numero">Numero GDEBA completo validado.</param>
    /// <returns>Nueva entidad de expediente incorporada al contexto de trabajo.</returns>
    private Expediente CrearExpediente(NumeroGdebaCompleto numero)
    {
        var expediente = new Expediente(numero.Valor);
        _expedienteRepository.Insert(expediente);
        return expediente;
    }

    /// <summary>
    /// Consolida en el expediente los movimientos recibidos desde el historial de pases GDEBA.
    /// </summary>
    /// <param name="expediente">Expediente local que sera actualizado.</param>
    /// <param name="historial">Movimientos devueltos por GDEBA.</param>
    /// <returns>Identificador del ultimo movimiento conocido cuando existe.</returns>
    private static Guid? ConsolidarMovimientos(
        Expediente expediente,
        IReadOnlyCollection<GdebaMovimientoExpedienteDto> historial)
    {
        MovimientoExpediente? ultimoMovimiento = null;
        var ultimoOrden = historial.Count == 0 ? 0 : historial.Max(x => x.Orden);

        foreach (var movimientoDto in historial.OrderBy(x => x.Orden))
        {
            var movimiento = expediente.RegistrarMovimientoDetectado(
                movimientoDto.Orden,
                movimientoDto.FechaOperacion,
                movimientoDto.EstadoOrigen,
                movimientoDto.EstadoDestino,
                movimientoDto.UsuarioOrigen,
                movimientoDto.UsuarioDestino,
                movimientoDto.Motivo,
                movimientoDto.ReparticionOrigen,
                movimientoDto.ReparticionDestino,
                esUltimoConocido: movimientoDto.Orden == ultimoOrden);

            if (movimiento.EsUltimoConocido)
            {
                ultimoMovimiento = movimiento;
            }
        }

        return ultimoMovimiento?.Id;
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
            trataId,
            detalle.Estado,
            detalle.SistemaOrigen,
            detalle.DescripcionTramite,
            detalle.FechaCaratulacion,
            detalle.UsuarioCaratulador,
            detalle.UsuarioDestino,
            sectorDestino: null,
            reparticionActual: null);
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
        DateTimeOffset fechaDeteccion)
    {
        foreach (var documentoDto in documentosDto)
        {
            var numeroDocumento = NumeroGdebaCompleto.Create(documentoDto.NumeroActuacionCompleto);
            var documento = documentos[numeroDocumento.Valor];

            expediente.RegistrarDocumentoDetectado(
                documento,
                documentoDto.FechaVinculacion,
                ordenRespuesta: null,
                documentoDto.UsuarioAsociacion,
                documentoDto.UsuarioGenerador,
                FuenteDeteccionGdeba.ConsultarExpedienteDetallado,
                fechaDeteccion);
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
            expediente.RegistrarArchivoAdjuntoDetectado(
                archivo,
                FuenteDeteccionGdeba.ConsultarExpedienteDetallado,
                fechaDeteccion);
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
        DateTimeOffset fechaDeteccion)
    {
        foreach (var relacion in relaciones)
        {
            expediente.RegistrarRelacionDetectada(
                relacion.NumeroExpedienteRelacionado,
                ParsearTipoRelacion(relacion.TipoRelacion),
                expedienteRelacionadoId: null,
                codigoTrataRelacionado: null,
                descripcionTrataRelacionado: null,
                fechaRelacion: null,
                usuarioRelacion: null,
                relacion.EsCabecera,
                FuenteDeteccionGdeba.ConsultarExpedienteDetallado,
                fechaDeteccion);
        }
    }

    /// <summary>
    /// Resuelve una trata por codigo y actualiza su informacion descriptiva cuando viene informada por GDEBA.
    /// </summary>
    /// <param name="codigoTrata">Codigo de trata informado en la respuesta externa.</param>
    /// <param name="descripcionTrata">Descripcion de la trata informada en la respuesta externa.</param>
    /// <returns>Entidad de trata local cuando el codigo esta informado; en caso contrario, null.</returns>
    private TrataGdeba? ResolverTrata(string? codigoTrata, string? descripcionTrata)
    {
        if (string.IsNullOrWhiteSpace(codigoTrata))
        {
            return null;
        }

        var codigo = codigoTrata.Trim();
        var trata = _expedienteCacheReadStore.BuscarTrataPorCodigo(codigo);
        if (trata is null)
        {
            trata = new TrataGdeba(codigo);
            _trataRepository.Insert(trata);
        }

        trata.ActualizarDatos(
            descripcionTrata,
            acronimoGedo: null,
            esAutomatica: null,
            esTrataManual: null,
            estado: null,
            idTrataGdeba: null,
            tipoReservaDescripcion: null,
            tipoReservaId: null,
            tipoReservaDescripcionTipoReserva: null);

        return trata;
    }

    /// <summary>
    /// Resuelve en bloque los documentos recibidos por numero de actuacion.
    /// </summary>
    /// <param name="documentosDto">Documentos recibidos desde GDEBA.</param>
    /// <returns>Documentos locales existentes o creados, indexados por numero de actuacion.</returns>
    private IReadOnlyDictionary<string, DocumentoGdeba> ResolverDocumentos(
        IReadOnlyCollection<GdebaDocumentoExpedienteDto> documentosDto)
    {
        var documentosNormalizados = documentosDto
            .Select(x => new
            {
                Dto = x,
                Numero = NumeroGdebaCompleto.Create(x.NumeroActuacionCompleto).Valor
            })
            .ToArray();

        var documentos = _expedienteCacheReadStore.BuscarDocumentosPorNumeroActuacion(
            documentosNormalizados.Select(x => x.Numero));

        var documentosResueltos = new Dictionary<string, DocumentoGdeba>(
            documentos,
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in documentosNormalizados)
        {
            if (!documentosResueltos.TryGetValue(item.Numero, out var documento))
            {
                documento = new DocumentoGdeba(item.Numero);
                _documentoRepository.Insert(documento);
                documentosResueltos[item.Numero] = documento;
            }

            documento.RegistrarMetadataParcial(
                item.Dto.TipoDocumentoCodigo,
                item.Dto.Referencia,
                item.Dto.FechaCreacion);
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
            expediente.GdebaNumeroCompleto,
            expediente.Trata?.Codigo,
            expediente.Trata?.Descripcion,
            expediente.EstadoActual,
            expediente.SistemaOrigen,
            expediente.DescripcionTramite,
            expediente.FechaCaratulacion,
            expediente.UsuarioCaratulador,
            expediente.UsuarioDestino,
            expediente.Documentos.Select(x => new DocumentoExpedienteDto(
                x.Documento.NumeroActuacionCompleto,
                x.Documento.TipoDocumentoCodigo,
                x.Documento.Referencia,
                x.Documento.FechaCreacion,
                x.FechaVinculacion,
                x.UsuarioAsociacion,
                x.UsuarioGenerador)).ToArray(),
            expediente.ArchivosAdjuntos.Select(x => new ArchivoAdjuntoExpedienteDto(x.NombreArchivo)).ToArray(),
            expediente.Relaciones.Select(x => new RelacionExpedienteDto(
                x.NumeroExpedienteRelacionado,
                x.TipoRelacion.ToString(),
                x.EsCabecera)).ToArray());
    }

    /// <summary>
    /// Proyecta una respuesta GDEBA detallada a una respuesta liviana para no bloquear por colecciones pesadas.
    /// </summary>
    /// <param name="detalle">Respuesta normalizada recibida desde GDEBA.</param>
    /// <returns>DTO de expediente con cabecera inmediata y colecciones vacias.</returns>
    private static ExpedienteDetalladoDto MapearRespuestaLiviana(GdebaExpedienteDetalladoDto detalle)
    {
        return new ExpedienteDetalladoDto(
            detalle.NumeroGdebaCompleto,
            detalle.CodigoTrata,
            detalle.DescripcionTrata,
            detalle.Estado,
            detalle.SistemaOrigen,
            detalle.DescripcionTramite,
            detalle.FechaCaratulacion,
            detalle.UsuarioCaratulador,
            detalle.UsuarioDestino,
            Array.Empty<DocumentoExpedienteDto>(),
            Array.Empty<ArchivoAdjuntoExpedienteDto>(),
            Array.Empty<RelacionExpedienteDto>());
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
            .Select(x => new MovimientoExpedienteDto(
                x.Orden,
                x.FechaOperacion,
                x.EstadoOrigen,
                x.EstadoDestino,
                x.UsuarioOrigen,
                x.UsuarioDestino,
                x.Motivo,
                x.ReparticionOrigen,
                x.ReparticionDestino,
                x.EsUltimoConocido))
            .ToArray();
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
            new RegistrarAuditoriaRequest(
                _currentApplicationAccessor.Current.ApplicationId,
                operacion,
                recurso,
                _gdebaExecutionContext.Ambiente,
                fuente,
                exitoso,
                fecha),
            cancellationToken);
    }

    #endregion
}
