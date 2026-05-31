using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/expedientes")]
public sealed class ExpedientesController : ControllerBase
{
    private readonly IConsultarExpedienteService _consultarExpedienteService;

    public ExpedientesController(IConsultarExpedienteService consultarExpedienteService)
    {
        _consultarExpedienteService = consultarExpedienteService;
    }

    [HttpGet("{numeroGdebaCompleto}")]
    public async Task<ActionResult<ConsultarExpedienteResult>> Consultar(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _consultarExpedienteService.ConsultarAsync(
            new ConsultarExpedienteRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Expediente is null ? NotFound(result) : Ok(result);
    }
}
