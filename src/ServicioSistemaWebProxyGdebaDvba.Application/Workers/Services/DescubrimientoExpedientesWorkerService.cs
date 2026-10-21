using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

/// <summary>
/// Coordina el descubrimiento programado: arma el plan de consultas y confirma cada trata/estado como una unidad local atomica, en su propio scope.
/// </summary>
public sealed class DescubrimientoExpedientesWorkerService : IDescubrimientoExpedientesWorkerService
{
    private readonly IIncorporacionExpedientesPorTrataService _incorporacionExpedientesPorTrataService;
    private readonly ITrackableRepository<EjecucionWorker> _ejecucionWorkerRepository;
    private readonly IRepository<ConfiguracionDescubrimientoEstadoExpediente> _configuracionDescubrimientoEstadoRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTemaExpediente> _configuracionDescubrimientoTemaRepository;
    private readonly IRepository<ConfiguracionDescubrimientoTrataExpediente> _configuracionDescubrimientoTrataRepository;
    private readonly IRepository<TemaExpedienteTrata> _temaExpedienteTrataRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataHabilitadaVialidadRepository;
    private readonly IRepository<EstadoExpedienteGdeba> _estadoExpedienteGdebaRepository;
    private readonly ITrackableRepository<ProcesoDescubrimientoTrataEstadoExpediente> _procesoDescubrimientoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DescubrimientoExpedientesWorkerService> _logger;

