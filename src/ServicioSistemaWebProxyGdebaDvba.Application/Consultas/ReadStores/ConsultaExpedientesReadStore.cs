using Microsoft.EntityFrameworkCore;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.ReadStores;

public sealed class ConsultaExpedientesReadStore : IConsultaExpedientesReadStore
{
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<HistorialExpedienteCacheControl> _historialCacheControlRepository;
    private readonly IRepository<MovimientoExpediente> _movimientoRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataRepository;

    public ConsultaExpedientesReadStore(
        IRepository<Expediente> expedienteRepository,
        IRepository<HistorialExpedienteCacheControl> historialCacheControlRepository,
        IRepository<MovimientoExpediente> movimientoRepository,
        IRepository<TrataHabilitadaVialidad> trataRepository)
    {
        _expedienteRepository = expedienteRepository;
        _historialCacheControlRepository = historialCacheControlRepository;
        _movimientoRepository = movimientoRepository;
        _trataRepository = trataRepository;
    }

    public async Task<ConsultaExpedientesResult> ConsultarAsync(ConsultaExpedientesFiltro filtro, CancellationToken cancellationToken)
    {
        var query = _expedienteRepository.Query().Where(x => x.TrataId.HasValue && filtro.TrataIds.Contains(x.TrataId.Value));
        if (filtro.CodigosTrata.Count > 0) query = query.Where(x => filtro.CodigosTrata.Contains(x.Trata!.CodigoTrata));
        if (filtro.EstadosActuales.Count > 0) query = query.Where(x => x.EstadoActual != null && filtro.EstadosActuales.Contains(x.EstadoActual));
        if (filtro.EstadosDetalle.Count > 0)
        {
            query = query.Where(x =>
                (filtro.EstadosDetalle.Contains("Pendiente") && (x.HistorialCacheControl == null || !x.HistorialCacheControl.EstaCompleto)) ||
                (filtro.EstadosDetalle.Contains("Vencido") && x.HistorialCacheControl != null && x.HistorialCacheControl.EstaCompleto && (x.HistorialCacheControl.FechaVencimiento == null || x.HistorialCacheControl.FechaVencimiento <= filtro.FechaConsulta)) ||
                (filtro.EstadosDetalle.Contains("Disponible") && x.HistorialCacheControl != null && x.HistorialCacheControl.EstaCompleto && x.HistorialCacheControl.FechaVencimiento != null && x.HistorialCacheControl.FechaVencimiento > filtro.FechaConsulta));
        }
        var totalRegistros = await query.CountAsync(cancellationToken);
        var queryOrdenada = filtro.CampoOrden switch
        {
            "numeroGdebaCompleto" => filtro.OrdenDescendente ? query.OrderByDescending(x => x.GdebaNumeroCompleto) : query.OrderBy(x => x.GdebaNumeroCompleto),
            "codigoTrata" => filtro.OrdenDescendente ? query.OrderByDescending(x => x.Trata!.CodigoTrata) : query.OrderBy(x => x.Trata!.CodigoTrata),
            "descripcionTrata" => filtro.OrdenDescendente ? query.OrderByDescending(x => x.Trata!.DescripcionTrata) : query.OrderBy(x => x.Trata!.DescripcionTrata),
            "estadoActual" => filtro.OrdenDescendente ? query.OrderByDescending(x => x.EstadoActual) : query.OrderBy(x => x.EstadoActual),
            "estadoDetalle" => filtro.OrdenDescendente
                ? query.OrderByDescending(x => x.HistorialCacheControl == null || !x.HistorialCacheControl.EstaCompleto)
                : query.OrderBy(x => x.HistorialCacheControl == null || !x.HistorialCacheControl.EstaCompleto),
            _ => filtro.OrdenDescendente ? query.OrderByDescending(x => x.HistorialCacheControl != null && x.HistorialCacheControl.UltimoMovimientoDetectado != null).ThenByDescending(x => x.HistorialCacheControl!.UltimoMovimientoDetectado!.FechaOperacion) : query.OrderBy(x => x.HistorialCacheControl != null && x.HistorialCacheControl.UltimoMovimientoDetectado != null).ThenBy(x => x.HistorialCacheControl!.UltimoMovimientoDetectado!.FechaOperacion)
        };
        var expedientes = await queryOrdenada.ThenByDescending(x => x.GdebaNumeroCompleto)
            .Skip((filtro.Pagina - 1) * filtro.TamanioPagina)
            .Take(filtro.TamanioPagina)
            .SelectAsync(cancellationToken);

        var expedientesIds = expedientes.Select(x => x.Id).ToArray();
        var historiales = await _historialCacheControlRepository.Query()
            .Where(x => expedientesIds.Contains(x.ExpedienteId))
            .SelectAsync(cancellationToken);
        var historialesPorExpedienteId = historiales.ToDictionary(x => x.ExpedienteId);
        var ultimosMovimientosIds = historiales.Where(x => x.UltimoMovimientoDetectadoId.HasValue).Select(x => x.UltimoMovimientoDetectadoId!.Value).ToArray();
        var ultimosMovimientos = ultimosMovimientosIds.Length == 0
            ? Array.Empty<MovimientoExpediente>()
            : await _movimientoRepository.Query().Where(x => ultimosMovimientosIds.Contains(x.Id)).SelectAsync(cancellationToken);
        var ultimosMovimientosPorId = ultimosMovimientos.ToDictionary(x => x.Id);
        var tratas = await _trataRepository.Query().Where(x => filtro.TrataIds.Contains(x.Id)).SelectAsync(cancellationToken);
        var tratasPorId = tratas.ToDictionary(x => x.Id);
        var items = expedientes.Select(x => ConsultaExpedientesReadStore.MapearExpediente(
            x,
            tratasPorId,
            historialesPorExpedienteId.GetValueOrDefault(x.Id),
            historialesPorExpedienteId.GetValueOrDefault(x.Id)?.UltimoMovimientoDetectadoId is Guid ultimoMovimientoId
                ? ultimosMovimientosPorId.GetValueOrDefault(ultimoMovimientoId)
                : null,
            filtro.FechaConsulta)).ToArray();

        return new ConsultaExpedientesResult(totalRegistros, filtro.Pagina, filtro.TamanioPagina, items);
    }

