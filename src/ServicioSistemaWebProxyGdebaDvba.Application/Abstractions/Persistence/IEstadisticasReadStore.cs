using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;

public interface IEstadisticasReadStore
{
    Task<IReadOnlyCollection<EstadisticaExpedientesPorTrataDto>>
        ConsultarTotalesExpedientesPorTrataAsync(
        EstadisticaExpedientesPorTrataFiltro filtro,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExpedienteCaratuladoDto>> ConsultarExpedientesCaratuladosAsync(EstadisticaExpedientesCaratuladosFiltro filtro, int maximo, CancellationToken cancellationToken);

    Task<UltimaDeteccionExpedientesResult?> ConsultarUltimaDeteccionAsync(CancellationToken cancellationToken);
}
