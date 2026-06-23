namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record GdebaExpedientePorTrataDto(
    string NumeroExpediente,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? Estado,
    DateTimeOffset? FechaModificacion,
    string? Motivo,
    string? UsuarioAnterior);
