using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
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

    private readonly IGdebaExpedienteGateway _gdebaExpedienteGateway;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IRepository<TrataGdeba> _trataRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpedienteService(
        IGdebaExpedienteGateway gdebaExpedienteGateway,
        IGdebaExecutionContext gdebaExecutionContext,
        IAuditoriaService auditoriaService,
        ICurrentApplicationAccessor currentApplicationAccessor,
        IRepository<Expediente> expedienteRepository,
        IRepository<DocumentoGdeba> documentoRepository,
        IRepository<TrataGdeba> trataRepository,
        IUnitOfWork unitOfWork)
    {
        _gdebaExpedienteGateway = gdebaExpedienteGateway;
        _gdebaExecutionContext = gdebaExecutionContext;
        _auditoriaService = auditoriaService;
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

        if (!request.ForceRefresh && PuedeResponderDesdeCache(expediente, resolvedAt))
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
                // Crea o actualiza el agregado local con la informacion detallada recibida desde GDEBA.
                expediente ??= CrearExpediente(numero);
                ConsolidarDetalle(expediente, detalle, resolvedAt);

                // Actualiza el estado de cache para futuras consultas bajo demanda.
                expediente.MarcarDetalleConsultadoCorrectamente(
                    resolvedAt,
                    resolvedAt,
                    resolvedAt.Add(DefaultDetalleTtl),
                    estaCompleto: true,
                    tieneDatosParciales: false);

                expedienteDto = Mapear(expediente);
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
    /// Determina si un expediente local puede responderse desde cache segun completitud y fecha de vencimiento.
    /// </summary>
    /// <param name="expediente">Expediente local a evaluar.</param>
    /// <param name="ahora">Fecha actual utilizada para comparar la vigencia de cache.</param>
    /// <returns>True cuando el cache esta completo y vigente; en caso contrario, false.</returns>
    private static bool PuedeResponderDesdeCache(Expediente? expediente, DateTimeOffset ahora)
    {
        return expediente?.CacheControl is not null &&
            expediente.CacheControl.EstaCompleto &&
            expediente.CacheControl.FechaVencimiento is not null &&
            expediente.CacheControl.FechaVencimiento > ahora;
    }

    /// <summary>
    /// Consolida la respuesta detallada de GDEBA dentro del agregado local de expediente.
    /// </summary>
    /// <param name="expediente">Expediente local que sera actualizado.</param>
    /// <param name="detalle">Datos detallados devueltos por GDEBA.</param>
    /// <param name="fechaDeteccion">Fecha en la que el proxy detecto los datos recibidos.</param>
    private void ConsolidarDetalle(
        Expediente expediente,
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaDeteccion)
    {
        // Resuelve o crea la trata antes de aplicar la cabecera del expediente.
        var trataId = ResolverTrata(detalle.CodigoTrata, detalle.DescripcionTrata)?.Id;

        // Aplica datos generales del expediente manteniendo la regla dentro del agregado.
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

        foreach (var documentoDto in detalle.Documentos)
        {
            // Resuelve el documento global y registra su vinculacion al expediente.
            var documento = ResolverDocumento(documentoDto);
            expediente.RegistrarDocumentoDetectado(
                documento,
                documentoDto.FechaVinculacion,
                ordenRespuesta: null,
                documentoDto.UsuarioAsociacion,
                documentoDto.UsuarioGenerador,
                FuenteDeteccionGdeba.ConsultarExpedienteDetallado,
                fechaDeteccion);
        }

        foreach (var archivo in detalle.ArchivosAdjuntos)
        {
            // Registra archivos adjuntos informados por la consulta detallada.
            expediente.RegistrarArchivoAdjuntoDetectado(
                archivo,
                FuenteDeteccionGdeba.ConsultarExpedienteDetallado,
                fechaDeteccion);
        }

        foreach (var relacion in detalle.Relaciones)
        {
            // Registra relaciones con otros expedientes sin resolver todavia el expediente relacionado.
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
        var trata = _trataRepository.Queryable().FirstOrDefault(x => x.Codigo == codigo);
        if (trata is null)
        {
            // Incorpora la trata al catalogo local cuando aparece por primera vez en GDEBA.
            trata = new TrataGdeba(codigo);
            _trataRepository.Insert(trata);
        }

        // Actualiza solo los datos conocidos por esta respuesta, sin suponer informacion no recibida.
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
    /// Resuelve un documento GDEBA por numero de actuacion y actualiza su metadata parcial.
    /// </summary>
    /// <param name="documentoDto">Documento informado dentro del detalle de expediente.</param>
    /// <returns>Documento local existente o creado para la actuacion indicada.</returns>
    private DocumentoGdeba ResolverDocumento(GdebaDocumentoExpedienteDto documentoDto)
    {
        // Normaliza el numero de actuacion antes de usarlo como identidad del documento.
        var numeroDocumento = NumeroGdebaCompleto.Create(documentoDto.NumeroActuacionCompleto);
        var documento = _documentoRepository
            .Queryable()
            .FirstOrDefault(x => x.NumeroActuacionCompleto == numeroDocumento.Valor);

        if (documento is null)
        {
            // Incorpora el documento global al catalogo local cuando aparece por primera vez.
            documento = new DocumentoGdeba(numeroDocumento.Valor);
            _documentoRepository.Insert(documento);
        }

        // Conserva metadata parcial detectada desde el expediente, sin asumir que es el detalle completo del documento.
        documento.RegistrarMetadataParcial(
            documentoDto.TipoDocumentoCodigo,
            documentoDto.Referencia,
            documentoDto.FechaCreacion);

        return documento;
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
