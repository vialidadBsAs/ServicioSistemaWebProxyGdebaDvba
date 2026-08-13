using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Models;

public sealed record DocumentoDetalleEnriquecidoResponse(
    Guid DocumentoId,
    string? NumeroDocumento,
    string Estado,
    string? UltimaActividad,
    DateTimeOffset? FechaUltimaActividad,
    string? UrlArchivo,
    bool? PuedeVerDocumento,
    string? CodigoTipoDocumento,
    string? NombreTipoDocumento,
    string? FamiliaTipoDocumento,
    string? Referencia,
    DateTimeOffset? FechaCreacion,
    bool MetadataCompleta)
{
    public static DocumentoDetalleEnriquecidoResponse Create(DocumentoDetailEnrichmentItemResult resultado)
    {
        return new DocumentoDetalleEnriquecidoResponse(
            resultado.DocumentoId,
            resultado.NumeroDocumento,
            resultado.Estado.ToString(),
            resultado.UltimaActividad,
            resultado.FechaUltimaActividad,
            resultado.UrlArchivo,
            resultado.PuedeVerDocumento,
            resultado.CodigoTipoDocumento,
            resultado.NombreTipoDocumento,
            resultado.FamiliaTipoDocumento,
            resultado.Referencia,
            resultado.FechaCreacion,
            resultado.MetadataCompleta);
    }
}
