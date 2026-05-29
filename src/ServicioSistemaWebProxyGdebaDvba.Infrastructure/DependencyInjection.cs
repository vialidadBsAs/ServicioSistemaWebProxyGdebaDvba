using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Security;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Security;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICurrentApplicationAccessor, CurrentApplicationAccessor>();
        services.AddScoped<IAuditoriaService, InMemoryAuditoriaService>();

        return services;
    }
}
