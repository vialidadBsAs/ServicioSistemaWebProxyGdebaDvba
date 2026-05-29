namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;

public sealed record ExpedienteGdebaDto(
    string NumeroExpediente,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? Estado);
