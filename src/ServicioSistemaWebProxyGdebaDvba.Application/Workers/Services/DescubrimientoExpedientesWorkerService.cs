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
/// Coordina el descubrimiento programado y confirma cada trata/estado como una unidad local atomica.
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
        IUnitOfWork unitOfWork)
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
    }

    public async Task<DescubrirExpedientesProgramadosResult> EjecutarAsync(
        Guid ejecucionWorkerId,
        DescubrirExpedientesProgramadosRequest request,
        CancellationToken cancellationToken)
    {
        var ejecucion = (await _ejecucionWorkerRepository.Query()
            .Include(x => x.ResultadosDescubrimientoTrataEstado)
            .Where(x => x.Id == ejecucionWorkerId)
            .Take(1)
            .SelectAsync(cancellationToken))
            .SingleOrDefault();
        if (ejecucion is null)
        {
            throw new InvalidOperationException("No existe la ejecucion de descubrimiento de expedientes indicada.");
        }

        var fecha = DateTimeOffset.Now;
        var fechaLocal = DateOnly.FromDateTime(fecha.LocalDateTime);
        var tratas = await this.CargarTratasAsync(cancellationToken);
        var configuracionesEstados = await _configuracionDescubrimientoEstadoRepository.Query()
            .Where(x => x.Habilitado)
            .OrderBy(x => x.Prioridad)
            .SelectAsync(cancellationToken);
        var estadosPorId = (await _estadoExpedienteGdebaRepository.Query()
                .Where(x => configuracionesEstados.Select(y => y.EstadoExpedienteGdebaId).Contains(x.Id))
                .SelectAsync(cancellationToken))
            .ToDictionary(x => x.Id);
        var procesos = await _procesoDescubrimientoRepository.Query().SelectAsync(cancellationToken);
        var procesosPorClave = procesos.ToDictionary(x => (x.CodigoTrata.Trim().ToUpperInvariant(), x.EstadoExpedienteGdebaId));
        var candidatos = new List<CandidatoDescubrimientoProgramado>();
        var omitidasPorConsultaDelDia = 0;
        var omitidasPorPausa = 0;

        foreach (var trata in tratas)
        {
            foreach (var configuracionEstado in configuracionesEstados)
            {
                if (!estadosPorId.TryGetValue(configuracionEstado.EstadoExpedienteGdebaId, out var estado))
                {
                    continue;
                }

                procesosPorClave.TryGetValue((trata.CodigoTrata, estado.Id), out var proceso);
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

        var seleccionados = candidatos
            .OrderBy(x => x.Proceso?.FechaUltimaConsulta is not null)
            .ThenBy(x => x.Proceso?.FechaUltimaConsulta)
            .ThenBy(x => x.Trata.PrioridadTema)
            .ThenBy(x => x.Trata.PrioridadTrata)
            .ThenBy(x => x.PrioridadEstado)
            .ThenBy(x => x.Trata.CodigoTrata)
            .Take(Math.Max(0, request.MaximoInvocaciones))
            .ToArray();

        var resultados = new List<IncorporarExpedientesPorTrataResult>();
        var resultadosPorTrataEstado = new List<ResultadoDescubrimientoProgramadoTrataEstado>();
        foreach (var candidato in seleccionados)
        {
            var resultado = await _incorporacionExpedientesPorTrataService.PrepararAsync(
                new IncorporarExpedientesPorTrataRequest(candidato.Trata.CodigoTrata, candidato.Estado.NombreGdeba, request.OrigenInvocacion),
                cancellationToken);
            var procesoEsNuevo = candidato.Proceso is null;
            var proceso = candidato.Proceso ?? new ProcesoDescubrimientoTrataEstadoExpediente(candidato.Trata.CodigoTrata, candidato.Estado.Id);
            proceso.RegistrarResultado(resultado.ResolvedAt, resultado.Habilitados > 0, request.ConsultasVaciasParaPausa, request.DiasPausaSinResultados);
            if (procesoEsNuevo)
            {
                _procesoDescubrimientoRepository.Insert(proceso);
            }
            else
            {
                _procesoDescubrimientoRepository.Update(proceso);
                _procesoDescubrimientoRepository.ApplyChanges(proceso);
            }

            ejecucion.RegistrarResultadoDescubrimiento(
                candidato.Trata.Id,
                candidato.Estado.Id,
                resultado.ResolvedAt,
                resultado.RecibidosGdeba,
                resultado.Habilitados,
                resultado.Descartados,
                resultado.Creados,
                resultado.Actualizados,
                resultado.SinCambios,
                resultado.ExpedientesNuevosIds);
            _ejecucionWorkerRepository.Update(ejecucion);
            _ejecucionWorkerRepository.ApplyChanges(ejecucion);

            // Una unica confirmacion persiste expediente, control operativo, auditoria y resultado de esta combinacion.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _ejecucionWorkerRepository.AcceptChanges(ejecucion);
            resultados.Add(resultado);
            resultadosPorTrataEstado.Add(new ResultadoDescubrimientoProgramadoTrataEstado(candidato.Trata.Id, candidato.Estado.Id, resultado));
        }

        return new DescubrirExpedientesProgramadosResult(
            resultados.Count,
            resultados.Sum(x => x.RecibidosGdeba),
            resultados.Sum(x => x.Habilitados),
            resultados.Sum(x => x.Descartados),
            resultados.Sum(x => x.Creados),
            resultados.Sum(x => x.Actualizados),
            resultados.Sum(x => x.SinCambios),
            omitidasPorConsultaDelDia,
            omitidasPorPausa,
            Math.Max(0, candidatos.Count - seleccionados.Length),
            resultadosPorTrataEstado);
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
        var tratasHabilitadasPorCodigo = (await _trataHabilitadaVialidadRepository.Query()
                .Where(x => codigosTrataConfigurados.Contains(x.CodigoTrata))
                .SelectAsync(cancellationToken))
            .ToDictionary(x => x.CodigoTrata.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
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
                tratasPorCodigo.Add(codigoTrata, new TrataDescubrimientoProgramado(asignacion.TrataHabilitadaVialidad.Id, codigoTrata, prioridadTema, prioridadTrata));
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
