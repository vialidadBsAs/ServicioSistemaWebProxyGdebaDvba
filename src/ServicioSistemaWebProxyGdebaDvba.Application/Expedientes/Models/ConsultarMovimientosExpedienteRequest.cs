namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ConsultarMovimientosExpedienteRequest(string NumeroGdebaCompleto, bool ForceRefresh, string? OperacionSolicitada = null, ServicioSistemaWebProxyGdebaDvba.Domain.Enums.OrigenInvocacionGdeba? Origen = null);
