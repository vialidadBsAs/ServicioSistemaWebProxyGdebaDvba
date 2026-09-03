using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/temas-expediente")]
[ServiceFilter(typeof(Filters.RequiereUsuarioGdebaFilter))]
public sealed class TemasExpedienteController : ControllerBase
{
    private readonly ITemaExpedienteAdminService _temaExpedienteAdminService;

    public TemasExpedienteController(ITemaExpedienteAdminService temaExpedienteAdminService)
    {
        _temaExpedienteAdminService = temaExpedienteAdminService;
    }

    // Los temas son personales: cada usuario ve y administra solo los suyos.
    private string? UsuarioActual => User.Identity?.Name ?? User.FindFirst("unique_name")?.Value;

    [HttpGet]
    [Authorize(Policy = SeguridadInstitucional.PoliticaAccesoExpedientes)]
    public async Task<ActionResult<IReadOnlyCollection<TemaExpedienteDto>>> ObtenerTemas(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UsuarioActual)) return Unauthorized();

        return Ok(await _temaExpedienteAdminService.ObtenerTemasAsync(UsuarioActual, cancellationToken));
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
        if (string.IsNullOrWhiteSpace(UsuarioActual)) return Unauthorized();

        var tema = await _temaExpedienteAdminService.CrearTemaAsync(request, UsuarioActual, cancellationToken);
        return CreatedAtAction(nameof(this.ObtenerTemas), new { id = tema.Id }, tema);
    }

    [HttpPut("{temaId:guid}")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<TemaExpedienteDto>> ActualizarTema(Guid temaId, [FromBody] GuardarTemaExpedienteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UsuarioActual)) return Unauthorized();

        return Ok(await _temaExpedienteAdminService.ActualizarTemaAsync(temaId, request, UsuarioActual, cancellationToken));
    }

    [HttpDelete("{temaId:guid}")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<IActionResult> EliminarTema(Guid temaId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UsuarioActual)) return Unauthorized();

        await _temaExpedienteAdminService.EliminarTemaAsync(temaId, UsuarioActual, cancellationToken);
        return NoContent();
    }
}
