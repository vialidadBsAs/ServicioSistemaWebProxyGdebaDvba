using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IWorkerExecutionService
{
    Task<ConsultaMonitoreoWorkersResult> ConsultarAsync(int cantidadEjecuciones, CancellationToken cancellationToken);

    Task<ConsultaDetalleEjecucionDescubrimientoResult> ConsultarDetalleDescubrimientoAsync(Guid ejecucionId, CancellationToken cancellationToken);

    Task<SolicitudEjecucionWorkerDto> SolicitarEjecucionManualAsync(SolicitarEjecucionManualWorkerRequest request, CancellationToken cancellationToken);

    Task<SolicitudEjecucionWorkerDto> IniciarSolicitudManualAsync(Guid solicitudId, CancellationToken cancellationToken);

    Task<EjecucionWorkerIniciada> IniciarEjecucionProgramadaAsync(ProcesoWorker proceso, CancellationToken cancellationToken);

    Task<EjecucionWorkerIniciada?> TomarSolicitudManualAsync(ProcesoWorker proceso, CancellationToken cancellationToken);

    Task FinalizarEjecucionAsync(Guid ejecucionId, EstadoEjecucionWorker estado, string? resumen, int? procesados, int? enriquecidos, int? sinDatos, int? errores, CancellationToken cancellationToken);

    Task FinalizarEjecucionDescubrimientoAsync(Guid ejecucionId, EstadoEjecucionWorker estado, string? resumen, int? procesados, int? creados, IReadOnlyCollection<ResultadoDescubrimientoProgramadoTrataEstado> resultadosPorTrataEstado, CancellationToken cancellationToken);
}
