using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

public sealed class WorkerExecutionService : IWorkerExecutionService
{
    private readonly ITrackableRepository<EjecucionWorker> _ejecucionWorkerRepository;
    private readonly IRepository<EjecucionWorkerDescubrimientoTrataEstado> _ejecucionWorkerDescubrimientoTrataEstadoRepository;
    private readonly IRepository<EjecucionWorkerExpedienteDescubierto> _ejecucionWorkerExpedienteDescubiertoRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataHabilitadaVialidadRepository;
    private readonly IRepository<EstadoExpedienteGdeba> _estadoExpedienteGdebaRepository;
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly ITrackableRepository<SolicitudEjecucionWorker> _solicitudEjecucionWorkerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkerExecutionService(
        ITrackableRepository<EjecucionWorker> ejecucionWorkerRepository,
        IRepository<EjecucionWorkerDescubrimientoTrataEstado> ejecucionWorkerDescubrimientoTrataEstadoRepository,
        IRepository<EjecucionWorkerExpedienteDescubierto> ejecucionWorkerExpedienteDescubiertoRepository,
        IRepository<TrataHabilitadaVialidad> trataHabilitadaVialidadRepository,
        IRepository<EstadoExpedienteGdeba> estadoExpedienteGdebaRepository,
        IRepository<Expediente> expedienteRepository,
        ITrackableRepository<SolicitudEjecucionWorker> solicitudEjecucionWorkerRepository,
        IUnitOfWork unitOfWork)
    {
        _ejecucionWorkerRepository = ejecucionWorkerRepository;
        _ejecucionWorkerDescubrimientoTrataEstadoRepository = ejecucionWorkerDescubrimientoTrataEstadoRepository;
        _ejecucionWorkerExpedienteDescubiertoRepository = ejecucionWorkerExpedienteDescubiertoRepository;
        _trataHabilitadaVialidadRepository = trataHabilitadaVialidadRepository;
        _estadoExpedienteGdebaRepository = estadoExpedienteGdebaRepository;
        _expedienteRepository = expedienteRepository;
        _solicitudEjecucionWorkerRepository = solicitudEjecucionWorkerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConsultaMonitoreoWorkersResult> ConsultarAsync(int cantidadEjecuciones, CancellationToken cancellationToken)
    {
        var ejecuciones = await _ejecucionWorkerRepository.Query()
            .OrderByDescending(x => x.FechaInicio)
            .Take(Math.Clamp(cantidadEjecuciones, 1, 200))
            .SelectAsync(cancellationToken);
        var solicitudesActivas = await _solicitudEjecucionWorkerRepository.Query()
            .Where(x => x.Estado == EstadoSolicitudEjecucionWorker.PendienteDeInicio ||
                x.Estado == EstadoSolicitudEjecucionWorker.Pendiente ||
                x.Estado == EstadoSolicitudEjecucionWorker.EnEjecucion)
            .OrderBy(x => x.FechaSolicitud)
            .SelectAsync(cancellationToken);
        var solicitudesManualesPorId = (await _solicitudEjecucionWorkerRepository.Query()
                .Where(x => ejecuciones.Select(y => y.SolicitudEjecucionWorkerId).Contains(x.Id))
                .SelectAsync(cancellationToken))
            .ToDictionary(x => x.Id);
        return new ConsultaMonitoreoWorkersResult(
            ejecuciones.Select(x => WorkerExecutionService.MapearEjecucion(
                x,
                x.SolicitudEjecucionWorkerId is Guid solicitudId && solicitudesManualesPorId.TryGetValue(solicitudId, out var solicitud)
                    ? solicitud
                    : null)).ToArray(),
            solicitudesActivas.Select(WorkerExecutionService.MapearSolicitud).ToArray());
    }

    public async Task<ConsultaDetalleEjecucionDescubrimientoResult> ConsultarDetalleDescubrimientoAsync(Guid ejecucionId, CancellationToken cancellationToken)
    {
        var ejecucion = (await _ejecucionWorkerRepository.Query()
            .Where(x => x.Id == ejecucionId && x.Proceso == ProcesoWorker.DescubrimientoExpedientes)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (ejecucion is null)
        {
            throw new InvalidOperationException("No existe la ejecucion de descubrimiento de expedientes solicitada.");
        }

        var resultadosPersistidos = await _ejecucionWorkerDescubrimientoTrataEstadoRepository.Query()
            .Where(x => x.EjecucionWorkerId == ejecucionId)
            .OrderBy(x => x.FechaResolucion)
            .SelectAsync(cancellationToken);
        var trataIds = resultadosPersistidos.Select(x => x.TrataHabilitadaVialidadId).Distinct().ToArray();
        var estadoIds = resultadosPersistidos.Select(x => x.EstadoExpedienteGdebaId).Distinct().ToArray();
        var expedientesDescubiertos = await _ejecucionWorkerExpedienteDescubiertoRepository.Query()
            .Where(x => resultadosPersistidos.Select(y => y.Id).Contains(x.EjecucionWorkerDescubrimientoTrataEstadoId))
            .SelectAsync(cancellationToken);
        var expedienteIds = expedientesDescubiertos.Select(x => x.ExpedienteId).Distinct().ToArray();
        var tratasPorId = (await _trataHabilitadaVialidadRepository.Query().Where(x => trataIds.Contains(x.Id)).SelectAsync(cancellationToken))
            .ToDictionary(x => x.Id);
        var estadosPorId = (await _estadoExpedienteGdebaRepository.Query().Where(x => estadoIds.Contains(x.Id)).SelectAsync(cancellationToken))
            .ToDictionary(x => x.Id);
        var expedientesPorId = (await _expedienteRepository.Query().Where(x => expedienteIds.Contains(x.Id)).SelectAsync(cancellationToken))
            .ToDictionary(x => x.Id);
        var expedientesPorResultado = expedientesDescubiertos
            .GroupBy(x => x.EjecucionWorkerDescubrimientoTrataEstadoId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ExpedienteId).ToArray());
        var resultados = resultadosPersistidos
            .Select(x => WorkerExecutionService.MapearResultadoDescubrimiento(x, tratasPorId, estadosPorId, expedientesPorResultado, expedientesPorId))
            .ToArray();
        var solicitudManual = ejecucion.SolicitudEjecucionWorkerId is Guid solicitudId
            ? (await _solicitudEjecucionWorkerRepository.Query().Where(x => x.Id == solicitudId).Take(1).SelectAsync(cancellationToken)).SingleOrDefault()
            : null;
        return new ConsultaDetalleEjecucionDescubrimientoResult(
            WorkerExecutionService.MapearEjecucion(ejecucion, solicitudManual), resultados);
    }

    public async Task<SolicitudEjecucionWorkerDto> SolicitarEjecucionManualAsync(SolicitarEjecucionManualWorkerRequest request, CancellationToken cancellationToken)
    {
        var solicitudExistente = (await _solicitudEjecucionWorkerRepository.Query()
            .Where(x => x.Proceso == request.Proceso &&
                (x.Estado == EstadoSolicitudEjecucionWorker.PendienteDeInicio ||
                x.Estado == EstadoSolicitudEjecucionWorker.Pendiente ||
                x.Estado == EstadoSolicitudEjecucionWorker.EnEjecucion))
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (solicitudExistente is not null)
        {
            return WorkerExecutionService.MapearSolicitud(solicitudExistente);
        }

        var solicitud = new SolicitudEjecucionWorker(request.Proceso, request.SolicitadaPor ?? "Administracion", DateTimeOffset.Now);
        _solicitudEjecucionWorkerRepository.Insert(solicitud);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WorkerExecutionService.MapearSolicitud(solicitud);
    }

    public async Task<SolicitudEjecucionWorkerDto> IniciarSolicitudManualAsync(Guid solicitudId, CancellationToken cancellationToken)
    {
        var solicitud = (await _solicitudEjecucionWorkerRepository.Query()
            .Where(x => x.Id == solicitudId)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (solicitud is null)
        {
            throw new InvalidOperationException("No existe la solicitud manual de Worker.");
        }

        solicitud.PrepararParaEjecucion();
        _solicitudEjecucionWorkerRepository.Update(solicitud);
        _solicitudEjecucionWorkerRepository.ApplyChanges(solicitud);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WorkerExecutionService.MapearSolicitud(solicitud);
    }

    public async Task<EjecucionWorkerIniciada> IniciarEjecucionProgramadaAsync(ProcesoWorker proceso, CancellationToken cancellationToken)
    {
        var ejecucion = new EjecucionWorker(proceso, OrigenInvocacionGdeba.WorkerProgramado, null, DateTimeOffset.Now);
        _ejecucionWorkerRepository.Insert(ejecucion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new EjecucionWorkerIniciada(ejecucion.Id, null);
    }

    public async Task<EjecucionWorkerIniciada?> TomarSolicitudManualAsync(ProcesoWorker proceso, CancellationToken cancellationToken)
    {
        var solicitud = (await _solicitudEjecucionWorkerRepository.Query()
            .Where(x => x.Proceso == proceso && x.Estado == EstadoSolicitudEjecucionWorker.Pendiente)
            .OrderBy(x => x.FechaSolicitud)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (solicitud is null)
        {
            return null;
        }

        var ejecucion = new EjecucionWorker(proceso, OrigenInvocacionGdeba.Administrativo, solicitud.Id, DateTimeOffset.Now);
        solicitud.Iniciar(ejecucion.Id, ejecucion.FechaInicio);
        _ejecucionWorkerRepository.Insert(ejecucion);
        _solicitudEjecucionWorkerRepository.Update(solicitud);
        _solicitudEjecucionWorkerRepository.ApplyChanges(solicitud);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new EjecucionWorkerIniciada(ejecucion.Id, solicitud.Id);
    }

    public async Task FinalizarEjecucionAsync(Guid ejecucionId, EstadoEjecucionWorker estado, string? resumen, int? procesados, int? enriquecidos, int? sinDatos, int? errores, CancellationToken cancellationToken)
    {
        var ejecucion = (await _ejecucionWorkerRepository.Query()
            .Where(x => x.Id == ejecucionId)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (ejecucion is null)
        {
            throw new InvalidOperationException("No existe la ejecucion de Worker a finalizar.");
        }

        ejecucion.Finalizar(estado, resumen, procesados, null, enriquecidos, sinDatos, errores, DateTimeOffset.Now);
        _ejecucionWorkerRepository.Update(ejecucion);
        _ejecucionWorkerRepository.ApplyChanges(ejecucion);
        if (ejecucion.SolicitudEjecucionWorkerId is Guid solicitudId)
        {
            var solicitud = (await _solicitudEjecucionWorkerRepository.Query()
                .Where(x => x.Id == solicitudId)
                .Take(1)
                .SelectAsync(cancellationToken))
                .SingleOrDefault();
            if (solicitud is not null)
            {
                solicitud.Finalizar(estado, resumen, ejecucion.FechaFinalizacion!.Value);
                _solicitudEjecucionWorkerRepository.Update(solicitud);
                _solicitudEjecucionWorkerRepository.ApplyChanges(solicitud);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task FinalizarEjecucionDescubrimientoAsync(
        Guid ejecucionId,
        EstadoEjecucionWorker estado,
        string? resumen,
        int? procesados,
        int? creados,
        CancellationToken cancellationToken)
    {
        var ejecucion = (await _ejecucionWorkerRepository.Query()
            .Where(x => x.Id == ejecucionId && x.Proceso == ProcesoWorker.DescubrimientoExpedientes)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (ejecucion is null)
        {
            throw new InvalidOperationException("No existe la ejecucion de descubrimiento de expedientes a finalizar.");
        }

        ejecucion.Finalizar(estado, resumen, procesados, creados, null, null, null, DateTimeOffset.Now);
        _ejecucionWorkerRepository.Update(ejecucion);
        _ejecucionWorkerRepository.ApplyChanges(ejecucion);
        if (ejecucion.SolicitudEjecucionWorkerId is Guid solicitudId)
        {
            var solicitud = (await _solicitudEjecucionWorkerRepository.Query()
                .Where(x => x.Id == solicitudId)
                .Take(1)
                .SelectAsync(cancellationToken))
                .SingleOrDefault();
            if (solicitud is not null)
            {
                solicitud.Finalizar(estado, resumen, ejecucion.FechaFinalizacion!.Value);
                _solicitudEjecucionWorkerRepository.Update(solicitud);
                _solicitudEjecucionWorkerRepository.ApplyChanges(solicitud);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static EjecucionWorkerDto MapearEjecucion(EjecucionWorker ejecucion, SolicitudEjecucionWorker? solicitudManual = null)
    {
        return new EjecucionWorkerDto(
            ejecucion.Id, ejecucion.Proceso, ejecucion.Origen, ejecucion.Estado,
            ejecucion.SolicitudEjecucionWorkerId, ejecucion.FechaInicio, ejecucion.FechaFinalizacion,
            ejecucion.Resumen, ejecucion.Procesados, ejecucion.Creados, ejecucion.Enriquecidos,
            ejecucion.SinDatos, ejecucion.Errores,
            solicitudManual is null
                ? null
                : new SolicitudManualAsociadaEjecucionWorkerDto(solicitudManual.Id, solicitudManual.SolicitadaPor, solicitudManual.FechaSolicitud));
    }

    private static SolicitudEjecucionWorkerDto MapearSolicitud(SolicitudEjecucionWorker solicitud)
    {
        return new SolicitudEjecucionWorkerDto(
            solicitud.Id, solicitud.Proceso, solicitud.Estado, solicitud.SolicitadaPor,
            solicitud.FechaSolicitud, solicitud.FechaInicio, solicitud.FechaFinalizacion,
            solicitud.Mensaje, solicitud.EjecucionWorkerId);
    }

    private static ResultadoEjecucionDescubrimientoTrataEstadoDto MapearResultadoDescubrimiento(
        EjecucionWorkerDescubrimientoTrataEstado resultado,
        IReadOnlyDictionary<Guid, TrataHabilitadaVialidad> tratasPorId,
        IReadOnlyDictionary<Guid, EstadoExpedienteGdeba> estadosPorId,
        IReadOnlyDictionary<Guid, Guid[]> expedientesPorResultado,
        IReadOnlyDictionary<Guid, Expediente> expedientesPorId)
    {
        if (!tratasPorId.TryGetValue(resultado.TrataHabilitadaVialidadId, out var trata) ||
            !estadosPorId.TryGetValue(resultado.EstadoExpedienteGdebaId, out var estado))
        {
            throw new InvalidOperationException("El resultado de descubrimiento referencia datos de dominio inexistentes.");
        }

        var expedientesNuevos = expedientesPorResultado.TryGetValue(resultado.Id, out var expedienteIds)
            ? expedienteIds.Where(expedientesPorId.ContainsKey).Select(x => expedientesPorId[x]).OrderBy(x => x.GdebaNumeroCompleto)
                .Select(x => new ExpedienteDescubiertoPorEjecucionDto(x.Id, x.GdebaNumeroCompleto)).ToArray()
            : Array.Empty<ExpedienteDescubiertoPorEjecucionDto>();
        return new ResultadoEjecucionDescubrimientoTrataEstadoDto(
            resultado.Id,
            trata.CodigoTrata,
            trata.DescripcionTrata,
            estado.NombreGdeba,
            resultado.FechaResolucion,
            resultado.RecibidosGdeba,
            resultado.Habilitados,
            resultado.Descartados,
            resultado.Creados,
            resultado.Actualizados,
            resultado.SinCambios,
            expedientesNuevos);
    }
}
