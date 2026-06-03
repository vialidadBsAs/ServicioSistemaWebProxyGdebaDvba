using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Messaging.Contracts;

public sealed record CachearDetalleExpedienteV1(
    string NumeroGdebaCompleto,
    DateTimeOffset FechaConsulta,
    GdebaExpedienteDetalladoDto Detalle);
