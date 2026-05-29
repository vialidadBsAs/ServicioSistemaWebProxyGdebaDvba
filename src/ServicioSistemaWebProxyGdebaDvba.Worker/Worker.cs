namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker de sincronizacion en espera. Proxima iteracion: {time}", DateTimeOffset.Now.AddMinutes(15));
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
