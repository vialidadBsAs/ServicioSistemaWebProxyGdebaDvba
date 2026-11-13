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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
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

    // Pestana "Alertas": lo detectado hoy (visto o no) mas lo no revisado de los ultimos dias configurados.
    [HttpGet("coincidencias")]
    public async Task<ActionResult<NovedadesAlertasDocumentalesDto>> ListarNovedades(CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return this.Unauthorized();
        }

        return this.Ok(await _alertaService.ListarNovedadesAsync(usuario, cancellationToken));
    }

    // Historico agrupado por alerta: una fila por alerta con sus cantidades; el detalle se pide a "historico" con AlertaId.
    [HttpGet("historico/resumen")]
    public async Task<ActionResult<ResumenHistoricoAlertasDto>> ResumirHistorico(CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return this.Unauthorized();
        }

        return this.Ok(await _alertaService.ResumirHistoricoAsync(usuario, cancellationToken));
    }

    // Pestana "Historico de alertas": todo lo anterior, paginado, con orden y filtros de columna resueltos en el servidor.
    [HttpPost("historico")]
    public async Task<ActionResult<HistoricoAlertasDocumentalesDto>> ConsultarHistorico([FromBody] ConsultaHistoricoAlertasRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return this.Unauthorized();
        }

        return this.Ok(await _alertaService.ConsultarHistoricoAsync(usuario, request, cancellationToken));
    }

    // "Marcar como revisadas" del historico: las filas elegidas; sin filas, todas las no revisadas.
    [HttpPost("historico/revisadas")]
    public async Task<ActionResult<int>> MarcarHistoricoRevisado([FromBody] MarcarHistoricoRevisadoRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return this.Unauthorized();
        }

        return this.Ok(await _alertaService.MarcarHistoricoRevisadoAsync(usuario, request.CoincidenciaIds, cancellationToken));
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

    // Abrir una coincidencia (expediente o PDF) la marca vista; las demas pendientes siguen pendientes.
    [HttpPost("vistas/{coincidenciaId:guid}")]
    public async Task<IActionResult> MarcarVista(Guid coincidenciaId, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        return await _alertaService.MarcarVistaAsync(usuario, coincidenciaId, cancellationToken) ? NoContent() : NotFound();
    }

    // "Marcar todas como vistas" de la pestana "Alertas": marca lo no revisado que esta ahi; nada se borra.
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
