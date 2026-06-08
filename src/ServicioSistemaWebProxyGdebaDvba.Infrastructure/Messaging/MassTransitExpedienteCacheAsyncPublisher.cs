using MassTransit;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Messaging;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Messaging.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;

public sealed class MassTransitExpedienteCacheAsyncPublisher : IExpedienteCacheAsyncPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitExpedienteCacheAsyncPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task SolicitarCacheDetalleAsync(
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
