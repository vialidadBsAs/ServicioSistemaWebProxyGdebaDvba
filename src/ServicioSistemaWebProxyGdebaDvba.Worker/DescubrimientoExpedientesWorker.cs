using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public sealed class DescubrimientoExpedientesWorker : BackgroundService
{
    private const string ServicioCuota = "ws_gdeba_consultaExpediente";
    private const string MetodoCuota = "buscarDatosExpedientePorCodigosTrata";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DescubrimientoExpedientesWorker> _logger;

    public DescubrimientoExpedientesWorker(IServiceScopeFactory scopeFactory, ILogger<DescubrimientoExpedientesWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.CerrarEjecucionesInterrumpidasAsync(stoppingToken);
        DateOnly? fechaEjecutada = null;
        var esperaInformada = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            // Ninguna excepcion del ciclo debe tumbar el host (StopHost detendria los tres workers): se loguea y se continua en la proxima vuelta.
            try
            {
                var configuracion = await this.ObtenerConfiguracionAsync(stoppingToken);
                var ejecutoSolicitudManual = await this.EjecutarSolicitudManualAsync(configuracion, stoppingToken);
                var ahora = DateTimeOffset.Now;
                var fecha = DateOnly.FromDateTime(ahora.LocalDateTime);
                if (!ejecutoSolicitudManual && configuracion.Habilitado && fechaEjecutada != fecha && this.EstaDentroDeLaVentana(configuracion, ahora))
                {
                    if (await this.YaSeEjecutoCorridaProgramadaAsync(fecha, stoppingToken))
                    {
                        fechaEjecutada = fecha;
                    }
                    else if (await this.ObtenerOmisionDelDiaAsync(fecha, stoppingToken) is OmisionCorridaProgramadaDto omision)
                    {
                        await this.RegistrarCorridaOmitidaPorOperadorAsync(omision, stoppingToken);
                        fechaEjecutada = fecha;
                        esperaInformada = false;
                    }
                    else
                    {
                        await this.EjecutarCorridaProgramadaAsync(configuracion, fecha, stoppingToken);
                        fechaEjecutada = fecha;
                        esperaInformada = false;
                    }
                }
                else if (!esperaInformada)
                {
                    _logger.LogInformation("Worker de descubrimiento en espera. Habilitado={Habilitado}. HoraLocal={HoraLocal}. Ventana={HoraInicio}-{HoraFin}.", configuracion.Habilitado, ahora, configuracion.HoraInicioLocal, configuracion.HoraFinLocal);
                    esperaInformada = true;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error inesperado en el ciclo del worker de descubrimiento; el worker continua.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task<bool> EjecutarSolicitudManualAsync(ConfiguracionProgramadaWorkerDto configuracion, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        var ejecucion = await ejecucionesWorker.TomarSolicitudManualAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
        if (ejecucion is null) return false;

        try
        {
            var descubrimientoExpedientesWorkerService = scope.ServiceProvider.GetRequiredService<IDescubrimientoExpedientesWorkerService>();
            this.ConfigurarAplicacionActual(scope.ServiceProvider);
            var resultado = await descubrimientoExpedientesWorkerService.EjecutarAsync(
                ejecucion.EjecucionId,
                new DescubrirExpedientesProgramadosRequest(int.MaxValue, configuracion.ConsultasVaciasParaPausa ?? 1, configuracion.DiasPausaSinResultados ?? 1, configuracion.OmitirConsultasRealizadasEnElDia, OrigenInvocacionGdeba.Administrativo),
                cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionDescubrimientoAsync(ejecucion.EjecucionId, this.ResolverEstadoEjecucion(resultado), this.CrearResumenResultado(resultado, esManual: true), resultado.Habilitados, resultado.Creados, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "La solicitud manual de descubrimiento de expedientes finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida, "La ejecucion manual finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }

        return true;
    }

    private async Task EjecutarCorridaProgramadaAsync(ConfiguracionProgramadaWorkerDto configuracion, DateOnly fecha, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        var ejecucion = await ejecucionesWorker.IniciarEjecucionProgramadaAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
        try
        {
            _logger.LogInformation("Iniciando corrida programada de descubrimiento. Fecha={Fecha}.", fecha);
            var cuotas = scope.ServiceProvider.GetRequiredService<IConsultaCuotasGdeba>();
            var expedienteService = scope.ServiceProvider.GetRequiredService<IExpedienteService>();
            var descubrimientoExpedientesWorkerService = scope.ServiceProvider.GetRequiredService<IDescubrimientoExpedientesWorkerService>();
            this.ConfigurarAplicacionActual(scope.ServiceProvider);
            var cuota = (await cuotas.ConsultarCuotasAsync(fecha, cancellationToken)).Operaciones.FirstOrDefault(x => x.Servicio == DescubrimientoExpedientesWorker.ServicioCuota && x.Operacion == DescubrimientoExpedientesWorker.MetodoCuota);
            var reserva = Math.Max(0, configuracion.CupoReservaDiaria);
            if (cuota?.LimiteDiario is not int limite)
            {
                var motivo = cuota is null ? "No existe configuracion de cuota para la operacion." : "La operacion no tiene limite diario configurado.";
                await expedienteService.RegistrarDescubrimientoProgramadoOmitidoAsync(new RegistrarDescubrimientoProgramadoOmitidoRequest(cuota?.LimiteDiario, cuota?.Total ?? 0, reserva, motivo), cancellationToken);
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, motivo, null, null, null, null, cancellationToken);
                return;
            }

            var presupuesto = Math.Max(0, limite - cuota.Total - reserva);
            if (presupuesto <= 0)
            {
                const string motivo = "Cupo diario agotado o reservado.";
                await expedienteService.RegistrarDescubrimientoProgramadoOmitidoAsync(new RegistrarDescubrimientoProgramadoOmitidoRequest(limite, cuota.Total, reserva, motivo), cancellationToken);
                await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, motivo, null, null, null, null, cancellationToken);
                return;
            }

            var resultado = await descubrimientoExpedientesWorkerService.EjecutarAsync(
                ejecucion.EjecucionId,
                new DescubrirExpedientesProgramadosRequest(presupuesto, configuracion.ConsultasVaciasParaPausa ?? 1, configuracion.DiasPausaSinResultados ?? 1, configuracion.OmitirConsultasRealizadasEnElDia, OrigenInvocacionGdeba.WorkerProgramado),
                cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionDescubrimientoAsync(ejecucion.EjecucionId, this.ResolverEstadoEjecucion(resultado), this.CrearResumenResultado(resultado, esManual: false, presupuesto), resultado.Habilitados, resultado.Creados, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "La corrida programada de descubrimiento de expedientes finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida, "La corrida programada finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }
    }

    private async Task<ConfiguracionProgramadaWorkerDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configuracionesWorker = scope.ServiceProvider.GetRequiredService<IConfiguracionProgramadaWorkerService>();
        return await configuracionesWorker.ObtenerAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
    }

    private async Task CerrarEjecucionesInterrumpidasAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkerExecutionService ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        int cerradas = await ejecucionesWorker.CerrarEjecucionesInterrumpidasAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
        if (cerradas > 0)
        {
            _logger.LogWarning("Al iniciar el Worker se marcaron como fallidas {Cerradas} ejecuciones de descubrimiento interrumpidas por un reinicio.", cerradas);
        }
    }

    private async Task<OmisionCorridaProgramadaDto?> ObtenerOmisionDelDiaAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IOmisionCorridaProgramadaWorkerService omisionesCorridaProgramada = scope.ServiceProvider.GetRequiredService<IOmisionCorridaProgramadaWorkerService>();
        return await omisionesCorridaProgramada.ObtenerOmisionAsync(ProcesoWorker.DescubrimientoExpedientes, fecha, cancellationToken);
    }

    private async Task RegistrarCorridaOmitidaPorOperadorAsync(OmisionCorridaProgramadaDto omision, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkerExecutionService ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        EjecucionWorkerIniciada ejecucion = await ejecucionesWorker.IniciarEjecucionProgramadaAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
        string motivo = $"La corrida programada del dia fue omitida por decision del operador ({omision.OmitidaPor}).";
        await ejecucionesWorker.FinalizarEjecucionAsync(ejecucion.EjecucionId, EstadoEjecucionWorker.Omitida, motivo, null, null, null, null, cancellationToken);
        _logger.LogInformation("Corrida programada de descubrimiento omitida por decision del operador. Fecha={Fecha}. Operador={Operador}.", omision.FechaLocal, omision.OmitidaPor);
    }

    private async Task<bool> YaSeEjecutoCorridaProgramadaAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkerExecutionService ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        DateTimeOffset? ultimaEjecucionProgramada = await ejecucionesWorker.ObtenerUltimaEjecucionProgramadaAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
        return ultimaEjecucionProgramada is DateTimeOffset ultima && DateOnly.FromDateTime(ultima.LocalDateTime) == fecha;
    }

    private void ConfigurarAplicacionActual(IServiceProvider serviceProvider)
    {
        var currentApplicationAccessor = serviceProvider.GetRequiredService<ICurrentApplicationAccessor>();
        currentApplicationAccessor.Current = new CurrentApplication("worker", "Worker de descubrimiento de expedientes");
    }

    private string CrearResumenResultado(DescubrirExpedientesProgramadosResult resultado, bool esManual, int? presupuesto = null)
    {
        if (resultado.ConsultasRealizadas == 0) return this.CrearResumenOmitido(resultado, esManual);

        var tipoEjecucion = esManual ? "Ejecucion manual completada sin limite operativo de cuota." : $"Corrida programada completada con presupuesto de {presupuesto} invocaciones.";
        return $"{tipoEjecucion} Consultas: {resultado.ConsultasRealizadas}. Recibidos: {resultado.RecibidosGdeba}. Habilitados: {resultado.Habilitados}. Descartados: {resultado.Descartados}. Creados: {resultado.Creados}. Actualizados: {resultado.Actualizados}. Sin cambios: {resultado.SinCambios}. Omitidas por consulta del día: {resultado.OmitidasPorConsultaDelDia}. Omitidas por pausa: {resultado.OmitidasPorPausa}. Omitidas por límite operativo: {resultado.OmitidasPorLimiteOperativo}.";
    }

    private string CrearResumenOmitido(DescubrirExpedientesProgramadosResult resultado, bool esManual)
    {
        var motivos = new List<string>();
        if (resultado.OmitidasPorConsultaDelDia > 0) motivos.Add($"{resultado.OmitidasPorConsultaDelDia} consultas de trata y estado ya se realizaron hoy");
        if (resultado.OmitidasPorPausa > 0) motivos.Add($"{resultado.OmitidasPorPausa} consultas de trata y estado están pausadas por resultados vacíos");
        if (resultado.OmitidasPorLimiteOperativo > 0) motivos.Add($"{resultado.OmitidasPorLimiteOperativo} consultas de trata y estado no fueron seleccionadas por el límite operativo");

        var origen = esManual ? "La ejecución manual fue omitida" : "La corrida programada fue omitida";
        var detalle = motivos.Count == 0 ? "no hay consultas de trata y estado habilitadas para descubrir" : string.Join("; ", motivos);
        return $"{origen}: no se invocó GDEBA porque {detalle}.";
    }

    private EstadoEjecucionWorker ResolverEstadoEjecucion(DescubrirExpedientesProgramadosResult resultado)
    {
        return resultado.ConsultasRealizadas == 0 ? EstadoEjecucionWorker.Omitida : EstadoEjecucionWorker.Finalizada;
    }

    private bool EstaDentroDeLaVentana(ConfiguracionProgramadaWorkerDto configuracion, DateTimeOffset ahora)
    {
        var hora = TimeOnly.FromDateTime(ahora.LocalDateTime);
        if (configuracion.HoraInicioLocal == configuracion.HoraFinLocal) return true;

        return configuracion.HoraInicioLocal < configuracion.HoraFinLocal
            ? hora >= configuracion.HoraInicioLocal && hora < configuracion.HoraFinLocal
            : hora >= configuracion.HoraInicioLocal || hora < configuracion.HoraFinLocal;
    }
}
