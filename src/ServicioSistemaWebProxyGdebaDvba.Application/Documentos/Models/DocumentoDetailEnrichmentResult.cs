namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record DocumentoDetailEnrichmentResult(
    int Procesados,
    int Enriquecidos,
    int SinDatos,
    int Errores);
