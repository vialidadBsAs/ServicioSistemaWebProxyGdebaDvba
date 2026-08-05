using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Authorize(Policy = SeguridadInstitucional.PoliticaAccesoExpedientes)]
[Route("api/gdeba/consultas/expedientes")]
public sealed class ConsultasExpedientesController : ControllerBase
{
    private readonly IConsultaExpedientesService _consultaExpedientesService;

    public ConsultasExpedientesController(IConsultaExpedientesService consultaExpedientesService)
    {
        _consultaExpedientesService = consultaExpedientesService;
    }

    [HttpGet]
    public async Task<ActionResult<ConsultaExpedientesResult>> Consultar([FromQuery] Guid[]? trataIds, [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 50, [FromQuery] string? campoOrden = null, [FromQuery] string? direccionOrden = null, [FromQuery] string[]? codigosTrata = null, [FromQuery] string[]? estadosActuales = null, [FromQuery] string[]? estadosDetalle = null, CancellationToken cancellationToken = default)
    {
        if (trataIds is null || trataIds.Length == 0) return BadRequest("Debe seleccionar al menos una trata.");

        return Ok(await _consultaExpedientesService.ConsultarAsync(new ConsultaExpedientesRequest(trataIds, pagina, tamanioPagina, campoOrden, direccionOrden, codigosTrata, estadosActuales, estadosDetalle), cancellationToken));
    }

    [HttpGet("valores-filtro")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> ObtenerValoresFiltro([FromQuery] Guid[]? trataIds, [FromQuery] string campo, CancellationToken cancellationToken = default)
    {
        if (trataIds is null || trataIds.Length == 0) return BadRequest("Debe seleccionar al menos una trata.");

        return Ok(await _consultaExpedientesService.ObtenerValoresFiltroAsync(new ConsultaExpedientesValoresFiltroRequest(trataIds, campo), cancellationToken));
    }
}
