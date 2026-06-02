namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarExpedienteDetalladoRequest(
    string NumeroGdebaCompleto,
    bool ForceRefresh = false);
