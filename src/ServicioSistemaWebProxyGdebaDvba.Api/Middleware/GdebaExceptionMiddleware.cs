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
    }
}
