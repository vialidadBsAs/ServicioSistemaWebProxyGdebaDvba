using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Messaging;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Messaging.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;

public sealed class MassTransitExpedienteCacheAsyncPublisher : IExpedienteCacheAsyncPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<MassTransitExpedienteCacheAsyncPublisher> _logger;

    public MassTransitExpedienteCacheAsyncPublisher(
        IPublishEndpoint publishEndpoint,
        IOptions<RabbitMqOptions> options,
        ILogger<MassTransitExpedienteCacheAsyncPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> SolicitarCacheDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken)
    {
        var timeoutSeconds = Math.Max(1, _options.Value.PublishTimeoutSeconds);
        using var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellationTokenSource.Token);

        try
        {
            await _publishEndpoint.Publish(
                new CachearDetalleExpedienteV1(
                    detalle.NumeroGdebaCompleto,
                    fechaConsulta,
                    detalle),
                linkedCancellationTokenSource.Token);

            return true;
        }
        catch (OperationCanceledException) when (timeoutCancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogWarning(
                "No se pudo publicar la solicitud de cache del expediente {NumeroExpediente} dentro de {TimeoutSeconds} segundos.",
                detalle.NumeroGdebaCompleto,
                timeoutSeconds);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo publicar la solicitud de cache del expediente {NumeroExpediente}.",
                detalle.NumeroGdebaCompleto);

            return false;
        }
    }
}
