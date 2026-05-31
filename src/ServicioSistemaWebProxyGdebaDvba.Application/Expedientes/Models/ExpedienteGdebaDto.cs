namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ExpedienteGdebaDto(
    string NumeroGdebaCompleto,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? Estado);
