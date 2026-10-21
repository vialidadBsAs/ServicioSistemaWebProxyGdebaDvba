using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.ReadStores;

public sealed class EstadisticasReadStore : IEstadisticasReadStore
{
    private readonly IRepository<EstadisticaExpedientesPorTrataReadModel>
        _estadisticasRepository;
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly ILogger<EstadisticasReadStore> _logger;

    public EstadisticasReadStore(
        IRepository<EstadisticaExpedientesPorTrataReadModel> estadisticasRepository,
        IRepository<Expediente> expedienteRepository,
        ILogger<EstadisticasReadStore> logger)
    {
        _estadisticasRepository = estadisticasRepository;
        _expedienteRepository = expedienteRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<ExpedienteCaratuladoDto>> ConsultarExpedientesCaratuladosAsync(EstadisticaExpedientesCaratuladosFiltro filtro, int maximo, CancellationToken cancellationToken)
    {
        IQuery<Expediente> query = _expedienteRepository.Query().Where(x => x.Trata != null && x.Trata.CodigoTrata == filtro.CodigoTrata);
        if (filtro.FechaDesde is DateTimeOffset desde) query = query.Where(x => x.FechaCaratulacion >= desde);
        if (filtro.FechaHastaExclusiva is DateTimeOffset hastaExclusiva) query = query.Where(x => x.FechaCaratulacion < hastaExclusiva);

        IEnumerable<Expediente> expedientes = await query
            .OrderByDescending(x => x.FechaCaratulacion)
            .Take(maximo)
            .SelectAsync(cancellationToken);

        return expedientes
            .Select(x => new ExpedienteCaratuladoDto(x.Id, x.GdebaNumeroCompleto, x.FechaCaratulacion, x.EstadoActual, x.Motivo ?? x.DescripcionAdicional, x.SectorDestino))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<EstadisticaExpedientesPorTrataDto>> ConsultarTotalesExpedientesPorTrataAsync(
        EstadisticaExpedientesPorTrataFiltro filtro,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CodigoTrata, DescripcionTrata, Estado, TotalExpedientes
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
                .GroupBy(x => new { x.CodigoTrata, x.DescripcionTrata })
                .OrderBy(x => x.Key.CodigoTrata)
                .Select(grupoTrata => new EstadisticaExpedientesPorTrataDto(
                    grupoTrata.Key.CodigoTrata,
                    grupoTrata.Key.DescripcionTrata,
                    grupoTrata.Sum(x => x.TotalExpedientes),
                    grupoTrata
                        .OrderBy(x => x.Estado ?? string.Empty)
                        .Select(x => new EstadisticaExpedientesPorTrataEstadoDto(x.Estado, x.TotalExpedientes))
                        .ToArray()))
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
