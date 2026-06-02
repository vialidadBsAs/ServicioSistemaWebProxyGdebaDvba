using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarExpedienteResult(
    ExpedienteGdebaDto? Expediente,
    FuenteRespuesta Fuente,
    DateTimeOffset ResolvedAt,
    DateTimeOffset? CachedAt);
