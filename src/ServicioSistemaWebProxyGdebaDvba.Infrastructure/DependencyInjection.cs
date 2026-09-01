using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Seguridad;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF;
using URF.Core.EF.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICurrentApplicationAccessor, CurrentApplicationAccessor>();
        services.AddScoped<IUsuarioActualAccessor, UsuarioActualAccessor>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentApplicationAccessor, CurrentApplicationAccessor>();
        services.AddScoped<IUsuarioActualAccessor, UsuarioActualAccessor>();

        var connectionString = configuration.GetConnectionString("ProxyGdeba");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // Produccion corre sobre SQL Server 2008: el nivel de compatibilidad 100 evita traducciones modernas (p. ej. OPENJSON en los Contains).
            services.AddDbContext<ProxyGdebaDbContext>(options => options.UseSqlServer(connectionString, sqlServer => sqlServer.UseCompatibilityLevel(100)));
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ProxyGdebaDbContext>());
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(ITrackableRepository<>), typeof(TrackableRepository<>));
        }

        return services;
    }
}
