using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.ReadStores;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Services;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Services;

namespace ServicioSistemaWebProxyGdebaDvba.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IExpedienteService, ExpedienteService>();
        services.AddScoped<IExpedienteCacheAsyncProcessor, ExpedienteCacheAsyncProcessor>();
        services.AddScoped<IExpedienteCacheReadStore, ExpedienteCacheReadStore>();
        services.AddScoped<IRegistroInvocacionesGdeba, RegistroInvocacionesGdeba>();
        services.AddScoped<IConsultaCuotasGdeba, ConsultaCuotasGdeba>();
        services.AddScoped<IAuditoriaService, PersistedAuditoriaService>();
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IExpedienteService, ExpedienteService>();
        services.AddScoped<IExpedienteCacheAsyncProcessor, ExpedienteCacheAsyncProcessor>();
        services.AddScoped<IExpedienteCacheReadStore, ExpedienteCacheReadStore>();
        services.AddScoped<IRegistroInvocacionesGdeba, RegistroInvocacionesGdeba>();
        services.AddScoped<IConsultaCuotasGdeba, ConsultaCuotasGdeba>();

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
