using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;
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

    private readonly IGdebaDocumentoGateway _gdebaDocumentoGateway;
    private readonly ITrackableRepository<DocumentoGdeba> _documentoRepository;
    private readonly IRepository<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> _configuracionEnriquecimientoTemaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;
    private readonly ILogger<DocumentoDetailEnrichmentService> _logger;

    public DocumentoDetailEnrichmentService(
        IGdebaDocumentoGateway gdebaDocumentoGateway,
        ITrackableRepository<DocumentoGdeba> documentoRepository,
        IRepository<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> configuracionEnriquecimientoTemaRepository,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoriaService,
        IGdebaExecutionContext gdebaExecutionContext,
        ICurrentApplicationAccessor currentApplicationAccessor,
        ILogger<DocumentoDetailEnrichmentService> logger)
    {
        _gdebaDocumentoGateway = gdebaDocumentoGateway;
        _documentoRepository = documentoRepository;
        _configuracionEnriquecimientoTemaRepository = configuracionEnriquecimientoTemaRepository;
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
        try
        {
            var resultado = await this.EnriquecerDocumentoAsync(documento, contextoInvocacion, cancellationToken);
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
        var enriquecidos = 0;
        var sinDatos = 0;
        var errores = 0;

        foreach (var documento in documentos)
        {
            try
            {
                var resultado = await this.EnriquecerDocumentoAsync(documento, contextoInvocacion, cancellationToken);
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

        documento.EnriquecerDesdeDetalleDocumento(
            detalle.NumeroEspecial, detalle.TipoDocumentoCodigo,
            detalle.TipoDocumentoNombre, detalle.TipoDocumentoDescripcion,
            detalle.Referencia, detalle.FechaCreacion,
            detalle.ListaFirmantes, detalle.UrlArchivo,
            detalle.PuedeVerDocumento, DateTimeOffset.Now);

        foreach (var actividad in detalle.Historial)
        {
            documento.RegistrarActividadHistorial(
                actividad.IdGdeba, actividad.Actividad, actividad.FechaInicio,
                actividad.FechaFin, actividad.Usuario,
                actividad.NombreUsuario, actividad.WorkflowOrigen);
        }

        _documentoRepository.Update(documento);
        _documentoRepository.ApplyChanges(documento);
        return new DocumentoDetailEnrichmentItemResult(
            documento.Id,
            detalle.NumeroDocumento ?? documento.NumeroActuacionCompleto,
            DocumentoDetailEnrichmentItemStatus.Enriquecido);
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
}
