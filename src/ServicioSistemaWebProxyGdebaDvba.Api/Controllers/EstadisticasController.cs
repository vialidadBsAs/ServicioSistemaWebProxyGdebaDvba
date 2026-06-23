using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/estadisticas")]
public sealed class EstadisticasController : ControllerBase
{
    private readonly IEstadisticasService _estadisticasService;

    public EstadisticasController(IEstadisticasService estadisticasService)
    {
        _estadisticasService = estadisticasService;
    }

    [HttpGet("expedientes-por-trata")]
    public async Task<ActionResult<EstadisticaExpedientesPorTrataResult>>
        ObtenerTotalesExpedientesPorTrata(
        [FromQuery] string? codigoTrata,
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        var result = await _estadisticasService.ObtenerTotalesExpedientesPorTrataAsync(
            new EstadisticaExpedientesPorTrataRequest(
                codigoTrata,
                fechaDesde,
                fechaHasta,
                estado),
            cancellationToken);

        return Ok(result);
    }
}
