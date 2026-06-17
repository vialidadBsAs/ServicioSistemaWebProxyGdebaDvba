namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record DocumentoMetadataEnrichmentItemResult(
    Guid DocumentoId,
    string? NumeroDocumento,
    DocumentoMetadataEnrichmentItemStatus Estado);
