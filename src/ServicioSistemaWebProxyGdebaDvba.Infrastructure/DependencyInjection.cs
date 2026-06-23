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

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentApplicationAccessor, CurrentApplicationAccessor>();

        var connectionString = configuration.GetConnectionString("ProxyGdeba");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ProxyGdebaDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ProxyGdebaDbContext>());
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(ITrackableRepository<>), typeof(TrackableRepository<>));
        }

        return services;
    }
}
