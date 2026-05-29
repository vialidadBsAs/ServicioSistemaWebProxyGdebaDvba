using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;

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

    [HttpGet("{numeroExpediente}")]
    public async Task<ActionResult<ConsultarExpedienteResult>> Consultar(
        string numeroExpediente,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _consultarExpedienteService.ConsultarAsync(
            new ConsultarExpedienteRequest(numeroExpediente, forceRefresh),
            cancellationToken);

        return result.Expediente is null ? NotFound(result) : Ok(result);
    }
}
