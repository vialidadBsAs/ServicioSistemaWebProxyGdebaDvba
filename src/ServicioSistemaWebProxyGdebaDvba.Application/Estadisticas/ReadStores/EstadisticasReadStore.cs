using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.ReadStores;

public sealed class EstadisticasReadStore : IEstadisticasReadStore
{
    private readonly IRepository<EstadisticaExpedientesPorTrataReadModel>
        _estadisticasRepository;
    private readonly ILogger<EstadisticasReadStore> _logger;

    public EstadisticasReadStore(
        IRepository<EstadisticaExpedientesPorTrataReadModel> estadisticasRepository,
        ILogger<EstadisticasReadStore> logger)
    {
        _estadisticasRepository = estadisticasRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<EstadisticaExpedientesPorTrataDto>> ConsultarTotalesExpedientesPorTrataAsync( EstadisticaExpedientesPorTrataFiltro filtro,
                                                                                                                        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CodigoTrata, DescripcionTrata, TotalExpedientes
            FROM dbo.fn_EstadisticaExpedientesPorTrata({0}, {1}, {2}, {3})
            """;

        try
        {
            var filas = await _estadisticasRepository
                .Query()
                .SelectSqlAsync(
                    sql,
                    EstadisticasReadStore.CrearParametros(filtro),
                    cancellationToken);

            return filas
                .Select(x => new EstadisticaExpedientesPorTrataDto(
                    x.CodigoTrata,
                    x.DescripcionTrata,
                    x.TotalExpedientes))
                .ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error tecnico al consultar totales de expedientes por trata.");

            throw new InvalidOperationException(
                "No se pudieron consultar los totales de expedientes por trata.",
                ex);
        }
    }

    private static object[] CrearParametros(EstadisticaExpedientesPorTrataFiltro filtro)
    {
        return new[]
        {
            EstadisticasReadStore.CrearParametro(filtro.CodigoTrata),
            EstadisticasReadStore.CrearParametro(filtro.FechaDesde),
            EstadisticasReadStore.CrearParametro(filtro.FechaHastaExclusiva),
            EstadisticasReadStore.CrearParametro(filtro.Estado),
        };
    }

    private static object CrearParametro(object? valor)
    {
        return valor ?? DBNull.Value;
    }
}
