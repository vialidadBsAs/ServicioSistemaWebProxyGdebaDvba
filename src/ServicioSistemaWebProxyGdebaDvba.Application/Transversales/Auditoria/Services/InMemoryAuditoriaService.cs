using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Services;

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
            "Auditoria solicitada={OperacionSolicitada} gdeba={OperacionGdeba} app={Aplicacion} recurso={Recurso} fuente={Fuente} exitoso={Exitoso} mensaje={Mensaje}",
            request.OperacionSolicitada,
            request.OperacionGdeba,
            request.AplicacionConsumidoraCodigo,
            request.Recurso,
            request.Fuente,
            request.Exitoso,
            request.Mensaje);

        return Task.CompletedTask;
    }
}
