using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Middleware;

/// <summary>
/// Adjunta a cada respuesta el estado del sensor de conexion con GDEBA, para que el front
/// muestre el cartel global "Sin conexion" montado en el trafico normal, sin polling.
/// </summary>
public sealed class SensorConexionGdebaHeaderMiddleware
{
    public const string HeaderName = "X-Gdeba-Accesible";

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
            context.Response.Headers[HeaderName] = sensorConexion.Accesible ? "true" : "false";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