    public DescubrimientoExpedientesWorkerService(
        IIncorporacionExpedientesPorTrataService incorporacionExpedientesPorTrataService,
        ITrackableRepository<EjecucionWorker> ejecucionWorkerRepository,
        IRepository<ConfiguracionDescubrimientoEstadoExpediente> configuracionDescubrimientoEstadoRepository,
        IRepository<ConfiguracionDescubrimientoTemaExpediente> configuracionDescubrimientoTemaRepository,
        IRepository<ConfiguracionDescubrimientoTrataExpediente> configuracionDescubrimientoTrataRepository,
        IRepository<TemaExpedienteTrata> temaExpedienteTrataRepository,
        IRepository<TrataHabilitadaVialidad> trataHabilitadaVialidadRepository,
        IRepository<EstadoExpedienteGdeba> estadoExpedienteGdebaRepository,
        ITrackableRepository<ProcesoDescubrimientoTrataEstadoExpediente> procesoDescubrimientoRepository,
        IUnitOfWork unitOfWork,
        ILogger<DescubrimientoExpedientesWorkerService> logger)
    {
        _incorporacionExpedientesPorTrataService = incorporacionExpedientesPorTrataService;
        _ejecucionWorkerRepository = ejecucionWorkerRepository;
        _configuracionDescubrimientoEstadoRepository = configuracionDescubrimientoEstadoRepository;
        _configuracionDescubrimientoTemaRepository = configuracionDescubrimientoTemaRepository;
        _configuracionDescubrimientoTrataRepository = configuracionDescubrimientoTrataRepository;
        _temaExpedienteTrataRepository = temaExpedienteTrataRepository;
        _trataHabilitadaVialidadRepository = trataHabilitadaVialidadRepository;
        _estadoExpedienteGdebaRepository = estadoExpedienteGdebaRepository;
        _procesoDescubrimientoRepository = procesoDescubrimientoRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PlanDescubrimientoProgramado> PrepararAsync(Guid ejecucionWorkerId, DescubrirExpedientesProgramadosRequest request, CancellationToken cancellationToken)
    {
        EjecucionWorker ejecucion = await this.CargarEjecucionAsync(ejecucionWorkerId, cancellationToken);
        DateTimeOffset fecha = DateTimeOffset.Now;
        DateOnly fechaLocal = DateOnly.FromDateTime(fecha.LocalDateTime);
        IReadOnlyCollection<TrataDescubrimientoProgramado> tratas = await this.CargarTratasAsync(cancellationToken);
        IEnumerable<ConfiguracionDescubrimientoEstadoExpediente> configuracionesEstados = await _configuracionDescubrimientoEstadoRepository.Query()
            .Where(x => x.Habilitado)
            .OrderBy(x => x.Prioridad)
            .SelectAsync(cancellationToken);
        Dictionary<Guid, EstadoExpedienteGdeba> estadosPorId = (await _estadoExpedienteGdebaRepository.Query()
                .Where(x => configuracionesEstados.Select(y => y.EstadoExpedienteGdebaId).Contains(x.Id))
                .SelectAsync(cancellationToken))
            .ToDictionary(x => x.Id);
        IEnumerable<ProcesoDescubrimientoTrataEstadoExpediente> procesos = await _procesoDescubrimientoRepository.Query().SelectAsync(cancellationToken);
        Dictionary<(string, Guid), ProcesoDescubrimientoTrataEstadoExpediente> procesosPorClave = procesos.ToDictionary(x => (x.CodigoTrata.Trim().ToUpperInvariant(), x.EstadoExpedienteGdebaId));
        List<CandidatoDescubrimientoProgramado> candidatos = new();
        int omitidasPorConsultaDelDia = 0;
        int omitidasPorPausa = 0;

        foreach (TrataDescubrimientoProgramado trata in tratas)
        {
            foreach (ConfiguracionDescubrimientoEstadoExpediente configuracionEstado in configuracionesEstados)
            {
                if (!estadosPorId.TryGetValue(configuracionEstado.EstadoExpedienteGdebaId, out EstadoExpedienteGdeba? estado))
                {
                    continue;
                }

                procesosPorClave.TryGetValue((trata.CodigoTrata, estado.Id), out ProcesoDescubrimientoTrataEstadoExpediente? proceso);
                if (request.OmitirConsultasRealizadasEnElDia && proceso?.FechaUltimaConsulta is DateTimeOffset fechaUltimaConsulta && DateOnly.FromDateTime(fechaUltimaConsulta.LocalDateTime) == fechaLocal)
                {
                    omitidasPorConsultaDelDia++;
                    continue;
                }

                if (proceso?.OmitirHasta > fecha)
                {
                    omitidasPorPausa++;
                    continue;
                }

                candidatos.Add(new CandidatoDescubrimientoProgramado(trata, configuracionEstado.Prioridad, estado, proceso));
            }
        }

        // Primero la prioridad (tema, trata, estado) y recien despues la antiguedad: las combinaciones prioritarias se consultan todos los dias
        // y el resto rota de la consulta mas vieja a la mas nueva, con las nunca consultadas como las mas viejas. Antes la antiguedad iba primero,
        // criterio de carga inicial que, hecha la corrida total, ponia cualquier combinacion nueva por delante de las prioritarias.
        ConsultaDescubrimientoProgramada[] seleccionadas = candidatos
            .OrderBy(x => x.Trata.PrioridadTema)
            .ThenBy(x => x.Trata.PrioridadTrata)
            .ThenBy(x => x.PrioridadEstado)
            .ThenBy(x => x.Proceso?.FechaUltimaConsulta ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.Trata.CodigoTrata)
            .Take(Math.Max(0, request.MaximoInvocaciones))
            .Select(x => new ConsultaDescubrimientoProgramada(x.Trata.Id, x.Trata.CodigoTrata, x.Estado.Id, x.Estado.NombreGdeba))
            .ToArray();

        // Denominador del avance: las consultas trata-estado seleccionadas para esta corrida (ya recortadas por prioridad y cuota).
        ejecucion.SellarLote(seleccionadas.Length);
        _ejecucionWorkerRepository.Update(ejecucion);
        _ejecucionWorkerRepository.ApplyChanges(ejecucion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _ejecucionWorkerRepository.AcceptChanges(ejecucion);

        return new PlanDescubrimientoProgramado(seleccionadas, omitidasPorConsultaDelDia, omitidasPorPausa, Math.Max(0, candidatos.Count - seleccionadas.Length));
    }

    public async Task<ResultadoConsultaDescubrimiento> ConsultarAsync(Guid ejecucionWorkerId, ConsultaDescubrimientoProgramada consulta, DescubrirExpedientesProgramadosRequest request, CancellationToken cancellationToken)
    {
        // La solicitud de cancelacion la escribe la Api desde otro contexto: se lee de la base antes de invocar GDEBA.
        if (await this.CancelacionSolicitadaAsync(ejecucionWorkerId, cancellationToken) is not null)
        {
            return new ResultadoConsultaDescubrimiento(consulta, EstadoConsultaDescubrimiento.CanceladaAntesDeConsultar, null, true);
        }

        IncorporarExpedientesPorTrataResult incorporacion;
        try
        {
            incorporacion = await _incorporacionExpedientesPorTrataService.PrepararAsync(
                new IncorporarExpedientesPorTrataRequest(consulta.CodigoTrata, consulta.NombreEstado, request.OrigenInvocacion),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (GdebaOperationException exception) when (!GdebaNoAccesibleException.EsNoAccesible(exception))
        {
            // GDEBA proceso el pedido y lo rechazo (fault, respuesta que supera sus limites): falla de esta consulta, la corrida sigue.
            // Se registra como consulta fallida para que no vuelva a quedar primera por "nunca consultada" y la auditoria pendiente se confirma aca.
            _logger.LogWarning("GDEBA rechazo la consulta de descubrimiento {CodigoTrata}/{Estado}: {Motivo}", consulta.CodigoTrata, consulta.NombreEstado, exception.Message);
            await this.RegistrarConsultaFallidaAsync(consulta, request, cancellationToken);
            return new ResultadoConsultaDescubrimiento(consulta, EstadoConsultaDescubrimiento.RechazadaPorGdeba, null, false);
        }
        catch (Exception exception) when (GdebaNoAccesibleException.EsNoAccesible(exception))
        {
            // Rechazo del gateway o sin respuesta. Aislado puede ser propio de esta combinacion (en octubre de 2026 el gateway devolvio 403 para
            // una trata-estado cuya respuesta supera los limites de GDEBA); el host decide el corte cuando se repite en consultas seguidas.
            _logger.LogWarning("Rechazo de acceso en la consulta de descubrimiento {CodigoTrata}/{Estado}: {Motivo}", consulta.CodigoTrata, consulta.NombreEstado, GdebaNoAccesibleException.DescribirMotivo(exception));
            await this.RegistrarConsultaFallidaAsync(consulta, request, cancellationToken);
            return new ResultadoConsultaDescubrimiento(consulta, EstadoConsultaDescubrimiento.RechazoDeAcceso, null, false);
        }

        EjecucionWorker ejecucion = await this.CargarEjecucionAsync(ejecucionWorkerId, cancellationToken);
        (ProcesoDescubrimientoTrataEstadoExpediente proceso, bool procesoEsNuevo) = await this.CargarProcesoAsync(consulta, cancellationToken);
        proceso.RegistrarResultado(incorporacion.ResolvedAt, incorporacion.Habilitados > 0, request.ConsultasVaciasParaPausa, request.DiasPausaSinResultados);
        this.PersistirProceso(proceso, procesoEsNuevo);

        ejecucion.RegistrarResultadoDescubrimiento(
            consulta.TrataHabilitadaVialidadId,
            consulta.EstadoExpedienteGdebaId,
            incorporacion.ResolvedAt,
            incorporacion.RecibidosGdeba,
            incorporacion.Habilitados,
            incorporacion.Descartados,
            incorporacion.Creados,
            incorporacion.Actualizados,
            incorporacion.SinCambios,
            incorporacion.ExpedientesNuevosIds,
            incorporacion.ExpedientesActualizadosIds);

        // La ejecucion se guarda completa: si la Api pidio cancelar mientras corria la consulta, la marca se trae a la entidad antes de guardar
        // para que el guardado no la pise con null.
        DateTimeOffset? cancelacionSolicitada = await this.CancelacionSolicitadaAsync(ejecucionWorkerId, cancellationToken);
        if (cancelacionSolicitada is DateTimeOffset fechaCancelacion)
        {
            ejecucion.SolicitarCancelacion(fechaCancelacion);
        }

        _ejecucionWorkerRepository.Update(ejecucion);
        _ejecucionWorkerRepository.ApplyChanges(ejecucion);

        // Una unica confirmacion persiste expediente, control operativo, auditoria y resultado de esta combinacion.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _ejecucionWorkerRepository.AcceptChanges(ejecucion);

        // Segunda lectura, despues de guardar: si la cancelacion entro entre la lectura anterior y el guardado, este la piso en la base;
        // se restituye y la corrida se detiene igual. Cierra la carrera sin depender de cuanto tarde el guardado.
        if (cancelacionSolicitada is null && await this.CancelacionSolicitadaAsync(ejecucionWorkerId, cancellationToken) is DateTimeOffset cancelacionTardia)
        {
            ejecucion.SolicitarCancelacion(cancelacionTardia);
            _ejecucionWorkerRepository.Update(ejecucion);
            _ejecucionWorkerRepository.ApplyChanges(ejecucion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _ejecucionWorkerRepository.AcceptChanges(ejecucion);
            cancelacionSolicitada = cancelacionTardia;
        }

        return new ResultadoConsultaDescubrimiento(consulta, EstadoConsultaDescubrimiento.Realizada, incorporacion, cancelacionSolicitada is not null);
    }

    private async Task RegistrarConsultaFallidaAsync(ConsultaDescubrimientoProgramada consulta, DescubrirExpedientesProgramadosRequest request, CancellationToken cancellationToken)
    {
        (ProcesoDescubrimientoTrataEstadoExpediente proceso, bool procesoEsNuevo) = await this.CargarProcesoAsync(consulta, cancellationToken);
        proceso.RegistrarConsultaFallida(DateTimeOffset.Now, request.ConsultasVaciasParaPausa, request.DiasPausaSinResultados);
        this.PersistirProceso(proceso, procesoEsNuevo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // El control por trata-estado se crea en la primera consulta de la combinacion.
    private async Task<(ProcesoDescubrimientoTrataEstadoExpediente Proceso, bool EsNuevo)> CargarProcesoAsync(ConsultaDescubrimientoProgramada consulta, CancellationToken cancellationToken)
    {
        ProcesoDescubrimientoTrataEstadoExpediente? existente = (await _procesoDescubrimientoRepository.Query()
            .Where(x => x.CodigoTrata == consulta.CodigoTrata && x.EstadoExpedienteGdebaId == consulta.EstadoExpedienteGdebaId)
            .SelectAsync(cancellationToken))
            .FirstOrDefault();

        return existente is null
            ? (new ProcesoDescubrimientoTrataEstadoExpediente(consulta.CodigoTrata, consulta.EstadoExpedienteGdebaId), true)
            : (existente, false);
    }

    private void PersistirProceso(ProcesoDescubrimientoTrataEstadoExpediente proceso, bool esNuevo)
    {
        if (esNuevo)
        {
            _procesoDescubrimientoRepository.Insert(proceso);
            return;
        }

        _procesoDescubrimientoRepository.Update(proceso);
        _procesoDescubrimientoRepository.ApplyChanges(proceso);
    }

    private async Task<EjecucionWorker> CargarEjecucionAsync(Guid ejecucionWorkerId, CancellationToken cancellationToken)
    {
        EjecucionWorker? ejecucion = (await _ejecucionWorkerRepository.Query()
            .Include(x => x.ResultadosDescubrimientoTrataEstado)
            .Where(x => x.Id == ejecucionWorkerId)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();

        return ejecucion ?? throw new InvalidOperationException("No existe la ejecucion de descubrimiento de expedientes indicada.");
    }

    private Task<DateTimeOffset?> CancelacionSolicitadaAsync(Guid ejecucionWorkerId, CancellationToken cancellationToken)
    {
        return _ejecucionWorkerRepository.Queryable()
            .Where(x => x.Id == ejecucionWorkerId)
            .Select(x => x.FechaCancelacionSolicitada)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<TrataDescubrimientoProgramado>> CargarTratasAsync(CancellationToken cancellationToken)
    {
        var configuracionesTemas = await _configuracionDescubrimientoTemaRepository.Query()
            .Where(x => x.Habilitado)
            .OrderBy(x => x.Prioridad)
            .SelectAsync(cancellationToken);
        var configuracionesTratas = await _configuracionDescubrimientoTrataRepository.Query().SelectAsync(cancellationToken);
        var configuracionesTrataPorCodigo = configuracionesTratas.ToDictionary(x => x.CodigoTrata.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
        var prioridadesTemaPorId = configuracionesTemas.ToDictionary(x => x.TemaExpedienteId, x => x.Prioridad);
        var idsTemas = prioridadesTemaPorId.Keys.ToArray();
        var asignacionesTemas = idsTemas.Length == 0
            ? Array.Empty<TemaExpedienteTrata>()
            : (await _temaExpedienteTrataRepository.Query()
                .Include(x => x.TrataHabilitadaVialidad)
                .Where(x => idsTemas.Contains(x.TemaExpedienteId))
                .SelectAsync(cancellationToken)).ToArray();
        var codigosTrataConfigurados = configuracionesTratas
            .Select(x => x.CodigoTrata.Trim().ToUpperInvariant())
            .Concat(asignacionesTemas.Select(x => x.TrataHabilitadaVialidad.CodigoTrata.Trim().ToUpperInvariant()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        // El catalogo admite el mismo codigo de trata habilitado desde varias reparticiones; el descubrimiento trabaja por codigo con una fila representante.
        IEnumerable<TrataHabilitadaVialidad> tratasHabilitadasConfiguradas = await _trataHabilitadaVialidadRepository.Query()
            .Where(x => codigosTrataConfigurados.Contains(x.CodigoTrata))
            .SelectAsync(cancellationToken);
        Dictionary<string, TrataHabilitadaVialidad> tratasHabilitadasPorCodigo = tratasHabilitadasConfiguradas
            .GroupBy(x => x.CodigoTrata.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, TrataHabilitadaVialidad.ElegirRepresentantePorCodigo, StringComparer.OrdinalIgnoreCase);
        var tratasPorCodigo = new Dictionary<string, TrataDescubrimientoProgramado>(StringComparer.OrdinalIgnoreCase);

        foreach (var asignacion in asignacionesTemas)
        {
            var codigoTrata = asignacion.TrataHabilitadaVialidad.CodigoTrata.Trim().ToUpperInvariant();
            configuracionesTrataPorCodigo.TryGetValue(codigoTrata, out var configuracionTrata);
            if (configuracionTrata is { Habilitada: false })
            {
                continue;
            }

            var prioridadTema = prioridadesTemaPorId[asignacion.TemaExpedienteId];
            var prioridadTrata = configuracionTrata?.Prioridad ?? int.MaxValue;
            if (tratasPorCodigo.TryGetValue(codigoTrata, out var existente))
            {
                tratasPorCodigo[codigoTrata] = existente with
                {
                    PrioridadTema = Math.Min(existente.PrioridadTema, prioridadTema),
                    PrioridadTrata = Math.Min(existente.PrioridadTrata, prioridadTrata)
                };
            }
            else
            {
                Guid trataId = tratasHabilitadasPorCodigo.TryGetValue(codigoTrata, out TrataHabilitadaVialidad? representante)
                    ? representante.Id
                    : asignacion.TrataHabilitadaVialidad.Id;
                tratasPorCodigo.Add(codigoTrata, new TrataDescubrimientoProgramado(trataId, codigoTrata, prioridadTema, prioridadTrata));
            }
        }

        foreach (var configuracionTrata in configuracionesTratas.Where(x => x.Habilitada))
        {
            var codigoTrata = configuracionTrata.CodigoTrata.Trim().ToUpperInvariant();
            if (!tratasHabilitadasPorCodigo.TryGetValue(codigoTrata, out var trataHabilitada))
            {
                throw new InvalidOperationException($"La configuracion de descubrimiento referencia la trata '{codigoTrata}', que no esta habilitada localmente.");
            }

            if (tratasPorCodigo.TryGetValue(codigoTrata, out var existente))
            {
                tratasPorCodigo[codigoTrata] = existente with { PrioridadTrata = Math.Min(existente.PrioridadTrata, configuracionTrata.Prioridad) };
            }
            else
            {
                tratasPorCodigo.Add(codigoTrata, new TrataDescubrimientoProgramado(trataHabilitada.Id, codigoTrata, int.MaxValue, configuracionTrata.Prioridad));
            }
        }

        return tratasPorCodigo.Values.ToArray();
    }

    private sealed record TrataDescubrimientoProgramado(Guid Id, string CodigoTrata, int PrioridadTema, int PrioridadTrata);

    private sealed record CandidatoDescubrimientoProgramado(
        TrataDescubrimientoProgramado Trata,
        int PrioridadEstado,
        EstadoExpedienteGdeba Estado,
        ProcesoDescubrimientoTrataEstadoExpediente? Proceso);
}
