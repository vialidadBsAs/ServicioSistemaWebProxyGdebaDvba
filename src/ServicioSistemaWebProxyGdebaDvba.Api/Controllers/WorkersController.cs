using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    private readonly IConfiguracionProgramadaWorkerService _configuracionProgramadaWorkerService;
    private readonly IConfiguracionDatosWorkerService _configuracionDatosWorkerService;
    private readonly IOmisionCorridaProgramadaWorkerService _omisionCorridaProgramadaWorkerService;
    private readonly IPanelEjecucionesWorkerReadStore _panelEjecucionesWorkerReadStore;

    public WorkersController(
        IWorkerExecutionService workerExecutionService,
        IConfiguracionProgramadaWorkerService configuracionProgramadaWorkerService,
        IConfiguracionDatosWorkerService configuracionDatosWorkerService,
        IOmisionCorridaProgramadaWorkerService omisionCorridaProgramadaWorkerService,
        IPanelEjecucionesWorkerReadStore panelEjecucionesWorkerReadStore)
    {
        _workerExecutionService = workerExecutionService;
        _configuracionProgramadaWorkerService = configuracionProgramadaWorkerService;
        _configuracionDatosWorkerService = configuracionDatosWorkerService;
        _omisionCorridaProgramadaWorkerService = omisionCorridaProgramadaWorkerService;
        _panelEjecucionesWorkerReadStore = panelEjecucionesWorkerReadStore;
    }

    [HttpGet("configuraciones")]
    public async Task<ActionResult<IReadOnlyCollection<ConfiguracionProgramadaWorkerResponse>>> ConsultarConfiguraciones(CancellationToken cancellationToken)
    {
        var configuraciones = await _configuracionProgramadaWorkerService.ConsultarAsync(cancellationToken);
        return this.Ok(configuraciones.Select(ConfiguracionProgramadaWorkerResponse.Create).ToArray());
    }

    [HttpGet("{proceso}/configuracion")]
    public async Task<ActionResult<ConfiguracionProgramadaWorkerResponse>> ConsultarConfiguracion(string proceso, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        try
        {
            ConfiguracionProgramadaWorkerDto configuracion = await _configuracionProgramadaWorkerService.ObtenerAsync(procesoWorker, cancellationToken);
            return this.Ok(ConfiguracionProgramadaWorkerResponse.Create(configuracion));
        }
        catch (InvalidOperationException exception)
        {
            return this.NotFound(exception.Message);
        }
    }

    [HttpPut("{proceso}/configuracion")]
    public async Task<ActionResult<ConfiguracionProgramadaWorkerResponse>> GuardarConfiguracion(string proceso, [FromBody] GuardarConfiguracionProgramadaWorkerApiRequest request, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        try
        {
            var configuracion = await _configuracionProgramadaWorkerService.GuardarAsync(
                new GuardarConfiguracionProgramadaWorkerRequest(
                    procesoWorker,
                    request.Habilitado,
                    request.HoraInicioLocal,
                    request.HoraFinLocal,
                    request.CupoReservaDiaria,
                    request.IntervaloMinutos,
                    request.EjecutarAlIniciar,
                    request.TamanoLote,
                    request.ConsultasVaciasParaPausa,
                    request.DiasPausaSinResultados,
                    request.OmitirConsultasRealizadasEnElDia),
                cancellationToken);
            return this.Ok(ConfiguracionProgramadaWorkerResponse.Create(configuracion));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return this.BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return this.BadRequest(exception.Message);
        }
    }

    [HttpGet("{proceso}/datos")]
    public async Task<ActionResult<ConfiguracionDatosWorkerResponse>> ConsultarDatos(string proceso, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        var configuracion = await _configuracionDatosWorkerService.ConsultarAsync(procesoWorker, cancellationToken);
        return this.Ok(ConfiguracionDatosWorkerResponse.Create(configuracion));
    }

    [HttpPut("{proceso}/datos/temas/{temaExpedienteId:guid}")]
    public async Task<ActionResult<ConfiguracionTemaWorkerResponse>> GuardarTema(string proceso, Guid temaExpedienteId, [FromBody] GuardarConfiguracionTemaWorkerApiRequest request, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        try
        {
            var configuracion = await _configuracionDatosWorkerService.GuardarTemaAsync(
                new GuardarConfiguracionTemaWorkerRequest(procesoWorker, temaExpedienteId, request.Habilitado, request.Prioridad), cancellationToken);
            return this.Ok(ConfiguracionTemaWorkerResponse.Create(configuracion));
        }
        catch (ArgumentException exception)
        {
            return this.BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return this.BadRequest(exception.Message);
        }
    }

    [HttpDelete("{proceso}/datos/temas/{temaExpedienteId:guid}")]
    public async Task<IActionResult> QuitarTema(string proceso, Guid temaExpedienteId, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        await _configuracionDatosWorkerService.QuitarTemaAsync(procesoWorker, temaExpedienteId, cancellationToken);
        return this.NoContent();
    }

    [HttpPut("descubrimiento-expedientes/datos/tratas/{codigoTrata}")]
    public async Task<ActionResult<ConfiguracionTrataDescubrimientoWorkerResponse>> GuardarTrataDescubrimiento(string codigoTrata, [FromBody] GuardarConfiguracionTrataDescubrimientoWorkerApiRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var configuracion = await _configuracionDatosWorkerService.GuardarTrataDescubrimientoAsync(
                new GuardarConfiguracionTrataDescubrimientoWorkerRequest(codigoTrata, request.Habilitada, request.Prioridad), cancellationToken);
            return this.Ok(ConfiguracionTrataDescubrimientoWorkerResponse.Create(configuracion));
        }
        catch (ArgumentException exception)
        {
            return this.BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return this.BadRequest(exception.Message);
        }
    }

    [HttpDelete("descubrimiento-expedientes/datos/tratas/{codigoTrata}")]
    public async Task<IActionResult> QuitarTrataDescubrimiento(string codigoTrata, CancellationToken cancellationToken)
    {
        await _configuracionDatosWorkerService.QuitarTrataDescubrimientoAsync(codigoTrata, cancellationToken);
        return this.NoContent();
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
    public async Task<ActionResult<SolicitudEjecucionWorkerResponse>> CrearSolicitudManual(string proceso, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SolicitarEjecucionManualWorkerApiRequest? request, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        string solicitadaPor = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value ?? "Administracion";
        try
        {
            SolicitudEjecucionWorkerDto solicitud = await _workerExecutionService.SolicitarEjecucionManualAsync(
                new SolicitarEjecucionManualWorkerRequest(procesoWorker, solicitadaPor, request?.FechaInicioProgramada), cancellationToken);
            return this.CreatedAtAction(nameof(WorkersController.ConsultarPanel), new { proceso }, SolicitudEjecucionWorkerResponse.Create(solicitud));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return this.BadRequest(exception.Message);
        }
    }

    [HttpPost("solicitudes/{solicitudId:guid}/cancelar")]
    public async Task<ActionResult<SolicitudEjecucionWorkerResponse>> CancelarSolicitudManual(Guid solicitudId, CancellationToken cancellationToken)
    {
        string canceladaPor = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value ?? "Administracion";
        try
        {
            SolicitudEjecucionWorkerDto solicitud = await _workerExecutionService.CancelarSolicitudManualAsync(solicitudId, canceladaPor, cancellationToken);
            return this.Ok(SolicitudEjecucionWorkerResponse.Create(solicitud));
        }
        catch (InvalidOperationException exception)
        {
            return this.BadRequest(exception.Message);
        }
    }

    [HttpPut("{proceso}/omisiones/hoy")]
    public async Task<ActionResult> OmitirCorridaDelDia(string proceso, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        string omitidaPor = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value ?? "Administracion";
        try
        {
            await _omisionCorridaProgramadaWorkerService.OmitirCorridaDelDiaAsync(procesoWorker, omitidaPor, cancellationToken);
            return this.NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return this.BadRequest(exception.Message);
        }
    }

    [HttpDelete("{proceso}/omisiones/hoy")]
    public async Task<IActionResult> QuitarOmisionDelDia(string proceso, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        await _omisionCorridaProgramadaWorkerService.QuitarOmisionDelDiaAsync(procesoWorker, cancellationToken);
        return this.NoContent();
    }

    [HttpGet("{proceso}/panel")]
    public async Task<ActionResult<PanelEjecucionesWorkerResponse>> ConsultarPanel(string proceso, [FromQuery] int? cantidadHistorico, CancellationToken cancellationToken)
    {
        if (!WorkersController.TryResolverProceso(proceso, out var procesoWorker))
        {
            return this.BadRequest("El proceso de Worker solicitado no es valido.");
        }

        try
        {
            ConsultaPanelEjecucionesWorkerResult resultado = await _panelEjecucionesWorkerReadStore.ConsultarAsync(procesoWorker, cantidadHistorico ?? 30, cancellationToken);
            return this.Ok(PanelEjecucionesWorkerResponse.Create(resultado));
        }
        catch (InvalidOperationException exception)
        {
            return this.NotFound(exception.Message);
        }
    }

    [HttpPost("solicitudes/{solicitudId:guid}/iniciar")]
    public async Task<ActionResult<SolicitudEjecucionWorkerResponse>> IniciarSolicitudManual(Guid solicitudId, CancellationToken cancellationToken)
    {
        try
        {
            SolicitudEjecucionWorkerDto solicitud = await _workerExecutionService.IniciarSolicitudManualAsync(solicitudId, cancellationToken);
            return this.Accepted(SolicitudEjecucionWorkerResponse.Create(solicitud));
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
