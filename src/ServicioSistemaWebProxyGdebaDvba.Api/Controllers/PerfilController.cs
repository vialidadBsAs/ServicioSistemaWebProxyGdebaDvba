using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/perfil")]
public sealed class PerfilController : ControllerBase
{
    private readonly IPerfilUsuarioService _perfilService;

    public PerfilController(IPerfilUsuarioService perfilService)
    {
        _perfilService = perfilService;
    }

    // La identidad viene del token institucional DVBA-Auth; el proxy no administra credenciales.
    private string? UsuarioActual => User.Identity?.Name ?? User.FindFirst("unique_name")?.Value;

    [HttpGet]
    public async Task<ActionResult<PerfilUsuarioDto>> Obtener(CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        return Ok(await _perfilService.ObtenerAsync(usuario, cancellationToken));
    }

    [HttpPut("usuario-gdeba")]
    public async Task<ActionResult<PerfilUsuarioDto>> GuardarUsuarioGdeba([FromBody] GuardarUsuarioGdebaRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _perfilService.GuardarUsuarioGdebaAsync(usuario, request.UsuarioGdeba, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpGet("seguimientos")]
    public async Task<ActionResult<IReadOnlyCollection<SeguimientoExpedienteDto>>> ListarSeguimientos(CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        return Ok(await _perfilService.ListarSeguimientosAsync(usuario, cancellationToken));
    }

    [HttpPost("seguimientos/{expedienteId:guid}")]
    public async Task<IActionResult> Seguir(Guid expedienteId, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        try
        {
            await _perfilService.SeguirExpedienteAsync(usuario, expedienteId, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }

        return NoContent();
    }

    [HttpDelete("seguimientos/{expedienteId:guid}")]
    public async Task<IActionResult> DejarDeSeguir(Guid expedienteId, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        await _perfilService.DejarDeSeguirAsync(usuario, expedienteId, cancellationToken);
        return NoContent();
    }

    [HttpPost("seguimientos/{expedienteId:guid}/visto")]
    public async Task<IActionResult> MarcarVisto(Guid expedienteId, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        await _perfilService.MarcarVistoAsync(usuario, expedienteId, cancellationToken);
        return NoContent();
    }

    // Variantes por numero completo: la consulta interactiva no conoce el identificador local del expediente.
    [HttpGet("seguimientos/estado")]
    public async Task<ActionResult<object>> EstadoSeguimiento([FromQuery] string numero, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        bool siguiendo = await _perfilService.EstaSiguiendoAsync(usuario, numero, cancellationToken);
        return Ok(new { siguiendo });
    }

    [HttpPost("seguimientos/por-numero")]
    public async Task<IActionResult> SeguirPorNumero([FromBody] SeguimientoPorNumeroRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        try
        {
            await _perfilService.SeguirExpedientePorNumeroAsync(usuario, request.NumeroGdebaCompleto, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }

        return NoContent();
    }

    [HttpPost("seguimientos/por-numero/quitar")]
    public async Task<IActionResult> DejarDeSeguirPorNumero([FromBody] SeguimientoPorNumeroRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        await _perfilService.DejarDeSeguirPorNumeroAsync(usuario, request.NumeroGdebaCompleto, cancellationToken);
        return NoContent();
    }

    // Apertura del detalle: evalua las novedades por coleccion contra la vista anterior y sella el visto en una sola operacion.
    [HttpPost("seguimientos/por-numero/apertura")]
    public async Task<ActionResult<AperturaSeguimientoDto>> AbrirPorNumero([FromBody] SeguimientoPorNumeroRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        return Ok(await _perfilService.AbrirExpedientePorNumeroAsync(usuario, request.NumeroGdebaCompleto, cancellationToken));
    }

    [HttpPost("seguimientos/por-numero/visto")]
    public async Task<IActionResult> MarcarVistoPorNumero([FromBody] SeguimientoPorNumeroRequest request, CancellationToken cancellationToken)
    {
        if (this.UsuarioActual is not string usuario)
        {
            return Unauthorized();
        }

        await _perfilService.MarcarVistoPorNumeroAsync(usuario, request.NumeroGdebaCompleto, cancellationToken);
        return NoContent();
    }

    public sealed record SeguimientoPorNumeroRequest(string NumeroGdebaCompleto);
}
