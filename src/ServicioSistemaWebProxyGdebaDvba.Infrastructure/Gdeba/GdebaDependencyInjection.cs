using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public static class GdebaDependencyInjection
{
    public static IServiceCollection AddGdebaIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GdebaOptions.SectionName);
        var options = section.Get<GdebaOptions>() ?? new GdebaOptions();

        services.Configure<GdebaOptions>(section);
        services.AddScoped<IGdebaExecutionContext, GdebaExecutionContext>();
        services.AddScoped<IGdebaInvocacionRecorder, GdebaInvocacionRecorder>();
        services.AddHttpClient<IGdebaJwtTokenProvider, GdebaJwtTokenProvider>();
        ValidateEnvironment(options);

        switch (options.GatewayMode.Trim())
        {
            case GdebaGatewayModes.Fake:
                services.AddScoped<IGdebaExpedienteGateway, FakeGdebaExpedienteGateway>();
                break;

            case GdebaGatewayModes.Soap:
                services.AddHttpClient<IGdebaExpedienteGateway, SoapGdebaExpedienteGateway>();
                break;

            case GdebaGatewayModes.Rest:
                throw new InvalidOperationException("El modo de integracion REST esta reservado, pero aun no fue implementado.");

            default:
                throw new InvalidOperationException($"Modo de integracion GDEBA no soportado: '{options.GatewayMode}'.");
        }

        return services;
    }

    private static void ValidateEnvironment(GdebaOptions options)
    {
        var environmentName = string.IsNullOrWhiteSpace(options.CurrentEnvironment)
            ? GdebaEnvironmentNames.Hml
            : options.CurrentEnvironment.Trim();

        if (!options.Environments.ContainsKey(environmentName))
        {
            throw new InvalidOperationException($"No existe configuracion GDEBA para el ambiente '{environmentName}'.");
        }
    }
}
