using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Auditoria;

public sealed class InMemoryAuditoriaService : IAuditoriaService
{
    private readonly ILogger<InMemoryAuditoriaService> _logger;

    public InMemoryAuditoriaService(ILogger<InMemoryAuditoriaService> logger)
    {
        _logger = logger;
    }

    public Task RegistrarAsync(RegistroAuditoria registro, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Auditoria {Operacion} app={Aplicacion} recurso={Recurso} fuente={Fuente} exitoso={Exitoso}",
            registro.Operacion,
            registro.Aplicacion,
            registro.Recurso,
            registro.Fuente,
            registro.Exitoso);

        return Task.CompletedTask;
    }
}
