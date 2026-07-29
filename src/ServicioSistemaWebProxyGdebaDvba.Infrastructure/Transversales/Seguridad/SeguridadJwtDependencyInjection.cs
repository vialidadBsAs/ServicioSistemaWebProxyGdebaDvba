using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Seguridad;

public static class SeguridadJwtDependencyInjection
{
    public static IServiceCollection AddSeguridadJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(SeguridadJwtOptions.SectionName);
        var options = section.Get<SeguridadJwtOptions>() ?? throw new InvalidOperationException($"No se encontro la configuracion '{SeguridadJwtOptions.SectionName}'.");
        var signingKey = configuration.GetConnectionString("MiLLave");
        SeguridadJwtDependencyInjection.Validar(options, signingKey);

        services.Configure<SeguridadJwtOptions>(section);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtOptions =>
        {
            jwtOptions.MapInboundClaims = true;
            jwtOptions.SaveToken = false;
            jwtOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = options.ValidIssuer,
                ValidAudience = options.ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!)),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds)
            };
        });

        var accessPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(context => SeguridadJwtDependencyInjection.TieneAccesoAplicacion(context.User, options.ApplicationName))
            .Build();
        var administratorPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(context => SeguridadJwtDependencyInjection.TieneAccesoAplicacion(context.User, options.ApplicationName))
            .RequireRole(options.AdministratorRole)
            .Build();

        services.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddPolicy(SeguridadInstitucional.PoliticaAccesoExpedientes, accessPolicy);
            authorizationOptions.AddPolicy(SeguridadInstitucional.PoliticaAdministracionExpedientes, administratorPolicy);
            authorizationOptions.FallbackPolicy = accessPolicy;
        });

        return services;
    }

    private static bool TieneAccesoAplicacion(ClaimsPrincipal user, string applicationName)
    {
        return user.FindAll(SeguridadInstitucional.ClaimAplicaciones)
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(application => string.Equals(application, applicationName, StringComparison.OrdinalIgnoreCase));
    }

    private static void Validar(SeguridadJwtOptions options, string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(options.ApplicationName) || string.IsNullOrWhiteSpace(options.AdministratorRole) || string.IsNullOrWhiteSpace(options.ValidIssuer) || string.IsNullOrWhiteSpace(options.ValidAudience))
        {
            throw new InvalidOperationException($"La configuracion '{SeguridadJwtOptions.SectionName}' esta incompleta.");
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("La clave JWT debe configurarse en 'ConnectionStrings:MiLLave'.");
        }

        if (options.ClockSkewSeconds < 0)
        {
            throw new InvalidOperationException($"El tiempo configurado en '{SeguridadJwtOptions.SectionName}:ClockSkewSeconds' no es valido.");
        }
    }
}
