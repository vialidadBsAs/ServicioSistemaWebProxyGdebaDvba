using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public sealed class ExpedienteDetalladoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpedienteDetalladoWorker> _logger;

    public ExpedienteDetalladoWorker(IServiceScopeFactory scopeFactory, ILogger<ExpedienteDetalladoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.CerrarEjecucionesInterrumpidasAsync(stoppingToken);
        ConfiguracionProgramadaWorkerDto configuracionInicial = await this.ObtenerConfiguracionAsync(stoppingToken);
        TimeSpan intervaloConfigurado = this.ResolverIntervalo(configuracionInicial);
        DateTimeOffset proximaEjecucionProgramada = await this.ResolverProximaEjecucionProgramadaAsync(intervaloConfigurado, stoppingToken);
        if (configuracionInicial.Habilitado && configuracionInicial.EjecutarAlIniciar)
        {
            await this.EjecutarCorridaProgramadaAsync(configuracionInicial, stoppingToken);
            proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloConfigurado);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            ConfiguracionProgramadaWorkerDto configuracion = await this.ObtenerConfiguracionAsync(stoppingToken);
            TimeSpan intervaloActual = this.ResolverIntervalo(configuracion);
            if (intervaloActual != intervaloConfigurado)
            {
                intervaloConfigurado = intervaloActual;
                proximaEjecucionProgramada = DateTimeOffset.Now.Add(intervaloConfigurado);
            }

            bool ejecutoSolicitudManual = await this.EjecutarSolicitudManualAsync(configuracion, stoppingToken);
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
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkerExecutionService ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        EjecucionWorkerIniciada? ejecucion = await ejecucionesWorker.TomarSolicitudManualAsync(ProcesoWorker.ExpedienteDetallado, cancellationToken);
        if (ejecucion is null) return false;

        try
        {
            IExpedienteDetalladoWorkerService expedienteDetalladoWorkerService = scope.ServiceProvider.GetRequiredService<IExpedienteDetalladoWorkerService>();
            this.ConfigurarAplicacionActual(scope.ServiceProvider);
            DetallarExpedientesPendientesResult resultado = await expedienteDetalladoWorkerService.DetallarPendientesAsync(Math.Max(1, configuracion.TamanoLote ?? 1), OrigenInvocacionGdeba.Administrativo, cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, this.ResolverEstadoEjecucion(resultado), this.CrearResumenResultado(resultado, esManual: true), resultado.Procesados, resultado.Detallados, null, resultado.Errores, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "La solicitud manual de expediente detallado finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida, "La ejecucion manual finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }

        return true;
    }

    private async Task EjecutarCorridaProgramadaAsync(ConfiguracionProgramadaWorkerDto configuracion, CancellationToken cancellationToken)
    {
        if (!this.EstaDentroDeLaVentana(configuracion))
        {
            _logger.LogDebug("Se omite el detalle de expedientes programado porque la hora local está fuera de la ventana configurada.");
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkerExecutionService ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        EjecucionWorkerIniciada ejecucion = await ejecucionesWorker.IniciarEjecucionProgramadaAsync(ProcesoWorker.ExpedienteDetallado, cancellationToken);
        try
        {
            IExpedienteDetalladoWorkerService expedienteDetalladoWorkerService = scope.ServiceProvider.GetRequiredService<IExpedienteDetalladoWorkerService>();
            this.ConfigurarAplicacionActual(scope.ServiceProvider);
            DetallarExpedientesPendientesResult resultado = await expedienteDetalladoWorkerService.DetallarPendientesAsync(Math.Max(1, configuracion.TamanoLote ?? 1), OrigenInvocacionGdeba.WorkerProgramado, cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, this.ResolverEstadoEjecucion(resultado), this.CrearResumenResultado(resultado, esManual: false), resultado.Procesados, resultado.Detallados, null, resultado.Errores, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "La corrida programada de expediente detallado finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida, "La corrida programada finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }
    }

    private async Task<ConfiguracionProgramadaWorkerDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IConfiguracionProgramadaWorkerService configuracionesWorker = scope.ServiceProvider.GetRequiredService<IConfiguracionProgramadaWorkerService>();
        return await configuracionesWorker.ObtenerAsync(ProcesoWorker.ExpedienteDetallado, cancellationToken);
    }

    private async Task CerrarEjecucionesInterrumpidasAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkerExecutionService ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        int cerradas = await ejecucionesWorker.CerrarEjecucionesInterrumpidasAsync(ProcesoWorker.ExpedienteDetallado, cancellationToken);
        if (cerradas > 0)
        {
            _logger.LogWarning("Al iniciar el Worker se marcaron como fallidas {Cerradas} ejecuciones de expediente detallado interrumpidas por un reinicio.", cerradas);
        }
    }

    private async Task<DateTimeOffset> ResolverProximaEjecucionProgramadaAsync(TimeSpan intervalo, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkerExecutionService ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        DateTimeOffset? ultimaEjecucionProgramada = await ejecucionesWorker.ObtenerUltimaEjecucionProgramadaAsync(ProcesoWorker.ExpedienteDetallado, cancellationToken);
        return ultimaEjecucionProgramada is DateTimeOffset ultima ? ultima.Add(intervalo) : DateTimeOffset.Now.Add(intervalo);
    }

    private void ConfigurarAplicacionActual(IServiceProvider serviceProvider)
    {
        ICurrentApplicationAccessor currentApplicationAccessor = serviceProvider.GetRequiredService<ICurrentApplicationAccessor>();
        currentApplicationAccessor.Current = new CurrentApplication("worker", "Worker de expediente detallado");
    }

    private string CrearResumenResultado(DetallarExpedientesPendientesResult resultado, bool esManual)
    {
        if (resultado.Procesados == 0)
        {
            return esManual
                ? "La ejecución manual fue omitida: no hay expedientes pendientes de detalle."
                : "La corrida programada fue omitida: no hay expedientes pendientes de detalle.";
        }

        string tipoEjecucion = esManual ? "Ejecución manual completada." : "Corrida programada completada.";
        return $"{tipoEjecucion} Expedientes procesados: {resultado.Procesados}. Detallados: {resultado.Detallados}. Errores: {resultado.Errores}. Quedan {resultado.PendientesRestantes} expedientes sin detallar.";
    }

    private EstadoEjecucionWorker ResolverEstadoEjecucion(DetallarExpedientesPendientesResult resultado)
    {
        return resultado.Procesados == 0 ? EstadoEjecucionWorker.Omitida : EstadoEjecucionWorker.Finalizada;
    }

    private TimeSpan ResolverIntervalo(ConfiguracionProgramadaWorkerDto configuracion)
    {
        return TimeSpan.FromMinutes(Math.Max(1, configuracion.IntervaloMinutos ?? 1));
    }

    private bool EstaDentroDeLaVentana(ConfiguracionProgramadaWorkerDto configuracion)
    {
        TimeOnly horaActual = TimeOnly.FromDateTime(DateTime.Now);
        if (configuracion.HoraInicioLocal == configuracion.HoraFinLocal) return true;

        return configuracion.HoraInicioLocal < configuracion.HoraFinLocal
            ? horaActual >= configuracion.HoraInicioLocal && horaActual < configuracion.HoraFinLocal
            : horaActual >= configuracion.HoraInicioLocal || horaActual < configuracion.HoraFinLocal;
    }
}
