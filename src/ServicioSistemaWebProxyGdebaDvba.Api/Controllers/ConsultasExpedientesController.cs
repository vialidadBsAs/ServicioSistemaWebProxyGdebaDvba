using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Api.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

// Todas las consultas reciben el filtro en el cuerpo (POST con JSON): la seleccion de tratas es una lista que no entra en una query
// string cuando abarca el catalogo entero. Siguen siendo lecturas; el metodo es solo el transporte del filtro.
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
    [HttpPost]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<ConsultaExpedientesResult>> Consultar([FromBody] ConsultarExpedientesPorTratasApiRequest request, CancellationToken cancellationToken)
    {
        if ((request.TrataIds is null || request.TrataIds.Length == 0) && string.IsNullOrWhiteSpace(request.Caratula)) return this.BadRequest("Debe seleccionar al menos una trata.");

        return this.Ok(await _consultaExpedientesService.ConsultarAsync(
            new ConsultaExpedientesRequest(request.TrataIds, request.Pagina, request.TamanioPagina, request.CampoOrden, request.DireccionOrden, request.CodigosTrata, request.EstadosActuales, request.EstadosDetalle, request.NumerosExpediente, request.FiltroFechaUltimoMovimiento, request.Caratula),
            cancellationToken));
    }

    // Busqueda puntual por texto de caratula: operacion de usuario final, con la politica de acceso general del controller.
    [HttpPost("caratula")]
    public async Task<ActionResult<ConsultaExpedientesResult>> BuscarPorCaratula([FromBody] BuscarExpedientesPorCaratulaApiRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Texto) || request.Texto.Trim().Length < 3) return this.BadRequest("Indique al menos 3 caracteres del texto de la caratula.");

        return this.Ok(await _consultaExpedientesService.ConsultarAsync(
            new ConsultaExpedientesRequest(request.TrataIds, request.Pagina, request.TamanioPagina, CodigosTrata: request.CodigosTrata, EstadosActuales: request.EstadosActuales, Caratula: request.Texto.Trim(), Skip: request.Skip, Take: request.Take, Orden: request.Orden),
            cancellationToken));
    }

    [HttpPost("caratula/valores-filtro")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> ObtenerValoresFiltroCaratula([FromBody] ValoresFiltroCaratulaApiRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Texto) || request.Texto.Trim().Length < 3) return this.BadRequest("Indique al menos 3 caracteres del texto de la caratula.");

        return this.Ok(await _consultaExpedientesService.ObtenerValoresFiltroCaratulaAsync(new ConsultaCaratulaValoresFiltroRequest(request.Texto, request.Campo, request.TrataIds), cancellationToken));
    }

    [HttpPost("cobertura-detalle")]
    public async Task<ActionResult<ConsultaCoberturaDetalleResult>> ConsultarCoberturaDetalle([FromBody] SeleccionTratasApiRequest request, CancellationToken cancellationToken)
    {
        return this.Ok(await _consultaExpedientesService.ConsultarCoberturaDetalleAsync(request.TrataIds, cancellationToken));
    }

    [HttpPost("documentos/cobertura-referencia")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<ConsultaCoberturaReferenciaResult>> ConsultarCoberturaReferencia([FromBody] SeleccionTratasApiRequest request, CancellationToken cancellationToken)
    {
        return this.Ok(await _consultaExpedientesService.ConsultarCoberturaReferenciaAsync(request.TrataIds, cancellationToken));
    }

    [HttpPost("documentos")]
    [Authorize(Policy = SeguridadInstitucional.PoliticaGestionTemasExpedientes)]
    public async Task<ActionResult<ConsultaDocumentosPorTrataResult>> ConsultarDocumentos([FromBody] ConsultarDocumentosPorTratasApiRequest request, CancellationToken cancellationToken)
    {
        if (request.TrataIds is null || request.TrataIds.Length == 0) return this.BadRequest("Debe seleccionar al menos una trata.");

        return this.Ok(await _consultaExpedientesService.ConsultarDocumentosAsync(
            new ConsultaDocumentosPorTrataRequest(request.TrataIds, request.Pagina, request.TamanioPagina, request.CodigoTipoDocumento, request.CampoOrden, request.DireccionOrden, request.NumerosExpediente, request.CodigosTrata, request.NumerosActuacion, request.Referencias, request.ReferenciaContiene, request.TiposDocumento, request.FiltroFechaCreacion, request.SoloSinReferencia, request.IncluirResumen, request.IncluirTotal),
            cancellationToken));
    }
}
