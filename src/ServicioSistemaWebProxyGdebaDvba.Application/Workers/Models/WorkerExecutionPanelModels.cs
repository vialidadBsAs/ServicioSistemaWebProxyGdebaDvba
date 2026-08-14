using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

public enum EstadoCorridaAutomatica
{
    Proyectada = 1,
    EjecutadaHoy = 2,
    OmitidaHoy = 3,
    Pausada = 4
}

public sealed record ProyeccionCorridaAutomaticaDto(EstadoCorridaAutomatica Estado, DateTimeOffset? ProximaCorrida, string? OmitidaPor);

public sealed record OmisionCorridaProgramadaDto(ProcesoWorker Proceso, DateOnly FechaLocal, string OmitidaPor, DateTimeOffset FechaRegistro);

public sealed record ConsultaPanelEjecucionesWorkerResult(
    ProcesoWorker Proceso,
    ConfiguracionProgramadaWorkerDto Configuracion,
    ProyeccionCorridaAutomaticaDto CorridaAutomatica,
    IReadOnlyCollection<SolicitudEjecucionWorkerDto> OrdenesManualesVivas,
    IReadOnlyCollection<EjecucionWorkerDto> EjecucionesDelDia,
    IReadOnlyCollection<EjecucionWorkerDto> Historico);
