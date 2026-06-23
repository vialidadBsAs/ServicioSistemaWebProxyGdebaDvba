using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Contracts;

public interface IEstadisticasService
{
    Task<EstadisticaExpedientesPorTrataResult> ObtenerTotalesExpedientesPorTrataAsync(
        EstadisticaExpedientesPorTrataRequest request,
        CancellationToken cancellationToken);
}
