using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
[Route("api/gdeba/temas-expediente")]
public sealed class TemasExpedienteController : ControllerBase
{
    private readonly ITemaExpedienteAdminService _temaExpedienteAdminService;

    public TemasExpedienteController(ITemaExpedienteAdminService temaExpedienteAdminService)
    {
        _temaExpedienteAdminService = temaExpedienteAdminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TemaExpedienteDto>>> ObtenerTemas(CancellationToken cancellationToken)
    {
        return Ok(await _temaExpedienteAdminService.ObtenerTemasAsync(cancellationToken));
    }

    [HttpGet("tratas-habilitadas")]
    public async Task<ActionResult<IReadOnlyCollection<TrataHabilitadaVialidadDto>>> ObtenerTratasHabilitadas(CancellationToken cancellationToken)
    {
        return Ok(await _temaExpedienteAdminService.ObtenerTratasHabilitadasAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<TemaExpedienteDto>> CrearTema([FromBody] GuardarTemaExpedienteRequest request, CancellationToken cancellationToken)
    {
        var tema = await _temaExpedienteAdminService.CrearTemaAsync(request, cancellationToken);
        return CreatedAtAction(nameof(this.ObtenerTemas), new { id = tema.Id }, tema);
    }

    [HttpPut("{temaId:guid}")]
    public async Task<ActionResult<TemaExpedienteDto>> ActualizarTema(Guid temaId, [FromBody] GuardarTemaExpedienteRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _temaExpedienteAdminService.ActualizarTemaAsync(temaId, request, cancellationToken));
    }

    [HttpDelete("{temaId:guid}")]
    public async Task<IActionResult> EliminarTema(Guid temaId, CancellationToken cancellationToken)
    {
        await _temaExpedienteAdminService.EliminarTemaAsync(temaId, cancellationToken);
        return NoContent();
    }
}
