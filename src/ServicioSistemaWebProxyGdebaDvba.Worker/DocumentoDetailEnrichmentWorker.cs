using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public sealed class DocumentoDetailEnrichmentWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentoDetailEnrichmentWorker> _logger;
    private readonly DocumentoDetailEnrichmentWorkerOptions _options;

    public DocumentoDetailEnrichmentWorker(IServiceScopeFactory scopeFactory, IOptions<DocumentoDetailEnrichmentWorkerOptions> options, ILogger<DocumentoDetailEnrichmentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Worker de enriquecimiento documental programado deshabilitado. Continuará atendiendo solicitudes manuales.");
        }

        var intervaloProgramado = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
        var proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloProgramado);
        if (_options.Enabled && _options.RunOnStartup)
        {
            await this.EjecutarCorridaProgramadaAsync(stoppingToken);
            proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloProgramado);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var ejecutoSolicitudManual = await this.EjecutarSolicitudManualAsync(stoppingToken);
            if (!ejecutoSolicitudManual && _options.Enabled && DateTimeOffset.Now >= proximaEjecucionProgramada)
            {
                await this.EjecutarCorridaProgramadaAsync(stoppingToken);
                proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloProgramado);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task<bool> EjecutarSolicitudManualAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        var ejecucion = await ejecucionesWorker.TomarSolicitudManualAsync(ProcesoWorker.EnriquecimientoDetalleDocumental, cancellationToken);
        if (ejecucion is null)
        {
            return false;
        }

        try
        {
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IDocumentoDetailEnrichmentService>();
            var resultado = await enrichmentService.EnriquecerPendientesAsync(
                Math.Max(1, _options.BatchSize), OrigenInvocacionGdeba.Administrativo, cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionAsync(
                ejecucion.EjecucionId, EstadoEjecucionWorker.Finalizada,
                "Ejecucion manual completada sin limite operativo de cuota.",
                resultado.Procesados, resultado.Enriquecidos, resultado.SinDatos, resultado.Errores, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La solicitud manual de enriquecimiento documental finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(
                ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida,
                "La ejecucion manual finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }

        return true;
    }

    private async Task EjecutarCorridaProgramadaAsync(CancellationToken cancellationToken)
    {
        if (!this.EstaDentroDeLaVentanaNoPico())
        {
            _logger.LogDebug("Se omite el enriquecimiento documental programado porque la hora local está fuera de la ventana no pico.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        var ejecucion = await ejecucionesWorker.IniciarEjecucionProgramadaAsync(ProcesoWorker.EnriquecimientoDetalleDocumental, cancellationToken);
        try
        {
            var consultaCuotas = scope.ServiceProvider.GetRequiredService<IConsultaCuotasGdeba>();
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IDocumentoDetailEnrichmentService>();
            var cuotas = await consultaCuotas.ConsultarCuotasAsync(DateOnly.FromDateTime(DateTime.Now), cancellationToken);
            var cuota = cuotas.Operaciones.FirstOrDefault(x =>
                string.Equals(x.Servicio, _options.ServicioCuota, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Operacion, _options.MetodoCuota, StringComparison.OrdinalIgnoreCase));
            if (cuota is null)
            {
                const string resumen = "No existe configuracion de cuota para la operacion documental.";
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, resumen, null, null, null, null, cancellationToken);
                return;
            }

            if (cuota.LimiteDiario is not int limiteDiario)
            {
                const string resumen = "La operacion documental no tiene limite diario configurado.";
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, resumen, null, null, null, null, cancellationToken);
                return;
            }

            var loteAutorizado = this.CalcularLoteAutorizado(cuota, limiteDiario);
            if (loteAutorizado <= 0)
            {
                const string resumen = "Cupo diario agotado o reservado.";
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, resumen, null, null, null, null, cancellationToken);
                return;
            }

            var resultado = await enrichmentService.EnriquecerPendientesAsync(loteAutorizado, OrigenInvocacionGdeba.WorkerProgramado, cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionAsync(
                ejecucion.EjecucionId, EstadoEjecucionWorker.Finalizada,
                $"Enriquecimiento documental finalizado. Lote autorizado: {loteAutorizado}.",
                resultado.Procesados, resultado.Enriquecidos, resultado.SinDatos, resultado.Errores, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La corrida programada de enriquecimiento documental finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(
                ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida,
                "La corrida programada finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }
    }

    private int CalcularLoteAutorizado(ConsumoCuotaOperacionGdebaDto cuota, int limiteDiario)
    {
        var remanente = limiteDiario - cuota.Total;
        var disponible = remanente - Math.Max(0, _options.CupoReservaDiaria);
        return Math.Max(0, Math.Min(Math.Max(1, _options.BatchSize), disponible));
    }

    private bool EstaDentroDeLaVentanaNoPico()
    {
        var horaActual = TimeOnly.FromDateTime(DateTime.Now);
        var inicio = DocumentoDetailEnrichmentWorker.CrearHora(_options.VentanaInicioHoraLocal);
        var fin = DocumentoDetailEnrichmentWorker.CrearHora(_options.VentanaFinHoraLocal);
        if (inicio == fin)
        {
            return true;
        }

        return inicio < fin ? horaActual >= inicio && horaActual < fin : horaActual >= inicio || horaActual < fin;
    }

    private static TimeOnly CrearHora(int hora)
    {
        var horaNormalizada = ((hora % 24) + 24) % 24;
        return new TimeOnly(horaNormalizada, 0);
    }
}
