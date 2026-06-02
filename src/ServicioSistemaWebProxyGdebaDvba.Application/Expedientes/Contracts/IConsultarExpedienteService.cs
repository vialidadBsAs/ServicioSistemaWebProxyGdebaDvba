using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

public interface IConsultarExpedienteService
{
    Task<ConsultarExpedienteResult> ConsultarAsync(ConsultarExpedienteRequest request, CancellationToken cancellationToken);
}
