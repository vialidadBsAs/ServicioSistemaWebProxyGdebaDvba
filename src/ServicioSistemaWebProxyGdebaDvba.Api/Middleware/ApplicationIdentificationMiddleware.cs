using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Middleware;

public sealed class ApplicationIdentificationMiddleware
{
    public const string ApplicationIdHeaderName = "X-Application-Id";

    private readonly RequestDelegate _next;

    public ApplicationIdentificationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentApplicationAccessor currentApplicationAccessor)
    {
        var applicationId = context.Request.Headers[ApplicationIdHeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(applicationId))
        {
            currentApplicationAccessor.Current = new CurrentApplication(applicationId, applicationId);
        }

        await _next(context);
    }
}
