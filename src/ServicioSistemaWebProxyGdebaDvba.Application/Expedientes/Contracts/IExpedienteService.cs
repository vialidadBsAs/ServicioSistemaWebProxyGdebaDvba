using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

public interface IExpedienteService
{
    Task<ConsultarExpedienteResult> ConsultarAsync(
        ConsultarExpedienteRequest request,
        CancellationToken cancellationToken);

    Task<ConsultarExpedienteDetalladoResult> ConsultarDetalleAsync(
        ConsultarExpedienteDetalladoRequest request,
        CancellationToken cancellationToken);
}
