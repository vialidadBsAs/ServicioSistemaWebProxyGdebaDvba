namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

/// <summary>
/// Resultado de una tanda de completado del catalogo: cuantos tipos sin sigla habia al empezar, cuantos quedaron confirmados,
/// cuantos GDEBA no reconocio (se desactivan), cuantos fallaron en GDEBA (se reintentan en otra tanda), cuantos siguen pendientes
/// y si la tanda se corto porque GDEBA no estaba accesible.
/// </summary>
public sealed record CompletadoCatalogoTiposDocumentoDto(
    int PendientesAlInicio,
    int Confirmados,
    int Desactivados,
    int ConError,
    int PendientesRestantes,
    bool Interrumpido,
    string? MotivoInterrupcion);
