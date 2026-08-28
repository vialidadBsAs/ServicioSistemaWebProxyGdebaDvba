using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Services;

public sealed class DocumentoPdfDownloadService : IDocumentoPdfDownloadService
{
    private const string OperacionDescargarPdfDocumento = "DescargarPdfDocumento";
    private const string OperacionBuscarPdfPorNumero = "buscarPDFPorNumero";

    private const string MotivoDocumentoNoVisible = "GDEBA no permite visualizar este documento.";

    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IGdebaDocumentoGateway _gdebaDocumentoGateway;
    private readonly IDocumentoDetailEnrichmentService _documentoDetailEnrichmentService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;

    public DocumentoPdfDownloadService(
        IRepository<DocumentoGdeba> documentoRepository,
        IGdebaDocumentoGateway gdebaDocumentoGateway,
        IDocumentoDetailEnrichmentService documentoDetailEnrichmentService,
        IAuditoriaService auditoriaService,
        IUnitOfWork unitOfWork,
        IGdebaExecutionContext gdebaExecutionContext,
        ICurrentApplicationAccessor currentApplicationAccessor)
    {
        _documentoRepository = documentoRepository;
        _gdebaDocumentoGateway = gdebaDocumentoGateway;
        _documentoDetailEnrichmentService = documentoDetailEnrichmentService;
        _auditoriaService = auditoriaService;
        _unitOfWork = unitOfWork;
        _gdebaExecutionContext = gdebaExecutionContext;
        _currentApplicationAccessor = currentApplicationAccessor;
    }

    public async Task<DocumentoPdfDescargaResult> DescargarPdfAsync(Guid documentoId, OrigenInvocacionGdeba origenInvocacion, CancellationToken cancellationToken)
    {
        var documento = (await _documentoRepository.Query().Where(x => x.Id == documentoId).Take(1).SelectAsync(cancellationToken)).SingleOrDefault();
        if (documento is null)
        {
            return new DocumentoPdfDescargaResult(false, false, null, null);
        }

        if (documento.PuedeVerDocumento is false)
        {
            return new DocumentoPdfDescargaResult(true, false, documento.NumeroActuacionCompleto, null, DocumentoPdfDownloadService.MotivoDocumentoNoVisible);
        }

        // Enriquecimiento a demanda: ver el documento incluye completar su metadata si todavia falta.
        var metadataPrevia = documento.MetadataCompleta;
        if (!metadataPrevia)
        {
            var enriquecimiento = await this.EnriquecerAsync(documentoId, origenInvocacion, cancellationToken);
            if (enriquecimiento is not null)
            {
                return enriquecimiento;
            }
        }

        try
        {
            var contextoInvocacion = ContextoInvocacionGdeba.Crear(origenInvocacion);
            var pdf = await _gdebaDocumentoGateway.BuscarPdfPorNumeroAsync(documento.NumeroActuacionCompleto, contextoInvocacion, cancellationToken);
            if (pdf is null && metadataPrevia)
            {
                // Auto-reparacion: la metadata previa pudo quedar vieja; se refresca una unica vez y se reintenta.
                var enriquecimiento = await this.EnriquecerAsync(documentoId, origenInvocacion, cancellationToken);
                if (enriquecimiento is not null)
                {
                    return enriquecimiento;
                }

                pdf = await _gdebaDocumentoGateway.BuscarPdfPorNumeroAsync(documento.NumeroActuacionCompleto, contextoInvocacion, cancellationToken);
            }

            if (pdf is null)
            {
                await this.RegistrarAuditoriaAsync(documento.NumeroActuacionCompleto, origenInvocacion, exitoso: false, "GDEBA no devolvio el archivo PDF.", cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return new DocumentoPdfDescargaResult(true, false, documento.NumeroActuacionCompleto, null);
            }

            await this.RegistrarAuditoriaAsync(documento.NumeroActuacionCompleto, origenInvocacion, exitoso: true, "PDF descargado desde GDEBA.", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DocumentoPdfDescargaResult(true, true, pdf.NumeroDocumento, pdf.Contenido);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await this.RegistrarAuditoriaAsync(documento.NumeroActuacionCompleto, origenInvocacion, exitoso: false, "No se pudo descargar el PDF desde GDEBA.", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Enriquece la metadata del documento y devuelve un resultado terminal si la descarga no debe continuar; null si puede seguir.
    /// </summary>
    private async Task<DocumentoPdfDescargaResult?> EnriquecerAsync(Guid documentoId, OrigenInvocacionGdeba origenInvocacion, CancellationToken cancellationToken)
    {
        var enriquecimiento = await _documentoDetailEnrichmentService.EnriquecerDocumentoAsync(documentoId, origenInvocacion, cancellationToken);
        if (enriquecimiento.Estado == DocumentoDetailEnrichmentItemStatus.DocumentoNoEncontrado)
        {
            return new DocumentoPdfDescargaResult(false, false, null, null);
        }

        if (enriquecimiento.PuedeVerDocumento is false)
        {
            return new DocumentoPdfDescargaResult(true, false, enriquecimiento.NumeroDocumento, null, DocumentoPdfDownloadService.MotivoDocumentoNoVisible);
        }

        return null;
    }

    private Task RegistrarAuditoriaAsync(string recurso, OrigenInvocacionGdeba origenInvocacion, bool exitoso, string mensaje, CancellationToken cancellationToken)
    {
        return _auditoriaService.RegistrarAsync(
            new RegistrarAuditoriaRequest(
                _currentApplicationAccessor.Current.ApplicationId,
                DocumentoPdfDownloadService.OperacionDescargarPdfDocumento,
                DocumentoPdfDownloadService.OperacionBuscarPdfPorNumero,
                recurso,
                _gdebaExecutionContext.Ambiente,
                FuenteRespuesta.Gdeba,
                exitoso,
                $"Origen: {origenInvocacion}. {mensaje}",
                DateTimeOffset.Now),
            cancellationToken);
    }
}
