using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

// Alertas documentales personales: definicion, coincidencias detectadas y vistas. La deteccion no pasa por aca: ocurre cuando
// cualquier consulta trae un documento nuevo (ver IDetectorCoincidenciasAlertaDocumental).
[ApiController]
[Route("api/perfil/alertas")]
public sealed class AlertasDocumentalesController : ControllerBase
{
    private readonly IAlertaDocumentalService _alertaService;

    public AlertasDocumentalesController(IAlertaDocumentalService alertaService)
    {
        _alertaService = alertaService;
    }

    // La identidad viene del token institucional DVBA-Auth; el proxy no administra credenciales.
    private string? UsuarioActual => User.Identity?.Name ?? User.FindFirst("unique_name")?.Value;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AlertaDocumentalDto>>> ListarAlertas(CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        return Ok(await _alertaService.ListarAlertasAsync(usuario, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AlertaDocumentalDto>> CrearAlerta([FromBody] GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _alertaService.CrearAlertaAsync(usuario, request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{alertaId:guid}")]
    public async Task<ActionResult<AlertaDocumentalDto>> ActualizarAlerta(Guid alertaId, [FromBody] GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _alertaService.ActualizarAlertaAsync(usuario, alertaId, request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{alertaId:guid}")]
    public async Task<IActionResult> EliminarAlerta(Guid alertaId, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        await _alertaService.EliminarAlertaAsync(usuario, alertaId, cancellationToken);
        return NoContent();
    }

    [HttpGet("coincidencias")]
    public async Task<ActionResult<IReadOnlyCollection<CoincidenciaAlertaDocumentalDto>>> ListarCoincidencias(CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        return Ok(await _alertaService.ListarCoincidenciasAsync(usuario, cancellationToken));
    }

    // Lectura liviana para el badge y el aviso: total pendiente y las detectadas despues de la lectura anterior del front.
    [HttpGet("pendientes")]
    public async Task<ActionResult<PendientesAlertasDocumentalesDto>> ConsultarPendientes([FromQuery] DateTimeOffset? desde, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        return Ok(await _alertaService.ConsultarPendientesAsync(usuario, desde, cancellationToken));
    }

    // Pasar por la pantalla de alertas marca vistas todas las pendientes y conserva las ultimas vistas por alerta.
    [HttpPost("vistas")]
    public async Task<IActionResult> MarcarVistas(CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        await _alertaService.MarcarVistasAsync(usuario, cancellationToken);
        return NoContent();
    }

    [HttpGet("tipos-documento")]
    public async Task<ActionResult<IReadOnlyCollection<TipoDocumentoAlertaDto>>> ListarTiposDocumento(CancellationToken cancellationToken)
    {
        return Ok(await _alertaService.ListarTiposDocumentoAsync(cancellationToken));
    }
}
