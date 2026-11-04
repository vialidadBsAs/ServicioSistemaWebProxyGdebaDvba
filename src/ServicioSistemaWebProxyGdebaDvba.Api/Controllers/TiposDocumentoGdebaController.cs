using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Authorize(Policy = SeguridadInstitucional.PoliticaAdministracionExpedientes)]
[Route("api/gdeba/tipos-documento")]
public sealed class TiposDocumentoGdebaController : ControllerBase
{
    private readonly ICompletadoCatalogoTiposDocumentoService _completadoCatalogo;

    public TiposDocumentoGdebaController(ICompletadoCatalogoTiposDocumentoService completadoCatalogo)
    {
        _completadoCatalogo = completadoCatalogo;
    }

    /// <summary>
    /// Completa con GDEBA la sigla de los tipos documentales del catalogo que todavia no la tienen, de a tandas de <paramref name="maximo"/>
    /// tipos (200 por defecto, 500 como mucho). Se vuelve a llamar hasta que <c>pendientesRestantes</c> llegue a cero.
    /// </summary>
    [HttpPost("completar", Name = "CompletarCatalogoTiposDocumento")]
    public async Task<ActionResult<CompletadoCatalogoTiposDocumentoDto>> CompletarCatalogo([FromQuery] int? maximo, CancellationToken cancellationToken)
    {
        CompletadoCatalogoTiposDocumentoDto resultado = await _completadoCatalogo.CompletarAsync(maximo ?? 200, cancellationToken);
        return Ok(resultado);
    }
}
