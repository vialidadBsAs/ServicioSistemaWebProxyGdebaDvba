using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Services;

public sealed class DocumentoMetadataEnrichmentService
    : IDocumentoMetadataEnrichmentService
{
    private readonly IGdebaDocumentoGateway _gdebaDocumentoGateway;
    private readonly ITrackableRepository<DocumentoGdeba> _documentoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentoMetadataEnrichmentService> _logger;

    public DocumentoMetadataEnrichmentService(
        IGdebaDocumentoGateway gdebaDocumentoGateway,
        ITrackableRepository<DocumentoGdeba> documentoRepository,
        IUnitOfWork unitOfWork,
        ILogger<DocumentoMetadataEnrichmentService> logger)
    {
        _gdebaDocumentoGateway = gdebaDocumentoGateway;
        _documentoRepository = documentoRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DocumentoMetadataEnrichmentItemResult> EnriquecerDocumentoAsync(
        Guid documentoId,
        OrigenInvocacionGdeba origenInvocacion,
        CancellationToken cancellationToken)
    {
        var documento = await this.CargarDocumentoAsync(documentoId, cancellationToken);
        if (documento is null)
        {
            return new DocumentoMetadataEnrichmentItemResult(
                documentoId,
                null,
                DocumentoMetadataEnrichmentItemStatus.DocumentoNoEncontrado);
        }

        var contextoInvocacion = ContextoInvocacionGdeba.Crear(origenInvocacion);
        var resultado = await this.EnriquecerDocumentoAsync(
            documento,
            contextoInvocacion,
            cancellationToken);
        if (resultado.Estado == DocumentoMetadataEnrichmentItemStatus.Enriquecido)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return resultado;
    }

    public async Task<DocumentoMetadataEnrichmentResult> EnriquecerPendientesAsync(
        int loteMaximo,
        OrigenInvocacionGdeba origenInvocacion,
        CancellationToken cancellationToken)
    {
        var limite = Math.Max(1, loteMaximo);
        var documentos = await _documentoRepository
            .Query()
            .Include(x => x.Historial)
            .Where(x => !x.MetadataCompleta)
            .OrderBy(x => x.FechaUltimoEnriquecimiento ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.NumeroActuacionCompleto)
            .Take(limite)
            .SelectAsync(cancellationToken);
        var documentosProcesados = documentos.Count();

        if (documentosProcesados == 0)
        {
            return new DocumentoMetadataEnrichmentResult(0, 0, 0, 0);
        }

        var contextoInvocacion = ContextoInvocacionGdeba.Crear(origenInvocacion);
        var enriquecidos = 0;
        var sinDatos = 0;
        var errores = 0;

        foreach (var documento in documentos)
        {
            try
            {
                var resultado = await this.EnriquecerDocumentoAsync(
                    documento,
                    contextoInvocacion,
                    cancellationToken);
                switch (resultado.Estado)
                {
                    case DocumentoMetadataEnrichmentItemStatus.Enriquecido:
                        enriquecidos++;
                        break;

                    case DocumentoMetadataEnrichmentItemStatus.SinDatos:
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
                _logger.LogWarning(
                    ex,
                    "No se pudo enriquecer el documento {NumeroDocumento}.",
                    documento.NumeroActuacionCompleto);
            }
        }

        if (enriquecidos > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new DocumentoMetadataEnrichmentResult(
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

    private async Task<DocumentoMetadataEnrichmentItemResult> EnriquecerDocumentoAsync(
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
            return new DocumentoMetadataEnrichmentItemResult(
                documento.Id,
                documento.NumeroActuacionCompleto,
                DocumentoMetadataEnrichmentItemStatus.SinDatos);
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
        return new DocumentoMetadataEnrichmentItemResult(
            documento.Id,
            documento.NumeroActuacionCompleto,
            DocumentoMetadataEnrichmentItemStatus.Enriquecido);
    }
}
