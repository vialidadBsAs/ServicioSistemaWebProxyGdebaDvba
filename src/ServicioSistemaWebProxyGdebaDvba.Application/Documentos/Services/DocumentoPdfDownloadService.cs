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

    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IGdebaDocumentoGateway _gdebaDocumentoGateway;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;

    public DocumentoPdfDownloadService(
        IRepository<DocumentoGdeba> documentoRepository,
        IGdebaDocumentoGateway gdebaDocumentoGateway,
        IAuditoriaService auditoriaService,
        IUnitOfWork unitOfWork,
        IGdebaExecutionContext gdebaExecutionContext,
        ICurrentApplicationAccessor currentApplicationAccessor)
    {
        _documentoRepository = documentoRepository;
        _gdebaDocumentoGateway = gdebaDocumentoGateway;
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

        if (string.IsNullOrWhiteSpace(documento.UrlArchivo) || documento.PuedeVerDocumento is false)
        {
            return new DocumentoPdfDescargaResult(true, false, documento.NumeroActuacionCompleto, null);
        }

        try
        {
            var contextoInvocacion = ContextoInvocacionGdeba.Crear(origenInvocacion);
            var pdf = await _gdebaDocumentoGateway.BuscarPdfPorNumeroAsync(documento.NumeroActuacionCompleto, contextoInvocacion, cancellationToken);
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
