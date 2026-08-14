using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.ReadStores;

public sealed class PanelEjecucionesWorkerReadStore : IPanelEjecucionesWorkerReadStore
{
    private readonly IRepository<ConfiguracionProgramadaWorker> _configuracionRepository;
    private readonly IRepository<SolicitudEjecucionWorker> _solicitudRepository;
    private readonly IRepository<EjecucionWorker> _ejecucionRepository;
    private readonly IRepository<OmisionCorridaProgramadaWorker> _omisionRepository;

    public PanelEjecucionesWorkerReadStore(
        IRepository<ConfiguracionProgramadaWorker> configuracionRepository,
        IRepository<SolicitudEjecucionWorker> solicitudRepository,
        IRepository<EjecucionWorker> ejecucionRepository,
        IRepository<OmisionCorridaProgramadaWorker> omisionRepository)
    {
        _configuracionRepository = configuracionRepository;
        _solicitudRepository = solicitudRepository;
        _ejecucionRepository = ejecucionRepository;
        _omisionRepository = omisionRepository;
    }

    public async Task<ConsultaPanelEjecucionesWorkerResult> ConsultarAsync(ProcesoWorker proceso, int cantidadHistorico, CancellationToken cancellationToken)
    {
        ConfiguracionProgramadaWorker? configuracion = await _configuracionRepository.Query().FirstOrDefaultAsync(x => x.Proceso == proceso, cancellationToken);
        if (configuracion is null)
        {
            throw new InvalidOperationException($"No existe configuración programada para el Worker '{proceso}'.");
        }

        DateTimeOffset ahora = DateTimeOffset.Now;
        DateOnly hoy = DateOnly.FromDateTime(ahora.LocalDateTime);
        DateTimeOffset inicioDelDia = new DateTimeOffset(hoy.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local));
        DateTimeOffset finDelDia = inicioDelDia.AddDays(1);

        IEnumerable<SolicitudEjecucionWorker> ordenesManualesVivas = await _solicitudRepository.Query()
            .Where(x => x.Proceso == proceso &&
                (x.Estado == EstadoSolicitudEjecucionWorker.PendienteDeInicio ||
                x.Estado == EstadoSolicitudEjecucionWorker.Programada ||
                x.Estado == EstadoSolicitudEjecucionWorker.Pendiente ||
                x.Estado == EstadoSolicitudEjecucionWorker.EnEjecucion))
            .OrderBy(x => x.FechaSolicitud)
            .SelectAsync(cancellationToken);

        EjecucionWorker[] historico = (await _ejecucionRepository.Query()
            .Where(x => x.Proceso == proceso)
            .OrderByDescending(x => x.FechaInicio)
            .Take(Math.Clamp(cantidadHistorico, 1, 200))
            .SelectAsync(cancellationToken))
            .ToArray();
        EjecucionWorker[] ejecucionesDelDia = historico.Where(x => x.FechaInicio >= inicioDelDia && x.FechaInicio < finDelDia).ToArray();

        Guid[] solicitudIds = historico.Where(x => x.SolicitudEjecucionWorkerId is not null).Select(x => x.SolicitudEjecucionWorkerId!.Value).Distinct().ToArray();
        Dictionary<Guid, SolicitudEjecucionWorker> solicitudesPorId = solicitudIds.Length == 0
            ? new Dictionary<Guid, SolicitudEjecucionWorker>()
            : (await _solicitudRepository.Query().Where(x => solicitudIds.Contains(x.Id)).SelectAsync(cancellationToken)).ToDictionary(x => x.Id);

        EjecucionWorker? ultimaCorridaAutomatica = (await _ejecucionRepository.Query()
            .Where(x => x.Proceso == proceso && x.Origen == OrigenInvocacionGdeba.WorkerProgramado)
            .OrderByDescending(x => x.FechaInicio)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        OmisionCorridaProgramadaWorker? omisionDelDia = await _omisionRepository.Query().FirstOrDefaultAsync(x => x.Proceso == proceso && x.FechaLocal == hoy, cancellationToken);

        ConfiguracionProgramadaWorkerDto configuracionDto = PanelEjecucionesWorkerReadStore.MapearConfiguracion(configuracion);
        return new ConsultaPanelEjecucionesWorkerResult(
            proceso,
            configuracionDto,
            PanelEjecucionesWorkerReadStore.ProyectarCorridaAutomatica(configuracionDto, ultimaCorridaAutomatica, omisionDelDia, ahora),
            ordenesManualesVivas.Select(PanelEjecucionesWorkerReadStore.MapearSolicitud).ToArray(),
            ejecucionesDelDia.Select(x => PanelEjecucionesWorkerReadStore.MapearEjecucion(x, solicitudesPorId)).ToArray(),
            historico.Select(x => PanelEjecucionesWorkerReadStore.MapearEjecucion(x, solicitudesPorId)).ToArray());
    }

