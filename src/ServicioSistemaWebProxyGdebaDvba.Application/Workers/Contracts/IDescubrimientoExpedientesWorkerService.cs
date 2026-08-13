using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IDescubrimientoExpedientesWorkerService
{
    Task<DescubrirExpedientesProgramadosResult> EjecutarAsync(
        Guid ejecucionWorkerId,
        DescubrirExpedientesProgramadosRequest request,
        CancellationToken cancellationToken);
}
