namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarExpedienteRequest(string NumeroGdebaCompleto, bool ForceRefresh = false);
