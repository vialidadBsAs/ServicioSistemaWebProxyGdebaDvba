using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Conexion;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public static class GdebaDependencyInjection
{
    public static IServiceCollection AddGdebaIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GdebaOptions.SectionName);
        var options = section.Get<GdebaOptions>() ?? new GdebaOptions();

        services.Configure<GdebaOptions>(section);
        services.Configure<GdebaConexionOptions>(configuration.GetSection(GdebaConexionOptions.SectionName));
        // Sensor de conexion (circuit breaker): estado compartido del proceso.
        services.AddSingleton<ISensorConexionGdeba, SensorConexionGdeba>();
        services.AddScoped<IGdebaExecutionContext, GdebaExecutionContext>();
        services.AddHttpClient<IGdebaJwtTokenProvider, GdebaJwtTokenProvider>();
        services.AddTransient<GdebaAuthenticationHandler>();
        GdebaDependencyInjection.ValidateEnvironment(options);

        switch (options.GatewayMode.Trim())
        {
            case GdebaGatewayModes.Fake:
                services.AddScoped<IGdebaExpedienteGateway, FakeGdebaExpedienteGateway>();
                services.AddScoped<IGdebaDocumentoGateway, FakeGdebaDocumentoGateway>();
                services.AddScoped<IGdebaTipoDocumentoGateway, FakeGdebaTipoDocumentoGateway>();
                break;

            case GdebaGatewayModes.Soap:
                services.AddHttpClient<IGdebaExpedienteGateway, SoapGdebaExpedienteGateway>().AddHttpMessageHandler<GdebaAuthenticationHandler>();
                services.AddHttpClient<IGdebaDocumentoGateway, SoapGdebaDocumentoGateway>().AddHttpMessageHandler<GdebaAuthenticationHandler>();
                services.AddHttpClient<IGdebaTipoDocumentoGateway, SoapGdebaTipoDocumentoGateway>().AddHttpMessageHandler<GdebaAuthenticationHandler>();
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
