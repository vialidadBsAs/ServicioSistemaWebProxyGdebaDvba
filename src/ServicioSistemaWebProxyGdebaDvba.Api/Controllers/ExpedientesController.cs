using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/expedientes")]
public sealed class ExpedientesController : ControllerBase
{
    private readonly IExpedienteService _expedienteService;

    public ExpedientesController(IExpedienteService expedienteService)
    {
        _expedienteService = expedienteService;
    }

   [HttpGet("{numeroGdebaCompleto}/detalle")]
    public async Task<ActionResult<ConsultarExpedienteDetalladoResult>> ConsultarDetalle(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ConsultarDetalleAsync(
            new ConsultarExpedienteDetalladoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Expediente is null ? NotFound(result) : Ok(result);
    }

    [HttpGet("{numeroGdebaCompleto}/movimientos")]
    public async Task<ActionResult<ConsultarMovimientosExpedienteResult>> ConsultarMovimientos(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ConsultarMovimientosAsync(
            new ConsultarMovimientosExpedienteRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }


    [HttpGet("{numeroGdebaCompleto}/sin-cache")]
    public async Task<ActionResult<ConsultarExpedienteSinCacheResult>> ConsultarSinCache(
       string numeroGdebaCompleto,
       CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ConsultarExpedienteSinCacheAsync(
            new ConsultarExpedienteSinCacheRequest(numeroGdebaCompleto),
            cancellationToken);

        return result.Expediente is null ? NotFound(result) : Ok(result);
    }
}
