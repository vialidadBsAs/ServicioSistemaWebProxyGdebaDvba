using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

/// <summary>Configuracion vigente y actividad de un worker desde una fecha: base del latido que el Worker escribe en su log.</summary>
public sealed record ResumenActividadWorkerDto(
    ProcesoWorker Proceso,
    bool Habilitado,
    TimeOnly HoraInicioLocal,
    TimeOnly HoraFinLocal,
    int? IntervaloMinutos,
    IReadOnlyDictionary<EstadoEjecucionWorker, int> EjecucionesPorEstado,
    UltimaEjecucionWorkerDto? UltimaEjecucion);

public sealed record UltimaEjecucionWorkerDto(DateTimeOffset FechaInicio, EstadoEjecucionWorker Estado, OrigenInvocacionGdeba Origen, string? Resumen);