    public async Task<IReadOnlyCollection<string>> ObtenerValoresFiltroAsync(IReadOnlyCollection<Guid> trataIds, string campo, DateTimeOffset fechaConsulta, CancellationToken cancellationToken)
    {
        var query = _expedienteRepository.Query().Where(x => x.TrataId.HasValue && trataIds.Contains(x.TrataId.Value));
        if (campo == "codigoTrata")
        {
            var tratas = await _trataRepository.Query().Where(x => trataIds.Contains(x.Id)).SelectAsync(cancellationToken);
            return tratas.Select(x => x.CodigoTrata).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }

        if (campo == "estadoActual")
        {
            var expedientes = await query.SelectAsync(cancellationToken);
            return expedientes.Where(x => !string.IsNullOrWhiteSpace(x.EstadoActual)).Select(x => x.EstadoActual!).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }

        if (campo == "estadoDetalle")
        {
            var expedientes = await query.SelectAsync(cancellationToken);
            var expedientesIds = expedientes.Select(x => x.Id).ToArray();
            var historiales = await _historialCacheControlRepository.Query().Where(x => expedientesIds.Contains(x.ExpedienteId)).SelectAsync(cancellationToken);
            var historialesPorExpedienteId = historiales.ToDictionary(x => x.ExpedienteId);
            return expedientesIds.Select(id => ConsultaExpedientesReadStore.ObtenerEstadoDetalle(historialesPorExpedienteId.GetValueOrDefault(id), fechaConsulta)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }

        return Array.Empty<string>();
    }

    private static ConsultaExpedienteDto MapearExpediente(
        Expediente expediente,
        IReadOnlyDictionary<Guid, TrataHabilitadaVialidad> tratasPorId,
        HistorialExpedienteCacheControl? historial,
        MovimientoExpediente? ultimoMovimiento,
        DateTimeOffset fechaConsulta)
    {
        if (!expediente.TrataId.HasValue || !tratasPorId.TryGetValue(expediente.TrataId.Value, out var trata))
        {
            throw new InvalidOperationException($"El expediente '{expediente.GdebaNumeroCompleto}' referencia una trata habilitada de Vialidad inexistente: '{expediente.TrataId}'.");
        }

        var estadoDetalle = ConsultaExpedientesReadStore.ObtenerEstadoDetalle(historial, fechaConsulta);

        return new ConsultaExpedienteDto(
            expediente.Id,
            expediente.GdebaNumeroCompleto,
            trata.CodigoTrata,
            trata.DescripcionTrata,
            expediente.EstadoActual,
            ultimoMovimiento?.FechaOperacion,
            estadoDetalle);
    }

    private static string ObtenerEstadoDetalle(HistorialExpedienteCacheControl? historial, DateTimeOffset fechaConsulta)
    {
        return historial is null || !historial.EstaCompleto
            ? "Pendiente"
            : historial.FechaVencimiento is null || historial.FechaVencimiento <= fechaConsulta ? "Vencido" : "Disponible";
    }
}
