using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Messaging;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging.Consumers;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeConsumers)
    {
        var options = configuration
            .GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddScoped<IExpedienteDetalleCacheDispatcher, MassTransitExpedienteDetalleCacheDispatcher>();

        services.AddMassTransit(busConfigurator =>
        {
            if (includeConsumers)
            {
                busConfigurator.AddConsumer<CachearDetalleExpedienteConsumer>();
            }

            busConfigurator.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, hostConfigurator =>
                {
                    hostConfigurator.Username(options.Username);
                    hostConfigurator.Password(options.Password);
                });

                if (includeConsumers)
                {
                    cfg.ReceiveEndpoint(options.CachearDetalleExpedienteQueue, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<CachearDetalleExpedienteConsumer>(context);
                    });
                }
            });
        });

        return services;
    }
}
