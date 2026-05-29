namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;

public sealed record ConsultarExpedienteRequest(string NumeroExpediente, bool ForceRefresh = false);
