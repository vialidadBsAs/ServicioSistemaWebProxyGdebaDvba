namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarExpedienteDetalladoRequest(string NumeroGdebaCompleto, bool ForceRefresh = false, string? OperacionSolicitada = null, ServicioSistemaWebProxyGdebaDvba.Domain.Enums.OrigenInvocacionGdeba? Origen = null);
