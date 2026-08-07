namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record DocumentoDetailEnrichmentItemResult(
    Guid DocumentoId,
    string? NumeroDocumento,
    DocumentoDetailEnrichmentItemStatus Estado,
    string? UltimaActividad = null,
    DateTimeOffset? FechaUltimaActividad = null,
    string? UrlArchivo = null,
    bool? PuedeVerDocumento = null);
