using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/cuotas")]
public sealed class CuotasGdebaController : ControllerBase
{
    private readonly IConsultaCuotasGdeba _consultaCuotas;

    public CuotasGdebaController(IConsultaCuotasGdeba consultaCuotas)
    {
        _consultaCuotas = consultaCuotas;
    }

    [HttpGet(Name = "ConsultarCuotasGdeba")]
    public async Task<ActionResult<ConsultaCuotasGdebaResult>> ConsultarCuotas([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var result = await _consultaCuotas.ConsultarCuotasAsync(fecha ?? DateOnly.FromDateTime(DateTime.Now), cancellationToken);

        return Ok(result);
    }
}
