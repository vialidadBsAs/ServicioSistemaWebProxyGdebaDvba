namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record GdebaTipoDocumentoDto(
    string Acronimo,
    string? CodigoTipoDocumentoGdeba,
    string? Nombre,
    string? Descripcion,
    string? Familia,
    string? TipoProduccion,
    string? Estado,
    bool? EsAutomatica,
    bool? EsComunicable,
    bool? EsConfidencial,
    bool? EsEmbebido,
    bool? EsEspecial,
    bool? EsFirmaConjunta,
    bool? EsFirmaExterna,
    bool? EsManual,
    bool? EsNotificable,
    bool? TieneTemplate,
    bool? TieneToken);
