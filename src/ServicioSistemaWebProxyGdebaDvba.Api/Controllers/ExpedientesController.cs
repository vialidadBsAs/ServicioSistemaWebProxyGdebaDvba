using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Route("api/gdeba/expedientes")]
public sealed class ExpedientesController : ControllerBase
{
    private readonly IExpedienteService _expedienteService;

    public ExpedientesController(IExpedienteService expedienteService)
    {
        _expedienteService = expedienteService;
    }

    [HttpGet("{numeroGdebaCompleto}/cabecera")]
    public async Task<ActionResult<ObtenerExpedienteRecursoResult<CabeceraExpedienteDto>>> ObtenerCabecera(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ObtenerCabeceraAsync(
            new ObtenerExpedienteRecursoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }

    [HttpGet("{numeroGdebaCompleto}/documentos")]
    public async Task<ActionResult<ObtenerExpedienteRecursoResult<IReadOnlyCollection<DocumentoExpedienteDto>>>> ObtenerDocumentos(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ObtenerDocumentosAsync(
            new ObtenerExpedienteRecursoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }

    [HttpGet("{numeroGdebaCompleto}/adjuntos")]
    public async Task<ActionResult<ObtenerExpedienteRecursoResult<IReadOnlyCollection<ArchivoAdjuntoExpedienteDto>>>> ObtenerAdjuntos(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ObtenerAdjuntosAsync(
            new ObtenerExpedienteRecursoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }

    [HttpGet("{numeroGdebaCompleto}/pases")]
    public async Task<ActionResult<ObtenerExpedienteRecursoResult<IReadOnlyCollection<MovimientoExpedienteDto>>>> ObtenerPases(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ObtenerPasesAsync(
            new ObtenerExpedienteRecursoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }

    [HttpGet("{numeroGdebaCompleto}/relaciones")]
    public async Task<ActionResult<ObtenerExpedienteRecursoResult<IReadOnlyCollection<RelacionExpedienteDto>>>> ObtenerRelaciones(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ObtenerRelacionesAsync(
            new ObtenerExpedienteRecursoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }

    [HttpGet("{numeroGdebaCompleto}/completo")]
    public async Task<ActionResult<ObtenerExpedienteRecursoResult<ExpedienteCompletoDto>>> ObtenerCompleto(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ObtenerCompletoAsync(
            new ObtenerExpedienteRecursoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }

    [HttpGet("{numeroGdebaCompleto}/detalle")]
    public async Task<ActionResult<ConsultarExpedienteDetalladoResult>> ConsultarDetalle(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ConsultarDetalleAsync(
            new ConsultarExpedienteDetalladoRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Expediente is null ? NotFound(result) : Ok(result);
    }

    [HttpGet("{numeroGdebaCompleto}/movimientos")]
    public async Task<ActionResult<ConsultarMovimientosExpedienteResult>> ConsultarMovimientos(
        string numeroGdebaCompleto,
        [FromQuery] bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ConsultarMovimientosAsync(
            new ConsultarMovimientosExpedienteRequest(numeroGdebaCompleto, forceRefresh),
            cancellationToken);

        return result.Exitoso ? Ok(result) : NotFound(result);
    }
    [HttpGet("{numeroGdebaCompleto}/sin-cache")]
    public async Task<ActionResult<ConsultarExpedienteSinCacheResult>> ConsultarSinCache(
       string numeroGdebaCompleto,
       CancellationToken cancellationToken)
    {
        var result = await _expedienteService.ConsultarExpedienteSinCacheAsync(
            new ConsultarExpedienteSinCacheRequest(numeroGdebaCompleto),
            cancellationToken);

        return result.Expediente is null ? NotFound(result) : Ok(result);
    }
}
