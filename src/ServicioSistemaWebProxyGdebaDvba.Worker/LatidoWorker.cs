using System.Globalization;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

/// <summary>
/// Latido del servicio: al arrancar y una vez por dia (Latido:HoraLocal, por defecto 08:00) escribe en el log el estado
/// y la agenda de cada worker y su actividad desde el latido anterior. Un dia sin latido indica que el servicio no estuvo activo.
/// </summary>
public sealed class LatidoWorker : BackgroundService
{
    private const int LargoMaximoResumen = 200;
    private static readonly TimeOnly HoraLatidoPorDefecto = new(8, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LatidoWorker> _logger;
    private readonly TimeOnly _horaLatido;

    public LatidoWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<LatidoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _horaLatido = TimeOnly.TryParse(configuration["Latido:HoraLocal"], CultureInfo.InvariantCulture, out TimeOnly horaConfigurada) ? horaConfigurada : LatidoWorker.HoraLatidoPorDefecto;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Al arrancar informa las ultimas 24 h: cubre lo ocurrido antes de un reinicio o de una caida.
        DateTimeOffset desde = DateTimeOffset.Now.AddDays(-1);
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset ahora = DateTimeOffset.Now;
            try
            {
                await this.EscribirLatidoAsync(desde, stoppingToken);
                desde = ahora;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "No se pudo escribir el latido del Worker; se reintenta en el proximo.");
            }

            await Task.Delay(this.CalcularEsperaHastaProximoLatido(DateTimeOffset.Now), stoppingToken);
        }
    }

    private async Task EscribirLatidoAsync(DateTimeOffset desde, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IResumenActividadWorkerReadStore resumenActividadWorkerReadStore = scope.ServiceProvider.GetRequiredService<IResumenActividadWorkerReadStore>();
        IReadOnlyCollection<ResumenActividadWorkerDto> resumenes = await resumenActividadWorkerReadStore.ConsultarAsync(desde, cancellationToken);
        foreach (ResumenActividadWorkerDto resumen in resumenes)
        {
            _logger.LogInformation(
                "Latido del Worker. {Proceso}: {Estado} ({Agenda}). Desde {Desde:dd/MM HH:mm}: {Actividad}. Ultima ejecucion: {UltimaEjecucion}.",
                resumen.Proceso.ToString(), resumen.Habilitado ? "habilitado" : "deshabilitado", LatidoWorker.DescribirAgenda(resumen), desde,
                LatidoWorker.DescribirActividad(resumen.EjecucionesPorEstado), LatidoWorker.DescribirUltimaEjecucion(resumen.UltimaEjecucion));
        }
    }

    private TimeSpan CalcularEsperaHastaProximoLatido(DateTimeOffset ahora)
    {
        DateTime proximo = DateTime.Today.Add(_horaLatido.ToTimeSpan());
        if (proximo <= ahora.LocalDateTime)
        {
            proximo = proximo.AddDays(1);
        }

        return proximo - ahora.LocalDateTime;
    }

    private static string DescribirAgenda(ResumenActividadWorkerDto resumen)
    {
        string ventana = resumen.HoraInicioLocal == resumen.HoraFinLocal ? "todo el dia" : $"ventana {resumen.HoraInicioLocal:HH\\:mm}-{resumen.HoraFinLocal:HH\\:mm}";
        return resumen.IntervaloMinutos is int intervalo ? $"cada {intervalo} min, {ventana}" : ventana;
    }

    private static string DescribirActividad(IReadOnlyDictionary<EstadoEjecucionWorker, int> ejecucionesPorEstado)
    {
        if (ejecucionesPorEstado.Count == 0)
        {
            return "sin ejecuciones";
        }

        return string.Join(", ", ejecucionesPorEstado.OrderBy(x => x.Key).Select(x => $"{x.Value} {x.Key}"));
    }

    private static string DescribirUltimaEjecucion(UltimaEjecucionWorkerDto? ultimaEjecucion)
    {
        if (ultimaEjecucion is null)
        {
            return "ninguna";
        }

        // El mensaje del latido cierra con punto: se quita el del resumen para no duplicarlo.
        string resumenCompleto = (ultimaEjecucion.Resumen ?? string.Empty).TrimEnd('.', ' ');
        string resumen = resumenCompleto.Length > LatidoWorker.LargoMaximoResumen ? $"{resumenCompleto[..LatidoWorker.LargoMaximoResumen]}..." : resumenCompleto;
        return $"{ultimaEjecucion.FechaInicio.LocalDateTime:dd/MM HH:mm} {ultimaEjecucion.Origen} {ultimaEjecucion.Estado}: {resumen}";
    }
}
