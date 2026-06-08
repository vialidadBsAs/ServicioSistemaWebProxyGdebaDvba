using MassTransit;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Messaging.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging.Consumers;

public sealed class CachearDetalleExpedienteConsumer : IConsumer<CachearDetalleExpedienteV1>
{
    private readonly IExpedienteCacheAsyncProcessor _processor;

    public CachearDetalleExpedienteConsumer(IExpedienteCacheAsyncProcessor processor)
    {
        _processor = processor;
    }

    public Task Consume(ConsumeContext<CachearDetalleExpedienteV1> context)
    {
        return _processor.CachearDetalleAsync(
            context.Message.Detalle,
            context.Message.FechaConsulta,
            context.CancellationToken);
    }
}
