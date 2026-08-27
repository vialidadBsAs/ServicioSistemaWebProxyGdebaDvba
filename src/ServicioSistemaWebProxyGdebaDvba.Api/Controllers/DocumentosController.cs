using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Api.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Authorize(Policy = SeguridadInstitucional.PoliticaAccesoExpedientes)]
[Route("api/gdeba/documentos")]
[ServiceFilter(typeof(Filters.RequiereUsuarioGdebaFilter))]
public sealed class DocumentosController : ControllerBase
{
    private readonly IDocumentoDetailEnrichmentService _documentoDetailEnrichmentService;
    private readonly IDocumentoPdfDownloadService _documentoPdfDownloadService;

    public DocumentosController(
        IDocumentoDetailEnrichmentService documentoDetailEnrichmentService,
        IDocumentoPdfDownloadService documentoPdfDownloadService)
    {
        _documentoDetailEnrichmentService = documentoDetailEnrichmentService;
        _documentoPdfDownloadService = documentoPdfDownloadService;
    }

    [HttpPost("{documentoId:guid}/detalle")]
    public async Task<IActionResult> EnriquecerDetalle(Guid documentoId, CancellationToken cancellationToken)
    {
        var resultado = await _documentoDetailEnrichmentService.EnriquecerDocumentoAsync(
            documentoId, OrigenInvocacionGdeba.Interactiva, cancellationToken);
        if (resultado.Estado == DocumentoDetailEnrichmentItemStatus.DocumentoNoEncontrado)
        {
            return this.NotFound();
        }

        return this.Ok(DocumentoDetalleEnriquecidoResponse.Create(resultado));
    }

    [HttpGet("{documentoId:guid}/pdf")]
    public async Task<IActionResult> DescargarPdf(Guid documentoId, CancellationToken cancellationToken)
    {
        var resultado = await _documentoPdfDownloadService.DescargarPdfAsync(documentoId, OrigenInvocacionGdeba.Interactiva, cancellationToken);
        if (!resultado.DocumentoEncontrado)
        {
            return this.NotFound();
        }

        if (!resultado.DisponibleParaDescarga || resultado.Contenido is null || string.IsNullOrWhiteSpace(resultado.NumeroDocumento))
        {
            return this.Conflict("El archivo PDF no esta disponible para este documento.");
        }

        return this.File(resultado.Contenido, "application/pdf", $"{resultado.NumeroDocumento}.pdf");
    }
}
