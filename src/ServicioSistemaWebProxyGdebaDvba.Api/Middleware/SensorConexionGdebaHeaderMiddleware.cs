using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Middleware;

/// <summary>
/// Adjunta a cada respuesta el estado del sensor de conexion con GDEBA, para que el front
/// muestre el cartel global "Sin conexion" montado en el trafico normal, sin polling.
/// </summary>
public sealed class SensorConexionGdebaHeaderMiddleware
{
    public const string HeaderName = "X-Gdeba-Accesible";

    // Solo viaja mientras GDEBA no es accesible: en que paso se corto el acceso, para que el aviso de "sin conexion" lo diga.
    public const string HeaderMotivo = "X-Gdeba-Motivo";

    private readonly RequestDelegate _next;

    public SensorConexionGdebaHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISensorConexionGdeba sensorConexion)
    {
        // Se resuelve al momento de enviar la respuesta: la propia consulta puede haber cambiado el estado.
        context.Response.OnStarting(() =>
        {
            OrigenGdebaNoAccesible? origenFalla = sensorConexion.OrigenFalla;
            context.Response.Headers[HeaderName] = origenFalla is null ? "true" : "false";
            if (origenFalla is OrigenGdebaNoAccesible origen)
            {
                context.Response.Headers[HeaderMotivo] = SensorConexionGdebaHeaderMiddleware.CodigoDeOrigen(origen);
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }

    // Codigo estable que lee el front; tambien lo informa api/gdeba/estado.
    public static string CodigoDeOrigen(OrigenGdebaNoAccesible origen)
    {
        return origen == OrigenGdebaNoAccesible.CredencialesIop ? "credenciales-iop" : "servicio-gdeba";
    }
}
