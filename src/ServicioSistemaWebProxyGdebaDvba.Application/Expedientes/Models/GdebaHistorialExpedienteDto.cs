namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record GdebaHistorialExpedienteDto(
    IReadOnlyCollection<GdebaDocumentoExpedienteDto> DocumentosVinculados,
    IReadOnlyCollection<GdebaMovimientoExpedienteDto> Movimientos,
    IReadOnlyCollection<GdebaRelacionExpedienteDto> Relaciones);
