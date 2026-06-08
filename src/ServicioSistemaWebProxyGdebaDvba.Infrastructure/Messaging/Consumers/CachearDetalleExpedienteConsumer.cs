using MassTransit;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Messaging.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging.Consumers;

public sealed class CachearDetalleExpedienteConsumer : IConsumer<CachearDetalleExpedienteV1>
{
    private readonly IExpedienteDetalleCacheProcessor _cacheProcessor;

    public CachearDetalleExpedienteConsumer(IExpedienteDetalleCacheProcessor cacheProcessor)
    {
        _cacheProcessor = cacheProcessor;
    }

    public Task Consume(ConsumeContext<CachearDetalleExpedienteV1> context)
    {
        return _cacheProcessor.ProcesarDetalleAsync(
            context.Message.Detalle,
            context.Message.FechaConsulta,
            context.CancellationToken);
    }
}
