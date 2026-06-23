using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;

public interface IEstadisticasReadStore
{
    Task<IReadOnlyCollection<EstadisticaExpedientesPorTrataDto>>
        ConsultarTotalesExpedientesPorTrataAsync(
        EstadisticaExpedientesPorTrataFiltro filtro,
        CancellationToken cancellationToken);
}
