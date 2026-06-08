using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarExpedienteDetalladoResult(
    ExpedienteDetalladoDto? Expediente,
    FuenteRespuesta Fuente,
    DateTimeOffset ResolvedAt,
    DateTimeOffset? CachedAt);
