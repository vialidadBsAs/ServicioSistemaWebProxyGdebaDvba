using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Filters;

/// <summary>
/// Compuerta de las consultas interactivas contra GDEBA: sin usuario GDEBA cargado en el perfil no se opera.
/// Ademas deposita la identidad del request en el accessor para que los gateways y la auditoria la utilicen.
/// Los workers no pasan por aca y quedan exentos por diseño.
/// </summary>
public sealed class RequiereUsuarioGdebaFilter : IAsyncActionFilter
{
    public const string Codigo = "PERFIL_SIN_USUARIO_GDEBA";

    private readonly IPerfilUsuarioService _perfilService;
    private readonly IUsuarioActualAccessor _usuarioActualAccessor;

    public RequiereUsuarioGdebaFilter(IPerfilUsuarioService perfilService, IUsuarioActualAccessor usuarioActualAccessor)
    {
        _perfilService = perfilService;
        _usuarioActualAccessor = usuarioActualAccessor;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string? usuario = context.HttpContext.User.Identity?.Name ?? context.HttpContext.User.FindFirst("unique_name")?.Value;

        // Sin identidad en el token la compuerta no aplica: la validacion de token es progresiva y no debe bloquear los ambientes donde aun no viaja.
        if (!string.IsNullOrWhiteSpace(usuario))
        {
            string? usuarioGdeba = await _perfilService.ObtenerUsuarioGdebaAsync(usuario, context.HttpContext.RequestAborted);
            if (string.IsNullOrWhiteSpace(usuarioGdeba))
            {
                context.Result = new ObjectResult(new
                {
                    codigo = RequiereUsuarioGdebaFilter.Codigo,
                    mensaje = "Para operar necesitás cargar tu usuario GDEBA en la configuración de tu perfil."
                })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
                return;
            }

            _usuarioActualAccessor.UsuarioInstitucional = usuario;
            _usuarioActualAccessor.UsuarioGdeba = usuarioGdeba;
        }

        await next();
    }
}
