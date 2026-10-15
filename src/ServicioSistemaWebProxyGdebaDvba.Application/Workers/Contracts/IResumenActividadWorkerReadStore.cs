using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IResumenActividadWorkerReadStore
{
    /// <summary>Por cada worker configurado: estado, agenda, ejecuciones por estado iniciadas desde <paramref name="desde"/> y la ultima ejecucion.</summary>
    Task<IReadOnlyCollection<ResumenActividadWorkerDto>> ConsultarAsync(DateTimeOffset desde, CancellationToken cancellationToken);
}
