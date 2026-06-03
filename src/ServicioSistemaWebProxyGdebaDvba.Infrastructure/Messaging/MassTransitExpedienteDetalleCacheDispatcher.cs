using MassTransit;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Messaging;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Messaging.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;

public sealed class MassTransitExpedienteDetalleCacheDispatcher : IExpedienteDetalleCacheDispatcher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitExpedienteDetalleCacheDispatcher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublicarCacheDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken)
    {
        return _publishEndpoint.Publish(
            new CachearDetalleExpedienteV1(
                detalle.NumeroGdebaCompleto,
                fechaConsulta,
                detalle),
            cancellationToken);
    }
}
