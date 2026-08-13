using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioSistemaWebProxyGdebaDvba.Api.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Controllers;

[ApiController]
[Authorize(Policy = SeguridadInstitucional.PoliticaAdministracionExpedientes)]
[Route("api/gdeba/workers")]
public sealed class WorkersController : ControllerBase
{
    private readonly IWorkerExecutionService _workerExecutionService;

    public WorkersController(IWorkerExecutionService workerExecutionService)
    {
        _workerExecutionService = workerExecutionService;
    }

    [HttpGet]
    public async Task<ActionResult<MonitoreoWorkersResponse>> Consultar([FromQuery] int? cantidad, CancellationToken cancellationToken)
    {
        var resultado = await _workerExecutionService.ConsultarAsync(cantidad ?? 30, cancellationToken);
        return this.Ok(MonitoreoWorkersResponse.Create(resultado));
    }

    [HttpGet("{ejecucionId:guid}/descubrimiento")]
    public async Task<ActionResult<DetalleEjecucionDescubrimientoResponse>> ConsultarDetalleDescubrimiento(Guid ejecucionId, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _workerExecutionService.ConsultarDetalleDescubrimientoAsync(ejecucionId, cancellationToken);
            return this.Ok(DetalleEjecucionDescubrimientoResponse.Create(resultado));
        }
        catch (InvalidOperationException exception)
        {
            return this.NotFound(exception.Message);
        }
    }

    [HttpPost("{proceso}/solicitudes")]
    public async Task<ActionResult<SolicitudEjecucionWorkerResponse>> CrearSolicitudManual(string proceso, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        var solicitadaPor = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value ?? "Administracion";
        var solicitud = await _workerExecutionService.SolicitarEjecucionManualAsync(
            new SolicitarEjecucionManualWorkerRequest(procesoWorker, solicitadaPor), cancellationToken);
        return this.CreatedAtAction(
            nameof(WorkersController.Consultar),
            new { cantidad = 30 },
            SolicitudEjecucionWorkerResponse.Create(solicitud));
    }

    [HttpPost("solicitudes/{solicitudId:guid}/iniciar")]
    public async Task<ActionResult<SolicitudEjecucionWorkerResponse>> IniciarSolicitudManual(Guid solicitudId, CancellationToken cancellationToken)
    {
        try
        {
            var solicitud = await _workerExecutionService.IniciarSolicitudManualAsync(solicitudId, cancellationToken);
            return this.AcceptedAtAction(
                nameof(WorkersController.Consultar),
                new { cantidad = 30 },
                SolicitudEjecucionWorkerResponse.Create(solicitud));
        }
        catch (InvalidOperationException exception)
        {
            return this.BadRequest(exception.Message);
        }
    }

    private static bool TryResolverProceso(string value, out ProcesoWorker proceso)
    {
        proceso = value.Trim().ToLowerInvariant() switch
        {
            "descubrimiento-expedientes" => ProcesoWorker.DescubrimientoExpedientes,
            "enriquecimiento-documental" => ProcesoWorker.EnriquecimientoDetalleDocumental,
            _ => default
        };
        return value.Trim().Equals("descubrimiento-expedientes", StringComparison.OrdinalIgnoreCase) ||
            value.Trim().Equals("enriquecimiento-documental", StringComparison.OrdinalIgnoreCase);
    }
}
