using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Middleware;

public sealed class GdebaExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GdebaExceptionMiddleware> _logger;

    public GdebaExceptionMiddleware(
        RequestDelegate next,
        ILogger<GdebaExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested && GdebaNoAccesibleException.EsNoAccesible(ex))
        {
            // GDEBA no accesible (rechazo del gateway 401/403 o sin respuesta) en una operacion sin copia local desde la cual responder:
            // es el mismo estado que muestra el cartel de "sin conexion", no un error devuelto por GDEBA.
            string motivo = GdebaNoAccesibleException.DescribirMotivo(ex);
            _logger.LogWarning(ex, "GDEBA no accesible al atender {Ruta}: {Motivo}.", context.Request.Path, motivo);

            ProblemDetails problem = new()
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Sin conexión con GDEBA",
                Detail = $"GDEBA no es accesible en este momento ({motivo}) y esta operación no tiene una copia local desde la cual responder.",
                Instance = context.Request.Path
            };
            problem.Extensions["codigo"] = "GDEBA_NO_ACCESIBLE";

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
        }
        catch (GdebaOperationException ex)
        {
            _logger.LogError(
                ex,
                "Fallo la operacion SOAP GDEBA {Operacion}.",
                ex.Operation);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Error al consultar GDEBA",
                Detail = ex.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["operacion"] = ex.Operation;

            if (ex.StatusCode is not null)
            {
                problem.Extensions["estadoHttpGdeba"] = ex.StatusCode.Value;
            }

            if (!string.IsNullOrWhiteSpace(ex.SoapFaultCode))
            {
                problem.Extensions["codigoSoap"] = ex.SoapFaultCode;
            }

            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsJsonAsync(
                problem,
                cancellationToken: context.RequestAborted);
        }
        catch (GdebaConfigurationException ex)
        {
            _logger.LogError(ex, "La configuracion GDEBA requerida para procesar la solicitud esta incompleta.");

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = ex.PublicTitle,
                Detail = ex.PublicMessage,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                problem,
                cancellationToken: context.RequestAborted);
        }
    }
}
