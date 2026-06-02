using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

namespace ServicioSistemaWebProxyGdebaDvba.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConsultarExpedienteService, ConsultarExpedienteService>();
        services.AddScoped<IAuditoriaService, PersistedAuditoriaService>();
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IConsultarExpedienteService, ConsultarExpedienteService>();

        var auditoriaMode = configuration[$"{AuditoriaOptions.SectionName}:Mode"] ?? AuditoriaModes.Persisted;

        switch (auditoriaMode.Trim())
        {
            case AuditoriaModes.InMemory:
                services.AddScoped<IAuditoriaService, InMemoryAuditoriaService>();
                break;

            case AuditoriaModes.Persisted:
                services.AddScoped<IAuditoriaService, PersistedAuditoriaService>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Modo de auditoria no soportado: '{auditoriaMode}'.");
        }

        return services;
    }
}
