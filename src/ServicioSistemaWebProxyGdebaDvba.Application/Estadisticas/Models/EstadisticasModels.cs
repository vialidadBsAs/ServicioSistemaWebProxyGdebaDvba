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

// Detalle de una trata dentro de un rango de caratulacion: los expedientes concretos, del mas nuevo al mas viejo.
public sealed record EstadisticaExpedientesCaratuladosRequest(string CodigoTrata, DateOnly? FechaDesde = null, DateOnly? FechaHasta = null);

public sealed record EstadisticaExpedientesCaratuladosFiltro(string CodigoTrata, DateTimeOffset? FechaDesde, DateTimeOffset? FechaHastaExclusiva);

public sealed record EstadisticaExpedientesCaratuladosResult(
    DateTimeOffset ResolvedAt,
    string CodigoTrata,
    bool Limitado,
    IReadOnlyCollection<ExpedienteCaratuladoDto> Expedientes);

public sealed record ExpedienteCaratuladoDto(
    Guid Id,
    string NumeroGdebaCompleto,
    DateTimeOffset? FechaCaratulacion,
    string? EstadoActual,
    string? Caratula,
    string? SectorDestino);

// Ultima corrida de descubrimiento que incorporo expedientes: cuando fue, cuantos entraron y entre que fechas de caratulacion estan.
// Es el rango por defecto del bloque "Expedientes nuevos": la corrida es nocturna, asi que lo del dia nunca esta y lo util es lo ultimo incorporado.
public sealed record UltimaDeteccionExpedientesResult(DateTimeOffset FechaDeteccion, int ExpedientesNuevos, DateOnly? CaratulacionDesde, DateOnly? CaratulacionHasta);

public sealed class EstadisticaExpedientesPorTrataReadModel
{
    public string CodigoTrata { get; set; } = string.Empty;

    public string? DescripcionTrata { get; set; }

    public string? Estado { get; set; }

    public int TotalExpedientes { get; set; }
}