    private static ProyeccionCorridaAutomaticaDto ProyectarCorridaAutomatica(ConfiguracionProgramadaWorkerDto configuracion, EjecucionWorker? ultimaCorrida, OmisionCorridaProgramadaWorker? omisionDelDia, DateTimeOffset ahora)
    {
        if (!configuracion.Habilitado)
        {
            return new ProyeccionCorridaAutomaticaDto(EstadoCorridaAutomatica.Pausada, null, null);
        }

        DateOnly hoy = DateOnly.FromDateTime(ahora.LocalDateTime);
        if (configuracion.Proceso == ProcesoWorker.DescubrimientoExpedientes)
        {
            DateTimeOffset proximaManana = PanelEjecucionesWorkerReadStore.EnFechaLocal(hoy.AddDays(1), configuracion.HoraInicioLocal);
            if (ultimaCorrida is not null && DateOnly.FromDateTime(ultimaCorrida.FechaInicio.LocalDateTime) == hoy)
            {
                return omisionDelDia is not null && ultimaCorrida.Estado == EstadoEjecucionWorker.Omitida
                    ? new ProyeccionCorridaAutomaticaDto(EstadoCorridaAutomatica.OmitidaHoy, proximaManana, omisionDelDia.OmitidaPor)
                    : new ProyeccionCorridaAutomaticaDto(EstadoCorridaAutomatica.EjecutadaHoy, proximaManana, null);
            }

            if (omisionDelDia is not null)
            {
                return new ProyeccionCorridaAutomaticaDto(EstadoCorridaAutomatica.OmitidaHoy, proximaManana, omisionDelDia.OmitidaPor);
            }

            return new ProyeccionCorridaAutomaticaDto(EstadoCorridaAutomatica.Proyectada, PanelEjecucionesWorkerReadStore.AjustarAVentana(ahora, configuracion.HoraInicioLocal, configuracion.HoraFinLocal), null);
        }

        TimeSpan intervalo = TimeSpan.FromMinutes(Math.Max(1, configuracion.IntervaloMinutos ?? 1));
        DateTimeOffset candidata = ultimaCorrida is not null && ultimaCorrida.FechaInicio.Add(intervalo) > ahora ? ultimaCorrida.FechaInicio.Add(intervalo) : ahora;
        return new ProyeccionCorridaAutomaticaDto(EstadoCorridaAutomatica.Proyectada, PanelEjecucionesWorkerReadStore.AjustarAVentana(candidata, configuracion.HoraInicioLocal, configuracion.HoraFinLocal), null);
    }

    private static DateTimeOffset AjustarAVentana(DateTimeOffset candidata, TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (horaInicio == horaFin)
        {
            return candidata;
        }

        TimeOnly hora = TimeOnly.FromDateTime(candidata.LocalDateTime);
        bool dentroDeVentana = horaInicio < horaFin ? hora >= horaInicio && hora < horaFin : hora >= horaInicio || hora < horaFin;
        if (dentroDeVentana)
        {
            return candidata;
        }

        DateOnly fecha = DateOnly.FromDateTime(candidata.LocalDateTime);
        return hora < horaInicio
            ? PanelEjecucionesWorkerReadStore.EnFechaLocal(fecha, horaInicio)
            : PanelEjecucionesWorkerReadStore.EnFechaLocal(fecha.AddDays(1), horaInicio);
    }

    private static DateTimeOffset EnFechaLocal(DateOnly fecha, TimeOnly hora)
    {
        return new DateTimeOffset(fecha.ToDateTime(hora, DateTimeKind.Local));
    }

    private static ConfiguracionProgramadaWorkerDto MapearConfiguracion(ConfiguracionProgramadaWorker configuracion)
    {
        return new ConfiguracionProgramadaWorkerDto(
            configuracion.Id,
            configuracion.Proceso,
            configuracion.Habilitado,
            configuracion.HoraInicioLocal,
            configuracion.HoraFinLocal,
            configuracion.CupoReservaDiaria,
            configuracion.IntervaloMinutos,
            configuracion.EjecutarAlIniciar,
            configuracion.TamanoLote,
            configuracion.ConsultasVaciasParaPausa,
            configuracion.DiasPausaSinResultados,
            configuracion.OmitirConsultasRealizadasEnElDia);
    }

    private static SolicitudEjecucionWorkerDto MapearSolicitud(SolicitudEjecucionWorker solicitud)
    {
        return new SolicitudEjecucionWorkerDto(
            solicitud.Id, solicitud.Proceso, solicitud.Estado, solicitud.SolicitadaPor,
            solicitud.FechaSolicitud, solicitud.FechaInicioProgramada, solicitud.FechaInicio, solicitud.FechaFinalizacion,
            solicitud.CanceladaPor, solicitud.FechaCancelacion, solicitud.Mensaje, solicitud.EjecucionWorkerId);
    }

    private static EjecucionWorkerDto MapearEjecucion(EjecucionWorker ejecucion, IReadOnlyDictionary<Guid, SolicitudEjecucionWorker> solicitudesPorId)
    {
        SolicitudManualAsociadaEjecucionWorkerDto? solicitudManual = ejecucion.SolicitudEjecucionWorkerId is Guid solicitudId && solicitudesPorId.TryGetValue(solicitudId, out SolicitudEjecucionWorker? solicitud)
            ? new SolicitudManualAsociadaEjecucionWorkerDto(solicitud.Id, solicitud.SolicitadaPor, solicitud.FechaSolicitud)
            : null;
        return new EjecucionWorkerDto(
            ejecucion.Id, ejecucion.Proceso, ejecucion.Origen, ejecucion.Estado,
            ejecucion.SolicitudEjecucionWorkerId, ejecucion.FechaInicio, ejecucion.FechaFinalizacion,
            ejecucion.Resumen, ejecucion.Procesados, ejecucion.Creados, ejecucion.Enriquecidos,
            ejecucion.SinDatos, ejecucion.Errores, solicitudManual);
    }
}
