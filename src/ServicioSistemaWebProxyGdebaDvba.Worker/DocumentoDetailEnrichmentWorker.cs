using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public sealed class DocumentoDetailEnrichmentWorker : BackgroundService
{
    private const string ServicioCuota = "ws_gdeba_consultaDocumento";
    private const string MetodoCuota = "buscarDetallePorNumero";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentoDetailEnrichmentWorker> _logger;

    public DocumentoDetailEnrichmentWorker(IServiceScopeFactory scopeFactory, ILogger<DocumentoDetailEnrichmentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuracionInicial = await this.ObtenerConfiguracionAsync(stoppingToken);
        var intervaloConfigurado = this.ResolverIntervalo(configuracionInicial);
        var proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloConfigurado);
        if (configuracionInicial.Habilitado && configuracionInicial.EjecutarAlIniciar)
        {
            await this.EjecutarCorridaProgramadaAsync(configuracionInicial, stoppingToken);
            proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloConfigurado);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var configuracion = await this.ObtenerConfiguracionAsync(stoppingToken);
            var intervaloActual = this.ResolverIntervalo(configuracion);
            if (intervaloActual != intervaloConfigurado)
            {
                intervaloConfigurado = intervaloActual;
                proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloConfigurado);
            }

            var ejecutoSolicitudManual = await this.EjecutarSolicitudManualAsync(configuracion, stoppingToken);
            if (!ejecutoSolicitudManual && configuracion.Habilitado && DateTimeOffset.Now >= proximaEjecucionProgramada)
            {
                await this.EjecutarCorridaProgramadaAsync(configuracion, stoppingToken);
                proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloConfigurado);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task<bool> EjecutarSolicitudManualAsync(ConfiguracionProgramadaWorkerDto configuracion, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        var ejecucion = await ejecucionesWorker.TomarSolicitudManualAsync(ProcesoWorker.EnriquecimientoDetalleDocumental, cancellationToken);
        if (ejecucion is null) return false;

        try
        {
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IDocumentoDetailEnrichmentService>();
            var resultado = await enrichmentService.EnriquecerPendientesAsync(Math.Max(1, configuracion.TamanoLote ?? 1), OrigenInvocacionGdeba.Administrativo, cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Finalizada, "Ejecucion manual completada sin limite operativo de cuota.", resultado.Procesados, resultado.Enriquecidos, resultado.SinDatos, resultado.Errores, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "La solicitud manual de enriquecimiento documental finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida, "La ejecucion manual finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }

        return true;
    }

    private async Task EjecutarCorridaProgramadaAsync(ConfiguracionProgramadaWorkerDto configuracion, CancellationToken cancellationToken)
    {
        if (!this.EstaDentroDeLaVentana(configuracion))
        {
            _logger.LogDebug("Se omite el enriquecimiento documental programado porque la hora local está fuera de la ventana configurada.");
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
                string.Equals(x.Servicio, DocumentoDetailEnrichmentWorker.ServicioCuota, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Operacion, DocumentoDetailEnrichmentWorker.MetodoCuota, StringComparison.OrdinalIgnoreCase));
            if (cuota is null)
            {
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, "No existe configuracion de cuota para la operacion documental.", null, null, null, null, cancellationToken);
                return;
            }

            if (cuota.LimiteDiario is not int limiteDiario)
            {
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, "La operacion documental no tiene limite diario configurado.", null, null, null, null, cancellationToken);
                return;
            }

            var loteAutorizado = this.CalcularLoteAutorizado(cuota, limiteDiario, configuracion);
            if (loteAutorizado <= 0)
            {
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, "Cupo diario agotado o reservado.", null, null, null, null, cancellationToken);
                return;
            }

            var resultado = await enrichmentService.EnriquecerPendientesAsync(loteAutorizado, OrigenInvocacionGdeba.WorkerProgramado, cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Finalizada, $"Enriquecimiento documental finalizado. Lote autorizado: {loteAutorizado}.", resultado.Procesados, resultado.Enriquecidos, resultado.SinDatos, resultado.Errores, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "La corrida programada de enriquecimiento documental finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida, "La corrida programada finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }
    }

    private async Task<ConfiguracionProgramadaWorkerDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configuracionesWorker = scope.ServiceProvider.GetRequiredService<IConfiguracionProgramadaWorkerService>();
        return await configuracionesWorker.ObtenerAsync(ProcesoWorker.EnriquecimientoDetalleDocumental, cancellationToken);
    }

    private int CalcularLoteAutorizado(ConsumoCuotaOperacionGdebaDto cuota, int limiteDiario, ConfiguracionProgramadaWorkerDto configuracion)
    {
        var remanente = limiteDiario - cuota.Total;
        var disponible = remanente - Math.Max(0, configuracion.CupoReservaDiaria);
        return Math.Max(0, Math.Min(Math.Max(1, configuracion.TamanoLote ?? 1), disponible));
    }

    private TimeSpan ResolverIntervalo(ConfiguracionProgramadaWorkerDto configuracion)
    {
        return TimeSpan.FromMinutes(Math.Max(1, configuracion.IntervaloMinutos ?? 1));
    }

    private bool EstaDentroDeLaVentana(ConfiguracionProgramadaWorkerDto configuracion)
    {
        var horaActual = TimeOnly.FromDateTime(DateTime.Now);
        if (configuracion.HoraInicioLocal == configuracion.HoraFinLocal) return true;

        return configuracion.HoraInicioLocal < configuracion.HoraFinLocal
            ? horaActual >= configuracion.HoraInicioLocal && horaActual < configuracion.HoraFinLocal
            : horaActual >= configuracion.HoraInicioLocal || horaActual < configuracion.HoraFinLocal;
    }
}
