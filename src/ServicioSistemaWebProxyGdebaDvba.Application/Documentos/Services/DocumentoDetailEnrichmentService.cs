using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Services;

public sealed class DocumentoDetailEnrichmentService
    : IDocumentoDetailEnrichmentService
{
    private const string OperacionEnriquecerDetalleDocumento = "EnriquecerDetalleDocumento";
    private const string OperacionBuscarDetallePorNumero = "buscarDetallePorNumero";
    private const string ServicioConsultaTipoDocumento = "ws_gdeba_consultaTipoDocumento";
    private const string OperacionConsultarTipoDocumento = "consultarTipoDocumento";

    private readonly IGdebaDocumentoGateway _gdebaDocumentoGateway;
    private readonly IGdebaTipoDocumentoGateway _gdebaTipoDocumentoGateway;
    private readonly ITrackableRepository<DocumentoGdeba> _documentoRepository;
    private readonly ITrackableRepository<TipoDocumentoGdeba> _tipoDocumentoRepository;
    private readonly IRepository<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> _configuracionEnriquecimientoTemaRepository;
    private readonly IConsultaCuotasGdeba _consultaCuotasGdeba;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;
    private readonly ILogger<DocumentoDetailEnrichmentService> _logger;

    public DocumentoDetailEnrichmentService(
        IGdebaDocumentoGateway gdebaDocumentoGateway,
        IGdebaTipoDocumentoGateway gdebaTipoDocumentoGateway,
        ITrackableRepository<DocumentoGdeba> documentoRepository,
        ITrackableRepository<TipoDocumentoGdeba> tipoDocumentoRepository,
        IRepository<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> configuracionEnriquecimientoTemaRepository,
        IConsultaCuotasGdeba consultaCuotasGdeba,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoriaService,
        IGdebaExecutionContext gdebaExecutionContext,
        ICurrentApplicationAccessor currentApplicationAccessor,
        ILogger<DocumentoDetailEnrichmentService> logger)
    {
        _gdebaDocumentoGateway = gdebaDocumentoGateway;
        _gdebaTipoDocumentoGateway = gdebaTipoDocumentoGateway;
        _documentoRepository = documentoRepository;
        _tipoDocumentoRepository = tipoDocumentoRepository;
        _configuracionEnriquecimientoTemaRepository = configuracionEnriquecimientoTemaRepository;
        _consultaCuotasGdeba = consultaCuotasGdeba;
        _unitOfWork = unitOfWork;
        _auditoriaService = auditoriaService;
        _gdebaExecutionContext = gdebaExecutionContext;
        _currentApplicationAccessor = currentApplicationAccessor;
        _logger = logger;
    }

    public async Task<DocumentoDetailEnrichmentItemResult> EnriquecerDocumentoAsync(
        Guid documentoId,
        OrigenInvocacionGdeba origenInvocacion,
        CancellationToken cancellationToken)
    {
        var documento = await this.CargarDocumentoAsync(documentoId, cancellationToken);
        if (documento is null)
        {
            return new DocumentoDetailEnrichmentItemResult(
                documentoId,
                null,
                DocumentoDetailEnrichmentItemStatus.DocumentoNoEncontrado);
        }

        var contextoInvocacion = ContextoInvocacionGdeba.Crear(origenInvocacion);
        var catalogoTiposDocumento = new CatalogoTiposDocumento();
        try
        {
            var resultado = await this.EnriquecerDocumentoAsync(documento, contextoInvocacion, catalogoTiposDocumento, cancellationToken);
            await this.RegistrarAuditoriaAsync(
                resultado.NumeroDocumento ?? documento.NumeroActuacionCompleto, origenInvocacion, exitoso: true,
                DocumentoDetailEnrichmentService.CrearMensajeResultado(resultado.Estado), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return resultado;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await this.RegistrarAuditoriaAsync(
                documento.NumeroActuacionCompleto, origenInvocacion, exitoso: false,
                DocumentoDetailEnrichmentService.CrearMensajeError(), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<DocumentoDetailEnrichmentResult> EnriquecerPendientesAsync(int loteMaximo, OrigenInvocacionGdeba origenInvocacion,
        CancellationToken cancellationToken)
    {
        var limite = Math.Max(1, loteMaximo);
        var configuracionesTema = await _configuracionEnriquecimientoTemaRepository
            .Query()
            .Where(x => x.Habilitado)
            .OrderBy(x => x.Prioridad)
            .ThenBy(x => x.TemaExpedienteId)
            .SelectAsync(cancellationToken);
        if (!configuracionesTema.Any())
        {
            _logger.LogInformation("Enriquecimiento de detalle documental omitido porque no hay temas habilitados en la configuracion local.");
            return new DocumentoDetailEnrichmentResult(0, 0, 0, 0);
        }

        var documentos = await this.CargarDocumentosPendientesPorTemaAsync(configuracionesTema, limite, cancellationToken);
        var documentosProcesados = documentos.Count();

        if (documentosProcesados == 0)
        {
            return new DocumentoDetailEnrichmentResult(0, 0, 0, 0);
        }

        var contextoInvocacion = ContextoInvocacionGdeba.Crear(origenInvocacion);
        var catalogoTiposDocumento = new CatalogoTiposDocumento();
        var enriquecidos = 0;
        var sinDatos = 0;
        var errores = 0;

        foreach (var documento in documentos)
        {
            try
            {
                var resultado = await this.EnriquecerDocumentoAsync(documento, contextoInvocacion, catalogoTiposDocumento, cancellationToken);
                await this.RegistrarAuditoriaAsync(
                    resultado.NumeroDocumento ?? documento.NumeroActuacionCompleto, origenInvocacion, exitoso: true,
                    DocumentoDetailEnrichmentService.CrearMensajeResultado(resultado.Estado), cancellationToken);
                switch (resultado.Estado)
                {
                    case DocumentoDetailEnrichmentItemStatus.Enriquecido:
                        enriquecidos++;
                        break;

                    case DocumentoDetailEnrichmentItemStatus.SinDatos:
                        sinDatos++;
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errores++;
                await this.RegistrarAuditoriaAsync(
                    documento.NumeroActuacionCompleto, origenInvocacion, exitoso: false,
                    DocumentoDetailEnrichmentService.CrearMensajeError(), cancellationToken);
                _logger.LogWarning(
                    ex,
                    "No se pudo enriquecer el documento {NumeroDocumento}.",
                    documento.NumeroActuacionCompleto);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DocumentoDetailEnrichmentResult(
            documentosProcesados,
            enriquecidos,
            sinDatos,
            errores);
    }

    private async Task<DocumentoGdeba?> CargarDocumentoAsync(
        Guid documentoId,
        CancellationToken cancellationToken)
    {
        return (await _documentoRepository
            .Query()
            .Include(x => x.Historial)
            .Where(x => x.Id == documentoId)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
    }

    private async Task<DocumentoDetailEnrichmentItemResult> EnriquecerDocumentoAsync(
        DocumentoGdeba documento,
        ContextoInvocacionGdeba contextoInvocacion,
        CatalogoTiposDocumento catalogoTiposDocumento,
        CancellationToken cancellationToken)
    {
        var detalle = await _gdebaDocumentoGateway.BuscarDetallePorNumeroAsync(
            documento.NumeroActuacionCompleto,
            contextoInvocacion,
            cancellationToken);
        if (detalle is null)
        {
            return new DocumentoDetailEnrichmentItemResult(
                documento.Id,
                documento.NumeroActuacionCompleto,
                DocumentoDetailEnrichmentItemStatus.SinDatos);
        }

        var tipoDocumento = await this.ResolverTipoDocumentoAsync(detalle.TipoDocumentoCodigo, contextoInvocacion, catalogoTiposDocumento, cancellationToken);
        documento.EnriquecerDesdeDetalleDocumento(
            detalle.NumeroEspecial, detalle.TipoDocumentoCodigo,
            detalle.Referencia, detalle.FechaCreacion, detalle.ListaFirmantes, detalle.UrlArchivo,
            detalle.PuedeVerDocumento, DateTimeOffset.Now);
        if (tipoDocumento is not null)
        {
            documento.AsignarTipoDocumento(tipoDocumento);
        }

        foreach (var actividad in detalle.Historial)
        {
            documento.RegistrarActividadHistorial(
                actividad.IdGdeba, actividad.Actividad, actividad.FechaInicio,
                actividad.FechaFin, actividad.Usuario,
                actividad.NombreUsuario, actividad.WorkflowOrigen);
        }

        _documentoRepository.Update(documento);
        _documentoRepository.ApplyChanges(documento);
        var ultimaActividad = documento.Historial
            .OrderByDescending(x => x.FechaFin ?? x.FechaInicio)
            .ThenByDescending(x => x.IdGdeba)
            .FirstOrDefault();
        return new DocumentoDetailEnrichmentItemResult(
            documento.Id,
            detalle.NumeroDocumento ?? documento.NumeroActuacionCompleto,
            DocumentoDetailEnrichmentItemStatus.Enriquecido,
            ultimaActividad?.Actividad,
            ultimaActividad?.FechaFin ?? ultimaActividad?.FechaInicio,
            documento.UrlArchivo,
            documento.PuedeVerDocumento,
            documento.TipoDocumentoCodigo,
            tipoDocumento?.Nombre,
            tipoDocumento?.Familia,
            documento.Referencia,
            documento.FechaCreacion,
            documento.MetadataCompleta);
    }

    private async Task<TipoDocumentoGdeba?> ResolverTipoDocumentoAsync(string? codigoTipoDocumento,
        ContextoInvocacionGdeba contextoInvocacion, CatalogoTiposDocumento catalogoTiposDocumento,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoTipoDocumento))
        {
            return null;
        }

        var codigoNormalizado = codigoTipoDocumento.Trim().ToUpperInvariant();
        if (catalogoTiposDocumento.TiposPorCodigo.TryGetValue(codigoNormalizado, out var tipoEnMemoria))
        {
            return tipoEnMemoria;
        }

        var tipoDocumento = (await _tipoDocumentoRepository
            .Query()
            .Where(x => x.Codigo == codigoNormalizado)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (tipoDocumento is not null)
        {
            catalogoTiposDocumento.TiposPorCodigo[codigoNormalizado] = tipoDocumento;
            return tipoDocumento;
        }

        var consultasDisponibles = await this.ResolverConsultasTipoDocumentoDisponiblesAsync(catalogoTiposDocumento, cancellationToken);
        if (consultasDisponibles <= 0)
        {
            catalogoTiposDocumento.TiposPorCodigo[codigoNormalizado] = null;
            _logger.LogInformation(
                "No se consulta el tipo documental {CodigoTipoDocumento} porque se alcanzo la cuota diaria de consultarTipoDocumento.",
                codigoNormalizado);
            return null;
        }

        catalogoTiposDocumento.ConsultasDisponibles--;
        var detalleTipoDocumento = await _gdebaTipoDocumentoGateway.ConsultarTipoDocumentoAsync(
            codigoNormalizado, contextoInvocacion, cancellationToken);
        if (detalleTipoDocumento is null)
        {
            catalogoTiposDocumento.TiposPorCodigo[codigoNormalizado] = null;
            return null;
        }

        var codigoCatalogo = string.IsNullOrWhiteSpace(detalleTipoDocumento.Acronimo)
            ? codigoNormalizado
            : detalleTipoDocumento.Acronimo.Trim().ToUpperInvariant();
        var nombre = string.IsNullOrWhiteSpace(detalleTipoDocumento.Nombre)
            ? detalleTipoDocumento.Descripcion ?? codigoCatalogo
            : detalleTipoDocumento.Nombre;
        tipoDocumento = new TipoDocumentoGdeba(codigoCatalogo, nombre);
        tipoDocumento.ActualizarMetadata(
            nombre,
            detalleTipoDocumento.CodigoTipoDocumentoGdeba,
            detalleTipoDocumento.Descripcion,
            detalleTipoDocumento.Familia,
            detalleTipoDocumento.TipoProduccion,
            detalleTipoDocumento.Estado,
            detalleTipoDocumento.EsAutomatica,
            detalleTipoDocumento.EsComunicable,
            detalleTipoDocumento.EsConfidencial,
            detalleTipoDocumento.EsEmbebido,
            detalleTipoDocumento.EsEspecial,
            detalleTipoDocumento.EsFirmaConjunta,
            detalleTipoDocumento.EsFirmaExterna,
            detalleTipoDocumento.EsManual,
            detalleTipoDocumento.EsNotificable,
            detalleTipoDocumento.TieneTemplate,
            detalleTipoDocumento.TieneToken,
            esResolucion: false,
            activo: !string.Equals(detalleTipoDocumento.Estado, "BAJA", StringComparison.OrdinalIgnoreCase));
        _tipoDocumentoRepository.Insert(tipoDocumento);
        catalogoTiposDocumento.TiposPorCodigo[codigoNormalizado] = tipoDocumento;
        catalogoTiposDocumento.TiposPorCodigo[codigoCatalogo] = tipoDocumento;
        return tipoDocumento;
    }

    private async Task<int> ResolverConsultasTipoDocumentoDisponiblesAsync(CatalogoTiposDocumento catalogoTiposDocumento,
        CancellationToken cancellationToken)
    {
        if (catalogoTiposDocumento.ConsultasDisponibles.HasValue)
        {
            return catalogoTiposDocumento.ConsultasDisponibles.Value;
        }

        var cuotas = await _consultaCuotasGdeba.ConsultarCuotasAsync(DateOnly.FromDateTime(DateTime.Now), cancellationToken);
        var cuotaTipoDocumento = cuotas.Operaciones.FirstOrDefault(x =>
            string.Equals(x.Servicio, DocumentoDetailEnrichmentService.ServicioConsultaTipoDocumento, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Operacion, DocumentoDetailEnrichmentService.OperacionConsultarTipoDocumento, StringComparison.OrdinalIgnoreCase));
        catalogoTiposDocumento.ConsultasDisponibles = cuotaTipoDocumento?.LimiteDiario is int limiteDiario
            ? Math.Max(0, limiteDiario - cuotaTipoDocumento.Total)
            : 0;
        return catalogoTiposDocumento.ConsultasDisponibles.Value;
    }

    private Task RegistrarAuditoriaAsync(
        string recurso,
        OrigenInvocacionGdeba origenInvocacion,
        bool exitoso,
        string mensaje,
        CancellationToken cancellationToken)
    {
        return _auditoriaService.RegistrarAsync(
            new RegistrarAuditoriaRequest(
                _currentApplicationAccessor.Current.ApplicationId,
                DocumentoDetailEnrichmentService.OperacionEnriquecerDetalleDocumento,
                DocumentoDetailEnrichmentService.OperacionBuscarDetallePorNumero,
                recurso,
                _gdebaExecutionContext.Ambiente,
                FuenteRespuesta.Gdeba,
                exitoso,
                $"Origen: {origenInvocacion}. {mensaje}",
                DateTimeOffset.Now),
            cancellationToken);
    }

    private async Task<IReadOnlyCollection<DocumentoGdeba>> CargarDocumentosPendientesPorTemaAsync(
        IEnumerable<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> configuracionesTema, int limite, CancellationToken cancellationToken)
    {
        var documentos = new List<DocumentoGdeba>();
        var idsDocumentosSeleccionados = new HashSet<Guid>();

        foreach (var configuracionTema in configuracionesTema)
        {
            var cantidadRestante = limite - documentos.Count;
            if (cantidadRestante == 0)
            {
                break;
            }

            IQuery<DocumentoGdeba> consultaDocumentos = _documentoRepository
                .Query()
                .Include(x => x.Historial)
                .Where(x => !x.MetadataCompleta)
                .Where(x => x.Expedientes.Any(vinculo =>
                    vinculo.Expediente.Trata != null &&
                    vinculo.Expediente.Trata.TemasExpediente.Any(asignacion =>
                        asignacion.TemaExpedienteId == configuracionTema.TemaExpedienteId)));
            if (idsDocumentosSeleccionados.Count > 0)
            {
                consultaDocumentos = consultaDocumentos.Where(x => !idsDocumentosSeleccionados.Contains(x.Id));
            }

            var documentosTema = await consultaDocumentos
                .OrderBy(x => x.FechaUltimoEnriquecimiento ?? DateTimeOffset.MinValue)
                .ThenBy(x => x.NumeroActuacionCompleto)
                .Take(cantidadRestante)
                .SelectAsync(cancellationToken);
            foreach (var documento in documentosTema)
            {
                if (idsDocumentosSeleccionados.Add(documento.Id))
                {
                    documentos.Add(documento);
                }
            }
        }

        return documentos;
    }

    private static string CrearMensajeResultado(DocumentoDetailEnrichmentItemStatus estado)
    {
        return estado switch
        {
            DocumentoDetailEnrichmentItemStatus.Enriquecido => "Detalle documental incorporado.",
            DocumentoDetailEnrichmentItemStatus.SinDatos => "GDEBA no devolvio detalle documental.",
            DocumentoDetailEnrichmentItemStatus.DocumentoNoEncontrado => "El documento no existe localmente.",
            _ => "El enriquecimiento documental finalizo con un estado no reconocido."
        };
    }

    private static string CrearMensajeError()
    {
        return "No se pudo completar el enriquecimiento documental. Consulte el registro tecnico de la invocacion GDEBA.";
    }

    private sealed class CatalogoTiposDocumento
    {
        public Dictionary<string, TipoDocumentoGdeba?> TiposPorCodigo { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int? ConsultasDisponibles { get; set; }
    }
}
