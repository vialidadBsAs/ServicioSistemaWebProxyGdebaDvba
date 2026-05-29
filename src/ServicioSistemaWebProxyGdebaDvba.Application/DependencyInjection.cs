using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;

namespace ServicioSistemaWebProxyGdebaDvba.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConsultarExpedienteService, ConsultarExpedienteService>();
        return services;
    }
}
