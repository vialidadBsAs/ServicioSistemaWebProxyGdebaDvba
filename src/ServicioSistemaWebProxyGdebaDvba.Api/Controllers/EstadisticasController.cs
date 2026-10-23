using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/estadisticas")]
[ServiceFilter(typeof(Filters.RequiereUsuarioGdebaFilter))]
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

    // Ultima corrida de descubrimiento con expedientes nuevos: rango por defecto del bloque "Expedientes nuevos". 204 si todavia no hubo ninguna.
    [HttpGet("ultima-deteccion")]
    public async Task<ActionResult<UltimaDeteccionExpedientesResult>> ObtenerUltimaDeteccion(CancellationToken cancellationToken)
    {
        UltimaDeteccionExpedientesResult? ultima = await _estadisticasService.ObtenerUltimaDeteccionAsync(cancellationToken);
        return ultima is null ? this.NoContent() : this.Ok(ultima);
    }

    // Detalle de una trata del bloque "expedientes caratulados": se pide al desplegar la fila, no junto con los totales.
    [HttpGet("expedientes-caratulados")]
    public async Task<ActionResult<EstadisticaExpedientesCaratuladosResult>> ObtenerExpedientesCaratulados([FromQuery] string? codigoTrata, [FromQuery] DateOnly? fechaDesde, [FromQuery] DateOnly? fechaHasta, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoTrata)) return this.BadRequest("Debe indicar el codigo de trata.");

        return this.Ok(await _estadisticasService.ObtenerExpedientesCaratuladosAsync(new EstadisticaExpedientesCaratuladosRequest(codigoTrata, fechaDesde, fechaHasta), cancellationToken));
    }
}
