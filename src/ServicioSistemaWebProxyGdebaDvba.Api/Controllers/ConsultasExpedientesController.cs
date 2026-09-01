using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Authorize(Policy = SeguridadInstitucional.PoliticaAccesoExpedientes)]
[Route("api/gdeba/consultas/expedientes")]
[ServiceFilter(typeof(Filters.RequiereUsuarioGdebaFilter))]
public sealed class ConsultasExpedientesController : ControllerBase
{
    private readonly IConsultaExpedientesService _consultaExpedientesService;

    public ConsultasExpedientesController(IConsultaExpedientesService consultaExpedientesService)
    {
        _consultaExpedientesService = consultaExpedientesService;
    }

    // Consulta masiva por tratas: respaldo exclusivo de la pantalla de temas y tratas.
    [HttpGet]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<ConsultaExpedientesResult>> Consultar([FromQuery] Guid[]? trataIds, [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 50, [FromQuery] string? campoOrden = null, [FromQuery] string? direccionOrden = null, [FromQuery] string[]? codigosTrata = null, [FromQuery] string[]? estadosActuales = null, [FromQuery] string[]? estadosDetalle = null, [FromQuery] string[]? numerosExpediente = null, [FromQuery] DateTimeOffset? fechaUltimoMovimientoDesde = null, [FromQuery] DateTimeOffset? fechaUltimoMovimientoHasta = null, [FromQuery] string? caratula = null, CancellationToken cancellationToken = default)
    {
        if ((trataIds is null || trataIds.Length == 0) && string.IsNullOrWhiteSpace(caratula)) return this.BadRequest("Debe seleccionar al menos una trata.");

        return this.Ok(await _consultaExpedientesService.ConsultarAsync(new ConsultaExpedientesRequest(trataIds, pagina, tamanioPagina, campoOrden, direccionOrden, codigosTrata, estadosActuales, estadosDetalle, numerosExpediente, fechaUltimoMovimientoDesde, fechaUltimoMovimientoHasta, caratula), cancellationToken));
    }

    // Busqueda puntual por texto de caratula: operacion de usuario final, con la politica de acceso general del controller.
    [HttpGet("caratula")]
    public async Task<ActionResult<ConsultaExpedientesResult>> BuscarPorCaratula([FromQuery] string texto, [FromQuery] Guid[]? trataIds = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3) return this.BadRequest("Indique al menos 3 caracteres del texto de la caratula.");

        return this.Ok(await _consultaExpedientesService.ConsultarAsync(new ConsultaExpedientesRequest(trataIds, Pagina: 1, TamanioPagina: 100, CampoOrden: "fechaUltimoMovimiento", DireccionOrden: "desc", Caratula: texto.Trim()), cancellationToken));
    }

    [HttpGet("cobertura-detalle")]
    public async Task<ActionResult<ConsultaCoberturaDetalleResult>> ConsultarCoberturaDetalle([FromQuery] Guid[]? trataIds, CancellationToken cancellationToken = default)
    {
        return this.Ok(await _consultaExpedientesService.ConsultarCoberturaDetalleAsync(trataIds, cancellationToken));
    }

    [HttpGet("valores-filtro")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<IReadOnlyCollection<string>>> ObtenerValoresFiltro([FromQuery] Guid[]? trataIds, [FromQuery] string campo, CancellationToken cancellationToken = default)
    {
        if (trataIds is null || trataIds.Length == 0) return this.BadRequest("Debe seleccionar al menos una trata.");

        return this.Ok(await _consultaExpedientesService.ObtenerValoresFiltroAsync(new ConsultaExpedientesValoresFiltroRequest(trataIds, campo), cancellationToken));
    }

    [HttpGet("documentos")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<ConsultaDocumentosPorTrataResult>> ConsultarDocumentos([FromQuery] Guid[]? trataIds, [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 50, [FromQuery] string? codigoTipoDocumento = null, [FromQuery] string? campoOrden = null, [FromQuery] string? direccionOrden = null, [FromQuery] string[]? numerosExpediente = null, [FromQuery] string[]? codigosTrata = null, [FromQuery] string[]? numerosActuacion = null, [FromQuery] string[]? referencias = null, [FromQuery] string? referenciaContiene = null, [FromQuery] string[]? tiposDocumento = null, [FromQuery] DateTimeOffset? fechaCreacionDesde = null, [FromQuery] DateTimeOffset? fechaCreacionHasta = null, [FromQuery] bool soloSinReferencia = false, CancellationToken cancellationToken = default)
    {
        if (trataIds is null || trataIds.Length == 0) return this.BadRequest("Debe seleccionar al menos una trata.");

        return this.Ok(await _consultaExpedientesService.ConsultarDocumentosAsync(new ConsultaDocumentosPorTrataRequest(trataIds, pagina, tamanioPagina, codigoTipoDocumento, campoOrden, direccionOrden, numerosExpediente, codigosTrata, numerosActuacion, referencias, referenciaContiene, tiposDocumento, fechaCreacionDesde, fechaCreacionHasta, soloSinReferencia), cancellationToken));
    }
}
