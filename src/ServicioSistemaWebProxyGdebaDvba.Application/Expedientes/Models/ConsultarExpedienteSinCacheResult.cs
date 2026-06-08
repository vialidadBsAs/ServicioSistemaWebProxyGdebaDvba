using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarExpedienteSinCacheResult(
    ExpedienteGdebaDto? Expediente,
    FuenteRespuesta Fuente,
    DateTimeOffset ResolvedAt);
