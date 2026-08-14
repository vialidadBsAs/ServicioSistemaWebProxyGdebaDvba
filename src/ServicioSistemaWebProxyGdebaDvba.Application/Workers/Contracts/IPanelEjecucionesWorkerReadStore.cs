using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IPanelEjecucionesWorkerReadStore
{
    Task<ConsultaPanelEjecucionesWorkerResult> ConsultarAsync(ProcesoWorker proceso, int cantidadHistorico, CancellationToken cancellationToken);
}
