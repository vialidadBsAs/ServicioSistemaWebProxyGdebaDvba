using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/temas-expediente")]
public sealed class TemasExpedienteController : ControllerBase
{
    private readonly ITemaExpedienteAdminService _temaExpedienteAdminService;

    public TemasExpedienteController(ITemaExpedienteAdminService temaExpedienteAdminService)
    {
        _temaExpedienteAdminService = temaExpedienteAdminService;
    }

    [HttpGet]
    [Authorize(Policy = SeguridadInstitucional.PoliticaAccesoExpedientes)]
    public async Task<ActionResult<IReadOnlyCollection<TemaExpedienteDto>>> ObtenerTemas(CancellationToken cancellationToken)
    {
        return Ok(await _temaExpedienteAdminService.ObtenerTemasAsync(cancellationToken));
    }

    [HttpGet("tratas-habilitadas")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaAccesoExpedientes)]
    public async Task<ActionResult<IReadOnlyCollection<TrataHabilitadaVialidadDto>>> ObtenerTratasHabilitadas(CancellationToken cancellationToken)
    {
        return Ok(await _temaExpedienteAdminService.ObtenerTratasHabilitadasAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<TemaExpedienteDto>> CrearTema([FromBody] GuardarTemaExpedienteRequest request, CancellationToken cancellationToken)
    {
        var tema = await _temaExpedienteAdminService.CrearTemaAsync(request, cancellationToken);
        return CreatedAtAction(nameof(this.ObtenerTemas), new { id = tema.Id }, tema);
    }

    [HttpPut("{temaId:guid}")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<TemaExpedienteDto>> ActualizarTema(Guid temaId, [FromBody] GuardarTemaExpedienteRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _temaExpedienteAdminService.ActualizarTemaAsync(temaId, request, cancellationToken));
    }

    [HttpDelete("{temaId:guid}")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<IActionResult> EliminarTema(Guid temaId, CancellationToken cancellationToken)
    {
        await _temaExpedienteAdminService.EliminarTemaAsync(temaId, cancellationToken);
        return NoContent();
    }
}
