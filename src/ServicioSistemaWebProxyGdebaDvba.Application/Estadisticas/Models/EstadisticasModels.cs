namespace ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

public sealed record EstadisticaExpedientesPorTrataRequest(
    string? CodigoTrata = null,
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null,
    string? Estado = null);

public sealed record EstadisticaExpedientesPorTrataFiltro(
    string? CodigoTrata,
    DateTimeOffset? FechaDesde,
    DateTimeOffset? FechaHastaExclusiva,
    string? Estado);

public sealed record EstadisticaExpedientesPorTrataFiltrosAplicadosDto(
    string? CodigoTrata,
    DateOnly? FechaDesde,
    DateOnly? FechaHasta,
    string? Estado);

public sealed record EstadisticaExpedientesPorTrataResult(
    DateTimeOffset ResolvedAt,
    EstadisticaExpedientesPorTrataFiltrosAplicadosDto FiltrosAplicados,
    int TotalExpedientes,
    IReadOnlyCollection<EstadisticaExpedientesPorTrataDto> Tratas);

public sealed record EstadisticaExpedientesPorTrataDto(
    string CodigoTrata,
    string? DescripcionTrata,
    int TotalExpedientes,
    IReadOnlyCollection<EstadisticaExpedientesPorTrataEstadoDto> Estados);

public sealed record EstadisticaExpedientesPorTrataEstadoDto(
    string? Estado,
    int TotalExpedientes);

public sealed class EstadisticaExpedientesPorTrataReadModel
{
    public string CodigoTrata { get; set; } = string.Empty;

    public string? DescripcionTrata { get; set; }

    public string? Estado { get; set; }

    public int TotalExpedientes { get; set; }
}
