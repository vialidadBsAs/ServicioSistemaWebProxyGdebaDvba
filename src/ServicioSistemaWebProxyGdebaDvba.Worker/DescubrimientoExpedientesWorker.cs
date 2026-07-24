using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Models;
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
            _logger.LogInformation("Worker de descubrimiento de expedientes deshabilitado por configuracion.");
            return;
        }

        DateOnly? fechaEjecutada = null;
        var esperaInformada = false;
        _logger.LogInformation("Worker de descubrimiento de expedientes iniciado. Ventana={HoraInicio:00}:00-{HoraFin:00}:00. Intervalo={IntervaloMinutos} minutos.", _options.HoraInicioLocal, _options.HoraFinLocal, _options.IntervalMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            var ahora = DateTimeOffset.Now;
            var fecha = DateOnly.FromDateTime(ahora.LocalDateTime);
            if (fechaEjecutada != fecha && this.EstaDentroDeLaVentana(ahora))
            {
                try
                {
                    _logger.LogInformation("Iniciando corrida programada de descubrimiento. Fecha={Fecha}. HoraLocal={HoraLocal}.", fecha, ahora);
                    using var scope = _scopeFactory.CreateScope();
                    var cuotas = scope.ServiceProvider.GetRequiredService<IConsultaCuotasGdeba>();
                    var expedienteService = scope.ServiceProvider.GetRequiredService<IExpedienteService>();
                    var currentApplicationAccessor = scope.ServiceProvider.GetRequiredService<ICurrentApplicationAccessor>();
                    currentApplicationAccessor.Current = new CurrentApplication("worker", "Worker de descubrimiento de expedientes");
                    var cuota = (await cuotas.ConsultarCuotasAsync(fecha, stoppingToken)).Operaciones.FirstOrDefault(x => x.Servicio == _options.ServicioCuota && x.Operacion == _options.MetodoCuota);
                    var reserva = Math.Max(0, _options.CupoReservaDiaria);
                    if (cuota?.LimiteDiario is not int limite)
                    {
                        var motivo = cuota is null ? "No existe configuracion de cuota para la operacion." : "La operacion no tiene limite diario configurado.";
                        await expedienteService.RegistrarDescubrimientoProgramadoOmitidoAsync(new RegistrarDescubrimientoProgramadoOmitidoRequest(cuota?.LimiteDiario, cuota?.Total ?? 0, reserva, motivo), stoppingToken);
                        _logger.LogWarning("Descubrimiento de expedientes omitido. {Motivo} Servicio={Servicio}. Metodo={Metodo}.", motivo, _options.ServicioCuota, _options.MetodoCuota);
                    }
                    else
                    {
                        var presupuesto = Math.Max(0, limite - cuota.Total - reserva);
                        if (presupuesto > 0)
                        {
                            _logger.LogInformation("Descubrimiento programado con presupuesto de {Presupuesto} invocaciones. Limite={Limite}. Registradas={Registradas}. Reserva={Reserva}.", presupuesto, limite, cuota.Total, reserva);
                            await expedienteService.DescubrirExpedientesProgramadosAsync(new DescubrirExpedientesProgramadosRequest(presupuesto, _options.ConsultasVaciasParaPausa, _options.DiasPausaSinResultados, OrigenInvocacionGdeba.WorkerProgramado), stoppingToken);
                            _logger.LogInformation("Finalizo la corrida programada de descubrimiento para la fecha {Fecha}.", fecha);
                        }
                        else
                        {
                            const string motivo = "Cupo diario agotado o reservado.";
                            await expedienteService.RegistrarDescubrimientoProgramadoOmitidoAsync(new RegistrarDescubrimientoProgramadoOmitidoRequest(limite, cuota.Total, reserva, motivo), stoppingToken);
                            _logger.LogInformation("Descubrimiento de expedientes omitido por cupo diario agotado. Limite={Limite}. InvocacionesRegistradas={InvocacionesRegistradas}. Reserva={Reserva}.", limite, cuota.Total, reserva);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "La corrida de descubrimiento de expedientes finalizo con error.");
                }

                fechaEjecutada = fecha;
            }
            else if (!esperaInformada)
            {
                _logger.LogInformation("Worker de descubrimiento en espera. HoraLocal={HoraLocal}. Ventana={HoraInicio:00}:00-{HoraFin:00}:00.", ahora, _options.HoraInicioLocal, _options.HoraFinLocal);
                esperaInformada = true;
            }
            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes)), stoppingToken);
        }
    }

    private bool EstaDentroDeLaVentana(DateTimeOffset ahora)
    {
        var hora = ahora.Hour;
        return _options.HoraInicioLocal < _options.HoraFinLocal ? hora >= _options.HoraInicioLocal && hora < _options.HoraFinLocal : hora >= _options.HoraInicioLocal || hora < _options.HoraFinLocal;
    }
}
