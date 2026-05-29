namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;

public interface IConsultarExpedienteService
{
    Task<ConsultarExpedienteResult> ConsultarAsync(ConsultarExpedienteRequest request, CancellationToken cancellationToken);
}
