using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly DocumentoMetadataEnrichmentWorkerOptions _options;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<DocumentoMetadataEnrichmentWorkerOptions> options,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Worker de enriquecimiento documental deshabilitado por configuracion.");
        }

        var intervalo = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
        if (_options.Enabled && _options.RunOnStartup)
        {
            await this.EjecutarCicloAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Worker en espera. Proxima iteracion de enriquecimiento documental: {time}",
                DateTimeOffset.Now.Add(intervalo));
            await Task.Delay(intervalo, stoppingToken);

            if (_options.Enabled)
            {
                await this.EjecutarCicloAsync(stoppingToken);
            }
        }
    }

    private async Task EjecutarCicloAsync(CancellationToken cancellationToken)
    {
        if (!this.EstaDentroDeLaVentanaNoPico())
        {
            _logger.LogInformation(
                "Enriquecimiento documental omitido porque la hora local actual esta fuera de la ventana no pico configurada.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var consultaCuotas = scope.ServiceProvider.GetRequiredService<IConsultaCuotasGdeba>();
        var enrichmentService = scope.ServiceProvider.GetRequiredService<IDocumentoMetadataEnrichmentService>();
        var cuotas = await consultaCuotas.ConsultarCuotasAsync(
            DateOnly.FromDateTime(DateTime.Now),
            cancellationToken);
        var cuota = cuotas.Operaciones.FirstOrDefault(x =>
            string.Equals(x.Servicio, _options.ServicioCuota, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Operacion, _options.MetodoCuota, StringComparison.OrdinalIgnoreCase));

        if (cuota is null)
        {
            _logger.LogWarning(
                "No se encontro configuracion de cuota para {Servicio}.{Metodo}. Se omite el enriquecimiento documental.",
                _options.ServicioCuota,
                _options.MetodoCuota);
            return;
        }

        var loteAutorizado = this.CalcularLoteAutorizado(cuota);
        if (loteAutorizado <= 0)
        {
            _logger.LogInformation(
                "Enriquecimiento documental omitido por cuota. Consumido hoy: {Consumido}. Limite operativo: {LimiteOperativo}. Reserva diaria: {Reserva}.",
                cuota.Total,
                this.CalcularLimiteOperativo(cuota),
                Math.Max(0, _options.CupoReservaDiaria));
            return;
        }

        var result = await enrichmentService.EnriquecerPendientesAsync(
            loteAutorizado,
            OrigenInvocacionGdeba.WorkerProgramado,
            cancellationToken);

        _logger.LogInformation(
            "Enriquecimiento documental finalizado. LoteAutorizado: {LoteAutorizado}. Procesados: {Procesados}. Enriquecidos: {Enriquecidos}. SinDatos: {SinDatos}. Errores: {Errores}.",
            loteAutorizado,
            result.Procesados,
            result.Enriquecidos,
            result.SinDatos,
            result.Errores);
    }

    private int CalcularLoteAutorizado(ConsumoCuotaOperacionGdebaDto cuota)
    {
        var limiteOperativo = this.CalcularLimiteOperativo(cuota);
        var remanente = limiteOperativo - cuota.Total;
        var disponible = remanente - Math.Max(0, _options.CupoReservaDiaria);
        return Math.Max(0, Math.Min(Math.Max(1, _options.BatchSize), disponible));
    }

    private int CalcularLimiteOperativo(ConsumoCuotaOperacionGdebaDto cuota)
    {
        var limiteConfigurado = cuota.LimiteDiario ?? _options.LimiteDiarioOperativo;
        return Math.Min(limiteConfigurado, Math.Max(1, _options.LimiteDiarioOperativo));
    }

    private bool EstaDentroDeLaVentanaNoPico()
    {
        var horaActual = TimeOnly.FromDateTime(DateTime.Now);
        var inicio = Worker.CrearHora(_options.VentanaInicioHoraLocal);
        var fin = Worker.CrearHora(_options.VentanaFinHoraLocal);

        if (inicio == fin)
        {
            return true;
        }

        return inicio < fin
            ? horaActual >= inicio && horaActual < fin
            : horaActual >= inicio || horaActual < fin;
    }

    private static TimeOnly CrearHora(int hora)
    {
        var horaNormalizada = ((hora % 24) + 24) % 24;
        return new TimeOnly(horaNormalizada, 0);
    }
}
