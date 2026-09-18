using Microsoft.Data.SqlClient;
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

        var connectionString = DependencyInjection.ResolverConnectionString(configuration);
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

    // El string base (servidor, base, opciones) es versionable; la credencial vive fuera del repo (User Secrets / variables de entorno)
    // como un fragmento "User Id=...;Password=..." nombrado en SqlLogins, y el entorno elige cual usar con ProxyGdebaLogin.
    // Sin ProxyGdebaLogin el string queda tal cual (p. ej. Integrated Security en local).
    private static string? ResolverConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProxyGdeba");
        var nombreLogin = configuration["ProxyGdebaLogin"];
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(nombreLogin))
        {
            return connectionString;
        }

        var credencial = configuration[$"SqlLogins:{nombreLogin}"];
        if (string.IsNullOrWhiteSpace(credencial))
        {
            throw new InvalidOperationException($"ProxyGdebaLogin indica '{nombreLogin}' pero no existe la credencial 'SqlLogins:{nombreLogin}' (User Secrets o variable de entorno).");
        }

        return new SqlConnectionStringBuilder($"{connectionString.TrimEnd(';')};{credencial}").ConnectionString;
    }
}
