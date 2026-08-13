using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public sealed class DescubrimientoExpedientesWorker : BackgroundService
{
    private readonly DescubrimientoExpedientesWorkerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DescubrimientoExpedientesWorker> _logger;

    public DescubrimientoExpedientesWorker(IServiceScopeFactory scopeFactory, IOptions<DescubrimientoExpedientesWorkerOptions> options, ILogger<DescubrimientoExpedientesWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Worker de descubrimiento programado deshabilitado. Continuará atendiendo solicitudes manuales.");
        }

        DateOnly? fechaEjecutada = null;
        var esperaInformada = false;
        _logger.LogInformation("Worker de descubrimiento de expedientes iniciado. Ventana={HoraInicio:00}:00-{HoraFin:00}:00. Intervalo de solicitudes manuales=1 minuto.", _options.HoraInicioLocal, _options.HoraFinLocal);
        while (!stoppingToken.IsCancellationRequested)
        {
            var ejecutoSolicitudManual = await this.EjecutarSolicitudManualAsync(stoppingToken);
            var ahora = DateTimeOffset.Now;
            var fecha = DateOnly.FromDateTime(ahora.LocalDateTime);
            if (!ejecutoSolicitudManual && _options.Enabled && fechaEjecutada != fecha && this.EstaDentroDeLaVentana(ahora))
            {
                await this.EjecutarCorridaProgramadaAsync(fecha, stoppingToken);
                fechaEjecutada = fecha;
            }
            else if (!esperaInformada)
            {
                _logger.LogInformation("Worker de descubrimiento en espera. HoraLocal={HoraLocal}. Ventana={HoraInicio:00}:00-{HoraFin:00}:00.", ahora, _options.HoraInicioLocal, _options.HoraFinLocal);
                esperaInformada = true;
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task<bool> EjecutarSolicitudManualAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        var ejecucion = await ejecucionesWorker.TomarSolicitudManualAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
        if (ejecucion is null)
        {
            return false;
        }

        try
        {
            var expedienteService = scope.ServiceProvider.GetRequiredService<IExpedienteService>();
            this.ConfigurarAplicacionActual(scope.ServiceProvider);
            var resultado = await expedienteService.DescubrirExpedientesProgramadosAsync(
                new DescubrirExpedientesProgramadosRequest(int.MaxValue, _options.ConsultasVaciasParaPausa, _options.DiasPausaSinResultados, _options.OmitirConsultasRealizadasEnElDia, OrigenInvocacionGdeba.Administrativo),
                cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionDescubrimientoAsync(
                ejecucion.EjecucionId, this.ResolverEstadoEjecucion(resultado),
                this.CrearResumenResultado(resultado, esManual: true), resultado.RecibidosGdeba, resultado.Creados, resultado.ResultadosPorTrataEstado, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La solicitud manual de descubrimiento de expedientes finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(
                ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida,
                "La ejecucion manual finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }

        return true;
    }

    private async Task EjecutarCorridaProgramadaAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ejecucionesWorker = scope.ServiceProvider.GetRequiredService<IWorkerExecutionService>();
        var ejecucion = await ejecucionesWorker.IniciarEjecucionProgramadaAsync(ProcesoWorker.DescubrimientoExpedientes, cancellationToken);
        try
        {
            _logger.LogInformation("Iniciando corrida programada de descubrimiento. Fecha={Fecha}.", fecha);
            var cuotas = scope.ServiceProvider.GetRequiredService<IConsultaCuotasGdeba>();
            var expedienteService = scope.ServiceProvider.GetRequiredService<IExpedienteService>();
            this.ConfigurarAplicacionActual(scope.ServiceProvider);
            var cuota = (await cuotas.ConsultarCuotasAsync(fecha, cancellationToken)).Operaciones.FirstOrDefault(x => x.Servicio == _options.ServicioCuota && x.Operacion == _options.MetodoCuota);
            var reserva = Math.Max(0, _options.CupoReservaDiaria);
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

            var resultado = await expedienteService.DescubrirExpedientesProgramadosAsync(
                new DescubrirExpedientesProgramadosRequest(presupuesto, _options.ConsultasVaciasParaPausa, _options.DiasPausaSinResultados, _options.OmitirConsultasRealizadasEnElDia, OrigenInvocacionGdeba.WorkerProgramado),
                cancellationToken);
            await ejecucionesWorker.FinalizarEjecucionDescubrimientoAsync(
                ejecucion.EjecucionId, this.ResolverEstadoEjecucion(resultado),
                this.CrearResumenResultado(resultado, esManual: false, presupuesto), resultado.RecibidosGdeba, resultado.Creados, resultado.ResultadosPorTrataEstado, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La corrida programada de descubrimiento de expedientes finalizo con error.");
            await ejecucionesWorker.FinalizarEjecucionAsync(
                ejecucion.EjecucionId, EstadoEjecucionWorker.Fallida,
                "La corrida programada finalizo con error. Consulte el registro tecnico.", null, null, null, 1, CancellationToken.None);
        }
    }

    private void ConfigurarAplicacionActual(IServiceProvider serviceProvider)
    {
        var currentApplicationAccessor = serviceProvider.GetRequiredService<ICurrentApplicationAccessor>();
        currentApplicationAccessor.Current = new CurrentApplication("worker", "Worker de descubrimiento de expedientes");
    }

    private string CrearResumenResultado(DescubrirExpedientesProgramadosResult resultado, bool esManual, int? presupuesto = null)
    {
        if (resultado.ConsultasRealizadas == 0)
        {
            return this.CrearResumenOmitido(resultado, esManual);
        }

        var tipoEjecucion = esManual
                ? "Ejecucion manual completada sin limite operativo de cuota."
                : $"Corrida programada completada con presupuesto de {presupuesto} invocaciones.";
        return $"{tipoEjecucion} Consultas: {resultado.ConsultasRealizadas}. Recibidos: {resultado.RecibidosGdeba}. Habilitados: {resultado.Habilitados}. Descartados: {resultado.Descartados}. Creados: {resultado.Creados}. Actualizados: {resultado.Actualizados}. Sin cambios: {resultado.SinCambios}. Omitidas por consulta del día: {resultado.OmitidasPorConsultaDelDia}. Omitidas por pausa: {resultado.OmitidasPorPausa}. Omitidas por límite operativo: {resultado.OmitidasPorLimiteOperativo}.";
    }

    private string CrearResumenOmitido(DescubrirExpedientesProgramadosResult resultado, bool esManual)
    {
        var motivos = new List<string>();
        if (resultado.OmitidasPorConsultaDelDia > 0)
        {
            motivos.Add($"{resultado.OmitidasPorConsultaDelDia} consultas de trata y estado ya se realizaron hoy");
        }

        if (resultado.OmitidasPorPausa > 0)
        {
            motivos.Add($"{resultado.OmitidasPorPausa} consultas de trata y estado están pausadas por resultados vacíos");
        }

        if (resultado.OmitidasPorLimiteOperativo > 0)
        {
            motivos.Add($"{resultado.OmitidasPorLimiteOperativo} consultas de trata y estado no fueron seleccionadas por el límite operativo");
        }

        var origen = esManual ? "La ejecución manual fue omitida" : "La corrida programada fue omitida";
        var detalle = motivos.Count == 0
            ? "no hay consultas de trata y estado habilitadas para descubrir"
            : string.Join("; ", motivos);
        return $"{origen}: no se invocó GDEBA porque {detalle}.";
    }

    private EstadoEjecucionWorker ResolverEstadoEjecucion(DescubrirExpedientesProgramadosResult resultado)
    {
        return resultado.ConsultasRealizadas == 0
            ? EstadoEjecucionWorker.Omitida
            : EstadoEjecucionWorker.Finalizada;
    }

    private bool EstaDentroDeLaVentana(DateTimeOffset ahora)
    {
        var hora = ahora.Hour;
        return _options.HoraInicioLocal < _options.HoraFinLocal
            ? hora >= _options.HoraInicioLocal && hora < _options.HoraFinLocal
            : hora >= _options.HoraInicioLocal || hora < _options.HoraFinLocal;
    }
}
