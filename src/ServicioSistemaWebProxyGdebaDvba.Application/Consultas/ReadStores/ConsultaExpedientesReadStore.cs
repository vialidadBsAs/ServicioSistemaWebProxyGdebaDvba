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
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(filtro.TrataIds, cancellationToken);
        var query = _expedienteRepository.Query().Where(x => x.TrataId.HasValue);
        if (trataIdsConsulta.Length > 0) query = query.Where(x => trataIdsConsulta.Contains(x.TrataId!.Value));
        if (filtro.CodigosTrata.Count > 0) query = query.Where(x => filtro.CodigosTrata.Contains(x.Trata!.CodigoTrata));
        if (filtro.EstadosActuales.Count > 0) query = query.Where(x => x.EstadoActual != null && filtro.EstadosActuales.Contains(x.EstadoActual));
        if (filtro.EstadosDetalle.Count > 0)
        {
            query = query.Where(x =>
                (filtro.EstadosDetalle.Contains("Pendiente") && (x.HistorialCacheControl == null || !x.HistorialCacheControl.EstaCompleto)) ||
                (filtro.EstadosDetalle.Contains("Vencido") && x.HistorialCacheControl != null && x.HistorialCacheControl.EstaCompleto && (x.HistorialCacheControl.FechaVencimiento == null || x.HistorialCacheControl.FechaVencimiento <= filtro.FechaConsulta)) ||
                (filtro.EstadosDetalle.Contains("Disponible") && x.HistorialCacheControl != null && x.HistorialCacheControl.EstaCompleto && x.HistorialCacheControl.FechaVencimiento != null && x.HistorialCacheControl.FechaVencimiento > filtro.FechaConsulta));
        }

        if (filtro.NumerosExpediente.Count == 1)
        {
            string numeroExpedienteBuscado = filtro.NumerosExpediente.First();
            query = query.Where(x => x.GdebaNumeroCompleto.Contains(numeroExpedienteBuscado));
        }
        else if (filtro.NumerosExpediente.Count > 1)
        {
            query = query.Where(x => filtro.NumerosExpediente.Contains(x.GdebaNumeroCompleto));
        }

        if (filtro.FechaUltimoMovimientoDesde is DateTimeOffset fechaMovimientoDesde)
        {
            query = query.Where(x => x.HistorialCacheControl != null && x.HistorialCacheControl.UltimoMovimientoDetectado != null && x.HistorialCacheControl.UltimoMovimientoDetectado.FechaOperacion >= fechaMovimientoDesde);
        }

        if (filtro.FechaUltimoMovimientoHasta is DateTimeOffset fechaMovimientoHasta)
        {
            query = query.Where(x => x.HistorialCacheControl != null && x.HistorialCacheControl.UltimoMovimientoDetectado != null && x.HistorialCacheControl.UltimoMovimientoDetectado.FechaOperacion < fechaMovimientoHasta);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Caratula))
        {
            string caratulaBuscada = filtro.Caratula.Trim();
            query = query.Where(x => x.DescripcionTramite != null && x.DescripcionTramite.Contains(caratulaBuscada));
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
        Guid[] trataIdsPagina = expedientes.Where(x => x.TrataId.HasValue).Select(x => x.TrataId!.Value).Distinct().ToArray();
        var tratas = await _trataRepository.Query().Where(x => trataIdsPagina.Contains(x.Id)).SelectAsync(cancellationToken);
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
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(filtro.TrataIds, cancellationToken);
        var query = _expedienteDocumentoRepository.Query()
            .Where(x => x.Expediente.TrataId.HasValue && trataIdsConsulta.Contains(x.Expediente.TrataId.Value));
        var vinculosResumen = await query.Include(x => x.Documento).SelectAsync(cancellationToken);
        var documentosResumen = vinculosResumen
            .Select(x => new DocumentoResumenLocal(x.DocumentoId, x.ExpedienteId, x.Documento.ActuacionTipoCodigo, x.Documento.TipoDocumentoCodigo, x.Documento.MetadataCompleta))
            .ToArray();
        var tiposPorCodigo = await this.CargarTiposPorCodigoAsync(documentosResumen.Select(x => x.CodigoTipoDocumento), cancellationToken);
        var resumenTipos = documentosResumen
            .GroupBy(x => x.CodigoActuacion, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => new ConsultaTipoDocumentoResumenDto(
                grupo.Key,
                grupo.Select(item => item.DocumentoId).Distinct().Count(),
                grupo.Select(item => item.ExpedienteId).Distinct().Count(),
                grupo.Where(item => item.MetadataCompleta).Select(item => item.DocumentoId).Distinct().Count()))
            .OrderByDescending(x => x.CantidadDocumentos)
            .ThenBy(x => x.CodigoTipoDocumento)
            .ToArray();
        var tratas = await _trataRepository.Query().Where(x => trataIdsConsulta.Contains(x.Id)).SelectAsync(cancellationToken);
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
                0,
                0,
                resumenTipos,
                Array.Empty<ConsultaDocumentoPorTrataDto>());
        }

        var queryDocumentos = query
            .Where(x => x.Documento.ActuacionTipoCodigo == filtro.CodigoTipoDocumento)
            .Include(x => x.Documento)
            .Include(x => x.Expediente);
        if (filtro.NumerosExpediente.Count > 0) queryDocumentos = queryDocumentos.Where(x => filtro.NumerosExpediente.Contains(x.Expediente.GdebaNumeroCompleto));
        if (filtro.CodigosTrata.Count > 0) queryDocumentos = queryDocumentos.Where(x => x.Expediente.Trata != null && filtro.CodigosTrata.Contains(x.Expediente.Trata.CodigoTrata));
        if (filtro.NumerosActuacion.Count > 0) queryDocumentos = queryDocumentos.Where(x => filtro.NumerosActuacion.Contains(x.Documento.NumeroActuacionCompleto));
        if (filtro.Referencias.Count > 0) queryDocumentos = queryDocumentos.Where(x => x.Documento.Referencia != null && filtro.Referencias.Contains(x.Documento.Referencia));
        var vinculosFiltrados = await queryDocumentos.SelectAsync(cancellationToken);
        var documentosAgrupados = vinculosFiltrados.GroupBy(x => x.DocumentoId).Select(x => x.ToArray()).ToArray();
        var totalRegistros = documentosAgrupados.Length;
        var totalDocumentosFiltrados = documentosAgrupados.Length;
        var totalExpedientesFiltrados = vinculosFiltrados.Select(x => x.ExpedienteId).Distinct().Count();
        var documentosIds = documentosAgrupados.Select(x => x[0].DocumentoId).ToArray();
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
        var documentos = documentosAgrupados.Select(x => ConsultaExpedientesReadStore.MapearDocumento(
            x,
            tratasPorId,
            tiposPorCodigo,
            ultimaActividadPorDocumentoId.GetValueOrDefault(x[0].DocumentoId)));
        var documentosOrdenados = filtro.CampoOrden switch
        {
            "numeroExpediente" => filtro.OrdenDescendente ? documentos.OrderByDescending(x => x.Expedientes.First().Numero) : documentos.OrderBy(x => x.Expedientes.First().Numero),
            "codigoTrata" => filtro.OrdenDescendente ? documentos.OrderByDescending(x => x.Expedientes.First().CodigoTrata) : documentos.OrderBy(x => x.Expedientes.First().CodigoTrata),
            "numeroActuacionCompleto" => filtro.OrdenDescendente ? documentos.OrderByDescending(x => x.NumeroActuacionCompleto) : documentos.OrderBy(x => x.NumeroActuacionCompleto),
            "fechaCreacion" => filtro.OrdenDescendente ? documentos.OrderByDescending(x => x.FechaCreacion) : documentos.OrderBy(x => x.FechaCreacion),
            "ultimaActividad" => filtro.OrdenDescendente ? documentos.OrderByDescending(x => x.UltimaActividad) : documentos.OrderBy(x => x.UltimaActividad),
            "fechaUltimaActividad" => filtro.OrdenDescendente ? documentos.OrderByDescending(x => x.FechaUltimaActividad) : documentos.OrderBy(x => x.FechaUltimaActividad),
            "referencia" => filtro.OrdenDescendente ? documentos.OrderByDescending(x => x.Referencia) : documentos.OrderBy(x => x.Referencia),
            _ => documentos.OrderByDescending(x => x.FechaCreacion)
        };
        var items = documentosOrdenados
            .ThenByDescending(x => x.NumeroActuacionCompleto)
            .Skip((filtro.Pagina - 1) * filtro.TamanioPagina)
            .Take(filtro.TamanioPagina)
            .ToArray();

        return new ConsultaDocumentosPorTrataResult(
            totalRegistros,
            filtro.Pagina,
            filtro.TamanioPagina,
            documentosResumen.Select(x => x.DocumentoId).Distinct().Count(),
            documentosResumen.Select(x => x.ExpedienteId).Distinct().Count(),
            documentosResumen.Where(x => x.MetadataCompleta).Select(x => x.DocumentoId).Distinct().Count(),
            totalDocumentosFiltrados,
            totalExpedientesFiltrados,
            resumenTipos,
            items);
    }

    public async Task<IReadOnlyCollection<string>> ObtenerValoresFiltroAsync(IReadOnlyCollection<Guid> trataIds, string campo, DateTimeOffset fechaConsulta, CancellationToken cancellationToken)
    {
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(trataIds, cancellationToken);
        var query = _expedienteRepository.Query().Where(x => x.TrataId.HasValue && trataIdsConsulta.Contains(x.TrataId.Value));
        if (campo == "codigoTrata")
        {
            var tratas = await _trataRepository.Query().Where(x => trataIdsConsulta.Contains(x.Id)).SelectAsync(cancellationToken);
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

    public async Task<ConsultaCoberturaDetalleResult> ConsultarCoberturaDetalleAsync(IReadOnlyCollection<Guid> trataIds, CancellationToken cancellationToken)
    {
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(trataIds, cancellationToken);
        var query = _expedienteRepository.Query().Where(x => x.TrataId.HasValue);
        if (trataIdsConsulta.Length > 0) query = query.Where(x => trataIdsConsulta.Contains(x.TrataId!.Value));
        int detallados = await query.Where(x => x.HistorialCacheControl != null && x.HistorialCacheControl.FechaUltimaConsultaGdeba != null).CountAsync(cancellationToken);
        int sinDetallar = await query.Where(x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null).CountAsync(cancellationToken);
        return new ConsultaCoberturaDetalleResult(detallados, sinDetallar);
    }

    private async Task<Guid[]> ExpandirTrataIdsPorCodigoAsync(IReadOnlyCollection<Guid> trataIds, CancellationToken cancellationToken)
    {
        if (trataIds.Count == 0) return Array.Empty<Guid>();

        // La seleccion de tratas es conceptualmente por codigo: el mismo codigo puede tener filas por reparticion y cada expediente conserva la de su caratulacion.
        IEnumerable<TrataHabilitadaVialidad> tratasSeleccionadas = await _trataRepository.Query().Where(x => trataIds.Contains(x.Id)).SelectAsync(cancellationToken);
        string[] codigos = tratasSeleccionadas.Select(x => x.CodigoTrata).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        IEnumerable<TrataHabilitadaVialidad> tratasDelCodigo = await _trataRepository.Query().Where(x => codigos.Contains(x.CodigoTrata)).SelectAsync(cancellationToken);
        return tratasDelCodigo.Select(x => x.Id).Union(trataIds).ToArray();
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
            estadoDetalle,
            expediente.DescripcionTramite,
            expediente.FechaCaratulacion);
    }

    private async Task<IReadOnlyDictionary<string, TipoDocumentoGdeba>> CargarTiposPorCodigoAsync(IEnumerable<string?> codigosTipoDocumento, CancellationToken cancellationToken)
    {
        string[] codigos = codigosTipoDocumento
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (codigos.Length == 0) return new Dictionary<string, TipoDocumentoGdeba>(StringComparer.OrdinalIgnoreCase);

        IEnumerable<TipoDocumentoGdeba> tipos = await _tipoDocumentoRepository.Query()
            .Where(x => codigos.Contains(x.Codigo) || (x.CodigoTipoDocumentoGdeba != null && codigos.Contains(x.CodigoTipoDocumentoGdeba)))
            .SelectAsync(cancellationToken);
        Dictionary<string, TipoDocumentoGdeba> tiposPorCodigo = new Dictionary<string, TipoDocumentoGdeba>(StringComparer.OrdinalIgnoreCase);
        foreach (TipoDocumentoGdeba tipo in tipos)
        {
            tiposPorCodigo[tipo.Codigo] = tipo;
        }

        foreach (IGrouping<string, TipoDocumentoGdeba> grupoActuacion in tipos
            .Where(x => !string.IsNullOrWhiteSpace(x.CodigoTipoDocumentoGdeba))
            .GroupBy(x => x.CodigoTipoDocumentoGdeba!, StringComparer.OrdinalIgnoreCase))
        {
            if (grupoActuacion.Count() == 1 && !tiposPorCodigo.ContainsKey(grupoActuacion.Key))
            {
                tiposPorCodigo[grupoActuacion.Key] = grupoActuacion.Single();
            }
        }

        return tiposPorCodigo;
    }

    private static ConsultaDocumentoPorTrataDto MapearDocumento(
        IReadOnlyCollection<ExpedienteDocumento> vinculos,
        IReadOnlyDictionary<Guid, TrataHabilitadaVialidad> tratasPorId,
        IReadOnlyDictionary<string, TipoDocumentoGdeba> tiposPorCodigo,
        HistorialDocumentoGdeba? ultimaActividad)
    {
        var documento = vinculos.First().Documento;
        var expedientes = vinculos.Select(vinculo =>
        {
            var expediente = vinculo.Expediente;
            if (!expediente.TrataId.HasValue || !tratasPorId.TryGetValue(expediente.TrataId.Value, out var trata)) throw new InvalidOperationException($"El expediente '{expediente.GdebaNumeroCompleto}' referencia una trata habilitada de Vialidad inexistente: '{expediente.TrataId}'.");
            return new ConsultaDocumentoExpedienteDto(expediente.Id, expediente.GdebaNumeroCompleto, trata.CodigoTrata);
        }).OrderBy(x => x.Numero).ToArray();
        var tipo = BuscarTipoDocumento(documento.TipoDocumentoCodigo, tiposPorCodigo);
        return new ConsultaDocumentoPorTrataDto(
            expedientes,
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

    private sealed record DocumentoResumenLocal(Guid DocumentoId, Guid ExpedienteId, string CodigoActuacion, string? CodigoTipoDocumento, bool MetadataCompleta);
}
