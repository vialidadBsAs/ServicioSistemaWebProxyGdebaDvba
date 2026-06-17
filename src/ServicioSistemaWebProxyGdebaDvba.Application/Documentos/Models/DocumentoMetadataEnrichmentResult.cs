namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record DocumentoMetadataEnrichmentResult(
    int Procesados,
    int Enriquecidos,
    int SinDatos,
    int Errores);
