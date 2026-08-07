using Microsoft.EntityFrameworkCore;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.ReadStores;

public sealed class ConsultaExpedientesReadStore : IConsultaExpedientesReadStore
{
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<ExpedienteDocumento> _expedienteDocumentoRepository;
    private readonly IRepository<HistorialDocumentoGdeba> _historialDocumentoRepository;
    private readonly IRepository<HistorialExpedienteCacheControl> _historialCacheControlRepository;
    private readonly IRepository<MovimientoExpediente> _movimientoRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataRepository;
    private readonly IRepository<TipoDocumentoGdeba> _tipoDocumentoRepository;

    public ConsultaExpedientesReadStore(
        IRepository<Expediente> expedienteRepository,
        IRepository<ExpedienteDocumento> expedienteDocumentoRepository,
        IRepository<HistorialDocumentoGdeba> historialDocumentoRepository,
        IRepository<HistorialExpedienteCacheControl> historialCacheControlRepository,
        IRepository<MovimientoExpediente> movimientoRepository,
        IRepository<TrataHabilitadaVialidad> trataRepository,
        IRepository<TipoDocumentoGdeba> tipoDocumentoRepository)
    {
        _expedienteRepository = expedienteRepository;
        _expedienteDocumentoRepository = expedienteDocumentoRepository;
        _historialDocumentoRepository = historialDocumentoRepository;
        _historialCacheControlRepository = historialCacheControlRepository;
        _movimientoRepository = movimientoRepository;
        _trataRepository = trataRepository;
        _tipoDocumentoRepository = tipoDocumentoRepository;
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

    public async Task<ConsultaDocumentosPorTrataResult> ConsultarDocumentosAsync(ConsultaDocumentosPorTrataFiltro filtro, CancellationToken cancellationToken)
    {
        var query = _expedienteDocumentoRepository.Query()
            .Where(x => x.Expediente.TrataId.HasValue && filtro.TrataIds.Contains(x.Expediente.TrataId.Value));
        var vinculosResumen = await query.Include(x => x.Documento).SelectAsync(cancellationToken);
        var documentosResumen = vinculosResumen
            .Select(x => new DocumentoResumenLocal(x.DocumentoId, x.ExpedienteId, x.Documento.TipoDocumentoCodigo, x.Documento.MetadataCompleta))
            .ToArray();
        var tiposPorCodigo = await this.CargarTiposPorCodigoAsync(documentosResumen.Select(x => x.CodigoTipoDocumento), cancellationToken);
        var resumenTipos = documentosResumen
            .Select(x => ConsultaExpedientesReadStore.MapearResumenTipo(x, tiposPorCodigo))
            .GroupBy(x => new { x.CodigoTipoDocumento, x.NombreTipoDocumento, x.FamiliaTipoDocumento })
            .Select(x => new ConsultaTipoDocumentoResumenDto(
                x.Key.CodigoTipoDocumento,
                x.Key.NombreTipoDocumento,
                x.Key.FamiliaTipoDocumento,
                x.Select(item => item.DocumentoId).Distinct().Count(),
                x.Select(item => item.ExpedienteId).Distinct().Count(),
                x.Where(item => item.MetadataCompleta).Select(item => item.DocumentoId).Distinct().Count()))
            .OrderByDescending(x => x.CantidadDocumentos)
            .ThenBy(x => x.NombreTipoDocumento ?? x.CodigoTipoDocumento ?? "Sin tipo documental")
            .ToArray();
        var tratas = await _trataRepository.Query().Where(x => filtro.TrataIds.Contains(x.Id)).SelectAsync(cancellationToken);
        var tratasPorId = tratas.ToDictionary(x => x.Id);
        if (string.IsNullOrWhiteSpace(filtro.CodigoTipoDocumento))
        {
            return new ConsultaDocumentosPorTrataResult(
                0,
                filtro.Pagina,
                filtro.TamanioPagina,
                documentosResumen.Select(x => x.DocumentoId).Distinct().Count(),
                documentosResumen.Select(x => x.ExpedienteId).Distinct().Count(),
                documentosResumen.Where(x => x.MetadataCompleta).Select(x => x.DocumentoId).Distinct().Count(),
                resumenTipos,
                Array.Empty<ConsultaDocumentoPorTrataDto>());
        }

        var queryDocumentos = query.Where(x => x.Documento.TipoDocumentoCodigo == filtro.CodigoTipoDocumento);
        var totalRegistros = await queryDocumentos.CountAsync(cancellationToken);
        var vinculos = await queryDocumentos
            .Include(x => x.Documento)
            .Include(x => x.Expediente)
            .OrderByDescending(x => x.FechaVinculacion ?? x.Documento.FechaCreacion)
            .ThenByDescending(x => x.Documento.ActuacionAnio)
            .ThenByDescending(x => x.Documento.ActuacionNumero)
            .Skip((filtro.Pagina - 1) * filtro.TamanioPagina)
            .Take(filtro.TamanioPagina)
            .SelectAsync(cancellationToken);
        var documentosIds = vinculos.Select(x => x.DocumentoId).Distinct().ToArray();
        var historialDocumentos = documentosIds.Length == 0
            ? Array.Empty<HistorialDocumentoGdeba>()
            : await _historialDocumentoRepository.Query().Where(x => documentosIds.Contains(x.DocumentoId)).SelectAsync(cancellationToken);
        var ultimaActividadPorDocumentoId = historialDocumentos
            .GroupBy(x => x.DocumentoId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(actividad => actividad.FechaFin ?? actividad.FechaInicio ?? DateTimeOffset.MinValue)
                    .ThenByDescending(actividad => actividad.IdGdeba)
                    .First());
        var items = vinculos.Select(x => ConsultaExpedientesReadStore.MapearDocumento(
            x,
            tratasPorId,
            tiposPorCodigo,
            ultimaActividadPorDocumentoId.GetValueOrDefault(x.DocumentoId))).ToArray();

        return new ConsultaDocumentosPorTrataResult(
            totalRegistros,
            filtro.Pagina,
            filtro.TamanioPagina,
            documentosResumen.Select(x => x.DocumentoId).Distinct().Count(),
            documentosResumen.Select(x => x.ExpedienteId).Distinct().Count(),
            documentosResumen.Where(x => x.MetadataCompleta).Select(x => x.DocumentoId).Distinct().Count(),
            resumenTipos,
            items);
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

    private async Task<IReadOnlyDictionary<string, TipoDocumentoGdeba>> CargarTiposPorCodigoAsync(IEnumerable<string?> codigosTipoDocumento, CancellationToken cancellationToken)
    {
        var codigos = codigosTipoDocumento
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (codigos.Length == 0) return new Dictionary<string, TipoDocumentoGdeba>(StringComparer.OrdinalIgnoreCase);

        var tipos = await _tipoDocumentoRepository.Query().Where(x => codigos.Contains(x.Codigo)).SelectAsync(cancellationToken);
        return tipos.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);
    }

    private static DocumentoResumenConTipo MapearResumenTipo(DocumentoResumenLocal documento, IReadOnlyDictionary<string, TipoDocumentoGdeba> tiposPorCodigo)
    {
        var tipo = BuscarTipoDocumento(documento.CodigoTipoDocumento, tiposPorCodigo);
        return new DocumentoResumenConTipo(
            documento.DocumentoId,
            documento.ExpedienteId,
            documento.CodigoTipoDocumento,
            tipo?.Nombre,
            tipo?.Familia,
            documento.MetadataCompleta);
    }

    private static ConsultaDocumentoPorTrataDto MapearDocumento(
        ExpedienteDocumento vinculo,
        IReadOnlyDictionary<Guid, TrataHabilitadaVialidad> tratasPorId,
        IReadOnlyDictionary<string, TipoDocumentoGdeba> tiposPorCodigo,
        HistorialDocumentoGdeba? ultimaActividad)
    {
        var expediente = vinculo.Expediente;
        if (!expediente.TrataId.HasValue || !tratasPorId.TryGetValue(expediente.TrataId.Value, out var trata))
        {
            throw new InvalidOperationException($"El expediente '{expediente.GdebaNumeroCompleto}' referencia una trata habilitada de Vialidad inexistente: '{expediente.TrataId}'.");
        }

        var documento = vinculo.Documento;
        var tipo = BuscarTipoDocumento(documento.TipoDocumentoCodigo, tiposPorCodigo);
        return new ConsultaDocumentoPorTrataDto(
            expediente.Id,
            expediente.GdebaNumeroCompleto,
            trata.CodigoTrata,
            trata.DescripcionTrata,
            documento.Id,
            documento.NumeroActuacionCompleto,
            documento.ActuacionTipoCodigo,
            documento.TipoDocumentoCodigo,
            tipo?.Nombre,
            tipo?.Familia,
            documento.Referencia,
            documento.FechaCreacion,
            documento.MetadataCompleta,
            documento.UrlArchivo,
            documento.PuedeVerDocumento,
            ultimaActividad?.Actividad,
            ultimaActividad?.FechaFin ?? ultimaActividad?.FechaInicio);
    }

    private static TipoDocumentoGdeba? BuscarTipoDocumento(string? codigoTipoDocumento, IReadOnlyDictionary<string, TipoDocumentoGdeba> tiposPorCodigo)
    {
        return string.IsNullOrWhiteSpace(codigoTipoDocumento) || !tiposPorCodigo.TryGetValue(codigoTipoDocumento.Trim(), out var tipo) ? null : tipo;
    }

    private static string ObtenerEstadoDetalle(HistorialExpedienteCacheControl? historial, DateTimeOffset fechaConsulta)
    {
        return historial is null || !historial.EstaCompleto
            ? "Pendiente"
            : historial.FechaVencimiento is null || historial.FechaVencimiento <= fechaConsulta ? "Vencido" : "Disponible";
    }

    private sealed record DocumentoResumenLocal(Guid DocumentoId, Guid ExpedienteId, string? CodigoTipoDocumento, bool MetadataCompleta);

    private sealed record DocumentoResumenConTipo(Guid DocumentoId, Guid ExpedienteId, string? CodigoTipoDocumento, string? NombreTipoDocumento, string? FamiliaTipoDocumento, bool MetadataCompleta);
}
