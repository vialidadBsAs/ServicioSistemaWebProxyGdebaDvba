using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Auditoria;

public sealed class InMemoryAuditoriaService : IAuditoriaService
{
    private readonly ILogger<InMemoryAuditoriaService> _logger;

    public InMemoryAuditoriaService(ILogger<InMemoryAuditoriaService> logger)
    {
        _logger = logger;
    }

    public Task RegistrarAsync(RegistrarAuditoriaRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Auditoria {Operacion} app={Aplicacion} recurso={Recurso} fuente={Fuente} exitoso={Exitoso}",
            request.Operacion,
            request.AplicacionConsumidoraCodigo,
            request.Recurso,
            request.Fuente,
            request.Exitoso);

        return Task.CompletedTask;
    }
}
