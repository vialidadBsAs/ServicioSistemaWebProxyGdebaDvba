using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

public sealed record SolicitarEjecucionManualWorkerRequest(ProcesoWorker Proceso, string? SolicitadaPor);

public sealed record SolicitudEjecucionWorkerDto(
    Guid Id,
    ProcesoWorker Proceso,
    EstadoSolicitudEjecucionWorker Estado,
    string SolicitadaPor,
    DateTimeOffset FechaSolicitud,
    DateTimeOffset? FechaInicio,
    DateTimeOffset? FechaFinalizacion,
    string? Mensaje,
    Guid? EjecucionWorkerId);

public sealed record EjecucionWorkerDto(
    Guid Id,
    ProcesoWorker Proceso,
    OrigenInvocacionGdeba Origen,
    EstadoEjecucionWorker Estado,
    Guid? SolicitudEjecucionWorkerId,
    DateTimeOffset FechaInicio,
    DateTimeOffset? FechaFinalizacion,
    string? Resumen,
    int? Procesados,
    int? Creados,
    int? Enriquecidos,
    int? SinDatos,
    int? Errores,
    SolicitudManualAsociadaEjecucionWorkerDto? SolicitudManual);

public sealed record SolicitudManualAsociadaEjecucionWorkerDto(Guid Id, string SolicitadaPor, DateTimeOffset FechaSolicitud);

public sealed record EjecucionWorkerIniciada(Guid EjecucionId, Guid? SolicitudId);

public sealed record ConsultaMonitoreoWorkersResult(
    IReadOnlyCollection<EjecucionWorkerDto> Ejecuciones,
    IReadOnlyCollection<SolicitudEjecucionWorkerDto> SolicitudesActivas);

public sealed record ConsultaDetalleEjecucionDescubrimientoResult(
    EjecucionWorkerDto Ejecucion,
    IReadOnlyCollection<ResultadoEjecucionDescubrimientoTrataEstadoDto> ResultadosPorTrataEstado);

public sealed record ResultadoEjecucionDescubrimientoTrataEstadoDto(
    Guid Id,
    string CodigoTrata,
    string? DescripcionTrata,
    string EstadoDestino,
    DateTimeOffset FechaResolucion,
    int RecibidosGdeba,
    int Habilitados,
    int Descartados,
    int Creados,
    int Actualizados,
    int SinCambios,
    IReadOnlyCollection<ExpedienteDescubiertoPorEjecucionDto> ExpedientesNuevos);

public sealed record ExpedienteDescubiertoPorEjecucionDto(Guid Id, string NumeroExpediente);
