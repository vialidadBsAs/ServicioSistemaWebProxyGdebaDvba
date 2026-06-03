namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarMovimientosExpedienteRequest(
    string NumeroGdebaCompleto,
    bool ForceRefresh);
