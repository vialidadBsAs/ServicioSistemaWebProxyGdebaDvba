using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarMovimientosExpedienteResult(
    string NumeroGdebaCompleto,
    IReadOnlyCollection<MovimientoExpedienteDto> Movimientos,
    FuenteRespuesta Source,
    bool Exitoso,
    DateTimeOffset ResolvedAt,
    DateTimeOffset? CachedAt);
