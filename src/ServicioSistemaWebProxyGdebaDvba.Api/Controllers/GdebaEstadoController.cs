using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/gdeba/estado")]
public sealed class GdebaEstadoController : ControllerBase
{
    private readonly ISensorConexionGdeba _sensorConexion;

    public GdebaEstadoController(ISensorConexionGdeba sensorConexion)
    {
        _sensorConexion = sensorConexion;
    }

    // Lectura liviana del estado en memoria (no llama a GDEBA): la consume el front al iniciar para el cartel global.
    [HttpGet]
    public IActionResult Get() => this.Ok(new { accesible = _sensorConexion.Accesible });
}
