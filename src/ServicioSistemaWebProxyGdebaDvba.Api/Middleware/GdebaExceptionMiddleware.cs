using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested && !context.Response.HasStarted)
        {
            // Cualquier otra falla (base de datos, bug): sin esto llegaba al front como 500 con cuerpo vacio y solo quedaba en el log de IIS.
            // Se registra con la traza y se responde un ProblemDetails que distingue al menos el timeout de SQL Server.
            _logger.LogError(ex, "Error no previsto al atender {Metodo} {Ruta}.", context.Request.Method, context.Request.Path);

            bool esTimeoutSql = ex is SqlException { Number: -2 };
            ProblemDetails problem = new()
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = esTimeoutSql ? "La consulta tardó demasiado" : "Error interno",
                Detail = esTimeoutSql
                    ? "La base de datos no respondió a tiempo. Volvé a intentar; si se repite, avisá al administrador."
                    : "Se produjo un error inesperado al procesar la solicitud. El detalle quedó registrado en el servidor.",
                Instance = context.Request.Path
            };
            problem.Extensions["codigo"] = esTimeoutSql ? "SQL_TIMEOUT" : "ERROR_INTERNO";

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
        }
    }
}
