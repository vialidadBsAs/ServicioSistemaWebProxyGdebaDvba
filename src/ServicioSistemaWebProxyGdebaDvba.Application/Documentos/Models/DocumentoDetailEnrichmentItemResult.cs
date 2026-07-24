namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record DocumentoDetailEnrichmentItemResult(
    Guid DocumentoId,
    string? NumeroDocumento,
    DocumentoDetailEnrichmentItemStatus Estado);
