using System.Linq.Expressions;
using System.Reflection;
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
        if (filtro.CodigosTrata.Count > 0) query = query.Where(ConsultaExpedientesReadStore.ContieneAlguno<Expediente>(filtro.CodigosTrata, x => x.Trata!.CodigoTrata));
        if (filtro.EstadosActuales.Count > 0) query = query.Where(ConsultaExpedientesReadStore.ContieneAlguno<Expediente>(filtro.EstadosActuales, x => x.EstadoActual));
        if (filtro.EstadosDetalle.Count > 0)
        {
            // El texto tipeado se resuelve localmente contra los tres estados posibles: la SQL recibe solo los booleanos.
            bool buscaPendiente = filtro.EstadosDetalle.Any(valor => "Pendiente".Contains(valor, StringComparison.OrdinalIgnoreCase));
            bool buscaVencido = filtro.EstadosDetalle.Any(valor => "Vencido".Contains(valor, StringComparison.OrdinalIgnoreCase));
            bool buscaDisponible = filtro.EstadosDetalle.Any(valor => "Disponible".Contains(valor, StringComparison.OrdinalIgnoreCase));
            query = query.Where(x =>
                (buscaPendiente && (x.HistorialCacheControl == null || !x.HistorialCacheControl.EstaCompleto)) ||
                (buscaVencido && x.HistorialCacheControl != null && x.HistorialCacheControl.EstaCompleto && (x.HistorialCacheControl.FechaVencimiento == null || x.HistorialCacheControl.FechaVencimiento <= filtro.FechaConsulta)) ||
                (buscaDisponible && x.HistorialCacheControl != null && x.HistorialCacheControl.EstaCompleto && x.HistorialCacheControl.FechaVencimiento != null && x.HistorialCacheControl.FechaVencimiento > filtro.FechaConsulta));
        }

        if (filtro.NumerosExpediente.Count > 0) query = query.Where(ConsultaExpedientesReadStore.ContieneAlguno<Expediente>(filtro.NumerosExpediente, x => x.GdebaNumeroCompleto));

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
            query = query.Where(x =>
                (x.Motivo != null && x.Motivo.Contains(caratulaBuscada)) ||
                (x.DescripcionAdicional != null && x.DescripcionAdicional.Contains(caratulaBuscada)));
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

        // Con miles de documentos por tema, materializar los vinculos completos para contar u ordenar en memoria era el cuello de botella:
        // los conteos se resuelven en SQL, el orden usa una proyeccion liviana y solo la pagina visible carga entidades completas.
        IQueryable<ExpedienteDocumento> vinculosTema = _expedienteDocumentoRepository.Queryable()
            .Where(x => x.Expediente!.TrataId.HasValue && trataIdsConsulta.Contains(x.Expediente!.TrataId!.Value));

        // El resumen del tema (agrupado + totales) solo se calcula cuando la pantalla lo necesita: la busqueda por referencia
        // nunca lo muestra, y la vista por tipo lo pide una unica vez (la paginacion posterior lo conserva del lado cliente).
        bool buscaPorReferencia = !string.IsNullOrWhiteSpace(filtro.ReferenciaContiene) || filtro.SoloSinReferencia;
        ConsultaTipoDocumentoResumenDto[] resumenTipos = Array.Empty<ConsultaTipoDocumentoResumenDto>();
        int totalDocumentos = 0;
        int totalDocumentosConMetadata = 0;
        int totalExpedientes = 0;
        int totalDocumentosConReferencia = 0;
        if (!buscaPorReferencia && filtro.IncluirResumen)
        {
            resumenTipos = (await vinculosTema
                .GroupBy(x => x.Documento!.ActuacionTipoCodigo)
                .Select(grupo => new
                {
                    CodigoTipoDocumento = grupo.Key,
                    CantidadDocumentos = grupo.Select(x => x.DocumentoId).Distinct().Count(),
                    CantidadExpedientes = grupo.Select(x => x.ExpedienteId).Distinct().Count(),
                    CantidadDocumentosConMetadata = grupo.Where(x => x.Documento!.MetadataCompleta).Select(x => x.DocumentoId).Distinct().Count()
                })
                .ToArrayAsync(cancellationToken))
                .Select(x => new ConsultaTipoDocumentoResumenDto(x.CodigoTipoDocumento, x.CantidadDocumentos, x.CantidadExpedientes, x.CantidadDocumentosConMetadata))
                .OrderByDescending(x => x.CantidadDocumentos)
                .ThenBy(x => x.CodigoTipoDocumento)
                .ToArray();
            // El tipo particiona a los documentos, por lo que los totales de documentos salen del propio resumen; los expedientes se repiten entre tipos.
            totalDocumentos = resumenTipos.Sum(x => x.CantidadDocumentos);
            totalDocumentosConMetadata = resumenTipos.Sum(x => x.CantidadDocumentosConMetadata);
            totalExpedientes = await vinculosTema.Select(x => x.ExpedienteId).Distinct().CountAsync(cancellationToken);
            // Cobertura declarada de la busqueda por referencia: solo los documentos llegados por historial o enriquecimiento la tienen.
            totalDocumentosConReferencia = await vinculosTema
                .Where(x => x.Documento!.Referencia != null && x.Documento!.Referencia != string.Empty)
                .Select(x => x.DocumentoId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(filtro.CodigoTipoDocumento) && !buscaPorReferencia)
        {
            return new ConsultaDocumentosPorTrataResult(
                0,
                filtro.Pagina,
                filtro.TamanioPagina,
                totalDocumentos,
                totalExpedientes,
                totalDocumentosConMetadata,
                totalDocumentosConReferencia,
                0,
                0,
                resumenTipos,
                Array.Empty<ConsultaDocumentoPorTrataDto>());
        }

        IQueryable<ExpedienteDocumento> vinculosFiltrados = vinculosTema;
        if (!string.IsNullOrWhiteSpace(filtro.CodigoTipoDocumento)) vinculosFiltrados = vinculosFiltrados.Where(x => x.Documento!.ActuacionTipoCodigo == filtro.CodigoTipoDocumento);
        if (filtro.SoloSinReferencia) vinculosFiltrados = vinculosFiltrados.Where(x => x.Documento!.Referencia == null || x.Documento!.Referencia == string.Empty);
        else if (buscaPorReferencia) vinculosFiltrados = vinculosFiltrados.Where(x => x.Documento!.Referencia != null && x.Documento!.Referencia.Contains(filtro.ReferenciaContiene!));
        if (filtro.TiposDocumento.Count > 0)
        {
            // El texto tipeado matchea el codigo o el nombre del catalogo (la celda muestra "codigo · nombre"); el nombre se resuelve localmente porque no es navegable desde el documento.
            IEnumerable<TipoDocumentoGdeba> tiposCatalogo = await _tipoDocumentoRepository.Query().SelectAsync(cancellationToken);
            string[] codigosPorCatalogo = tiposCatalogo
                .Where(tipo => filtro.TiposDocumento.Any(valor => tipo.Codigo.Contains(valor, StringComparison.OrdinalIgnoreCase) || tipo.Nombre.Contains(valor, StringComparison.OrdinalIgnoreCase)))
                .Select(tipo => tipo.Codigo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            vinculosFiltrados = codigosPorCatalogo.Length > 0
                ? vinculosFiltrados.Where(x => x.Documento!.TipoDocumentoCodigo != null && codigosPorCatalogo.Contains(x.Documento!.TipoDocumentoCodigo))
                : vinculosFiltrados.Where(ConsultaExpedientesReadStore.ContieneAlguno<ExpedienteDocumento>(filtro.TiposDocumento, x => x.Documento!.TipoDocumentoCodigo));
        }

        if (filtro.FechaCreacionDesde is DateTimeOffset fechaCreacionDesde) vinculosFiltrados = vinculosFiltrados.Where(x => x.Documento!.FechaCreacion >= fechaCreacionDesde);
        if (filtro.FechaCreacionHastaExclusiva is DateTimeOffset fechaCreacionHasta) vinculosFiltrados = vinculosFiltrados.Where(x => x.Documento!.FechaCreacion < fechaCreacionHasta);
        if (filtro.NumerosExpediente.Count > 0) vinculosFiltrados = vinculosFiltrados.Where(ConsultaExpedientesReadStore.ContieneAlguno<ExpedienteDocumento>(filtro.NumerosExpediente, x => x.Expediente!.GdebaNumeroCompleto));
        if (filtro.CodigosTrata.Count > 0) vinculosFiltrados = vinculosFiltrados.Where(ConsultaExpedientesReadStore.ContieneAlguno<ExpedienteDocumento>(filtro.CodigosTrata, x => x.Expediente!.Trata!.CodigoTrata));
        if (filtro.NumerosActuacion.Count > 0) vinculosFiltrados = vinculosFiltrados.Where(ConsultaExpedientesReadStore.ContieneAlguno<ExpedienteDocumento>(filtro.NumerosActuacion, x => x.Documento!.NumeroActuacionCompleto));
        if (filtro.Referencias.Count > 0) vinculosFiltrados = vinculosFiltrados.Where(ConsultaExpedientesReadStore.ContieneAlguno<ExpedienteDocumento>(filtro.Referencias, x => x.Documento!.Referencia));

        int totalRegistros;
        int totalExpedientesFiltrados;
        Guid[] documentosIdsPagina;
        bool ordenaPorActividad = filtro.CampoOrden is "ultimaActividad" or "fechaUltimaActividad";
        Dictionary<Guid, HistorialDocumentoGdeba> ultimaActividadPorDocumentoId = new Dictionary<Guid, HistorialDocumentoGdeba>();
        bool ordenaPorColumnaDelDocumento = filtro.CampoOrden is not ("numeroExpediente" or "codigoTrata" or "ultimaActividad" or "fechaUltimaActividad");
        if (ordenaPorColumnaDelDocumento)
        {
            // Camino rapido (los ordenes habituales, columnas propias del documento): total, orden y pagina se resuelven integramente en SQL.
            // La referencia (texto libre potencialmente largo) solo participa del DISTINCT cuando el orden la necesita.
            bool ordenaPorReferencia = filtro.CampoOrden == "referencia";
            var clavesDeOrden = vinculosFiltrados
                .Select(x => new
                {
                    x.DocumentoId,
                    x.Documento!.FechaCreacion,
                    x.Documento!.NumeroActuacionCompleto,
                    Referencia = ordenaPorReferencia ? x.Documento!.Referencia : null
                })
                .Distinct();
            totalRegistros = await clavesDeOrden.CountAsync(cancellationToken);
            totalExpedientesFiltrados = await vinculosFiltrados.Select(x => x.ExpedienteId).Distinct().CountAsync(cancellationToken);
            var clavesOrdenadas = filtro.CampoOrden switch
            {
                "numeroActuacionCompleto" => filtro.OrdenDescendente ? clavesDeOrden.OrderByDescending(x => x.NumeroActuacionCompleto) : clavesDeOrden.OrderBy(x => x.NumeroActuacionCompleto),
                "referencia" => (filtro.OrdenDescendente ? clavesDeOrden.OrderByDescending(x => x.Referencia) : clavesDeOrden.OrderBy(x => x.Referencia)).ThenByDescending(x => x.NumeroActuacionCompleto),
                _ => (filtro.OrdenDescendente ? clavesDeOrden.OrderByDescending(x => x.FechaCreacion) : clavesDeOrden.OrderBy(x => x.FechaCreacion)).ThenByDescending(x => x.NumeroActuacionCompleto)
            };
            documentosIdsPagina = await clavesOrdenadas
                .Skip((filtro.Pagina - 1) * filtro.TamanioPagina)
                .Take(filtro.TamanioPagina)
                .Select(x => x.DocumentoId)
                .ToArrayAsync(cancellationToken);
        }
        else
        {
            // Ordenes por expediente o por actividad del historial: proyeccion minima y orden en memoria, sin arrastrar las entidades.
            FilaOrdenDocumento[] filasFiltradas = await vinculosFiltrados
                .Select(x => new FilaOrdenDocumento(
                    x.DocumentoId,
                    x.ExpedienteId,
                    x.Expediente!.GdebaNumeroCompleto,
                    x.Expediente!.Trata != null ? x.Expediente!.Trata!.CodigoTrata : null,
                    x.Documento!.NumeroActuacionCompleto,
                    x.Documento!.FechaCreacion,
                    null))
                .ToArrayAsync(cancellationToken);
            FilaOrdenDocumento[][] documentosAgrupados = filasFiltradas.GroupBy(x => x.DocumentoId).Select(x => x.OrderBy(fila => fila.NumeroExpediente).ToArray()).ToArray();
            totalRegistros = documentosAgrupados.Length;
            totalExpedientesFiltrados = filasFiltradas.Select(x => x.ExpedienteId).Distinct().Count();
            if (ordenaPorActividad)
            {
                ultimaActividadPorDocumentoId = await this.CargarUltimaActividadAsync(documentosAgrupados.Select(x => x[0].DocumentoId).ToArray(), cancellationToken);
            }

            IOrderedEnumerable<FilaOrdenDocumento[]> gruposOrdenados = filtro.CampoOrden switch
            {
                "codigoTrata" => ConsultaExpedientesReadStore.Ordenar(documentosAgrupados, x => x[0].CodigoTrata, filtro.OrdenDescendente),
                "ultimaActividad" => ConsultaExpedientesReadStore.Ordenar(documentosAgrupados, x => ultimaActividadPorDocumentoId.GetValueOrDefault(x[0].DocumentoId)?.Actividad, filtro.OrdenDescendente),
                "fechaUltimaActividad" => ConsultaExpedientesReadStore.Ordenar(documentosAgrupados, x => ultimaActividadPorDocumentoId.GetValueOrDefault(x[0].DocumentoId) is HistorialDocumentoGdeba actividad ? actividad.FechaFin ?? actividad.FechaInicio : null, filtro.OrdenDescendente),
                _ => ConsultaExpedientesReadStore.Ordenar(documentosAgrupados, x => x[0].NumeroExpediente, filtro.OrdenDescendente)
            };
            documentosIdsPagina = gruposOrdenados
                .ThenByDescending(x => x[0].NumeroActuacionCompleto)
                .Skip((filtro.Pagina - 1) * filtro.TamanioPagina)
                .Take(filtro.TamanioPagina)
                .Select(x => x[0].DocumentoId)
                .ToArray();
        }

        // Solo la pagina visible materializa entidades completas (documento, expedientes y tipos) para el mapeo final.
        ExpedienteDocumento[] vinculosPagina = documentosIdsPagina.Length == 0
            ? Array.Empty<ExpedienteDocumento>()
            : await vinculosFiltrados
                .Where(x => documentosIdsPagina.Contains(x.DocumentoId))
                .Include(x => x.Documento)
                .Include(x => x.Expediente)
                .ToArrayAsync(cancellationToken);
        Dictionary<Guid, ExpedienteDocumento[]> vinculosPaginaPorDocumentoId = vinculosPagina
            .GroupBy(x => x.DocumentoId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        if (!ordenaPorActividad)
        {
            ultimaActividadPorDocumentoId = await this.CargarUltimaActividadAsync(documentosIdsPagina, cancellationToken);
        }

        var tratas = await _trataRepository.Query().Where(x => trataIdsConsulta.Contains(x.Id)).SelectAsync(cancellationToken);
        var tratasPorId = tratas.ToDictionary(x => x.Id);
        var tiposPorCodigo = await this.CargarTiposPorCodigoAsync(vinculosPagina.Select(x => x.Documento.TipoDocumentoCodigo), cancellationToken);
        ConsultaDocumentoPorTrataDto[] items = documentosIdsPagina
            .Where(vinculosPaginaPorDocumentoId.ContainsKey)
            .Select(documentoId => ConsultaExpedientesReadStore.MapearDocumento(
                vinculosPaginaPorDocumentoId[documentoId],
                tratasPorId,
                tiposPorCodigo,
                ultimaActividadPorDocumentoId.GetValueOrDefault(documentoId)))
            .ToArray();

        return new ConsultaDocumentosPorTrataResult(
            totalRegistros,
            filtro.Pagina,
            filtro.TamanioPagina,
            totalDocumentos,
            totalExpedientes,
            totalDocumentosConMetadata,
            totalDocumentosConReferencia,
            totalRegistros,
            totalExpedientesFiltrados,
            resumenTipos,
            items);
    }

    private async Task<Dictionary<Guid, HistorialDocumentoGdeba>> CargarUltimaActividadAsync(Guid[] documentosIds, CancellationToken cancellationToken)
    {
        if (documentosIds.Length == 0)
        {
            return new Dictionary<Guid, HistorialDocumentoGdeba>();
        }

        IEnumerable<HistorialDocumentoGdeba> historialDocumentos = await _historialDocumentoRepository.Query()
            .Where(x => documentosIds.Contains(x.DocumentoId))
            .SelectAsync(cancellationToken);
        return historialDocumentos
            .GroupBy(x => x.DocumentoId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(actividad => actividad.FechaFin ?? actividad.FechaInicio ?? DateTimeOffset.MinValue)
                    .ThenByDescending(actividad => actividad.IdGdeba)
                    .First());
    }

    private static IOrderedEnumerable<FilaOrdenDocumento[]> Ordenar<TClave>(
        IEnumerable<FilaOrdenDocumento[]> grupos,
        Func<FilaOrdenDocumento[], TClave> clave,
        bool descendente)
    {
        return descendente ? grupos.OrderByDescending(clave) : grupos.OrderBy(clave);
    }

    private sealed record FilaOrdenDocumento(
        Guid DocumentoId,
        Guid ExpedienteId,
        string NumeroExpediente,
        string? CodigoTrata,
        string NumeroActuacionCompleto,
        DateTimeOffset? FechaCreacion,
        string? Referencia);

    public async Task<ConsultaCoberturaDetalleResult> ConsultarCoberturaDetalleAsync(IReadOnlyCollection<Guid> trataIds, CancellationToken cancellationToken)
    {
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(trataIds, cancellationToken);
        var query = _expedienteRepository.Query().Where(x => x.TrataId.HasValue);
        if (trataIdsConsulta.Length > 0) query = query.Where(x => trataIdsConsulta.Contains(x.TrataId!.Value));
        int detallados = await query.Where(x => x.HistorialCacheControl != null && x.HistorialCacheControl.FechaUltimaConsultaGdeba != null).CountAsync(cancellationToken);
        int sinDetallar = await query.Where(x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null).CountAsync(cancellationToken);
        return new ConsultaCoberturaDetalleResult(detallados, sinDetallar);
    }

    private static readonly MethodInfo MetodoContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    // Filtros de texto de las grillas: cada valor tipeado se busca por "contiene", y varios valores se unen con O (LIKE encadenados, compatible con SQL Server 2008).
    private static Expression<Func<T, bool>> ContieneAlguno<T>(IReadOnlyCollection<string> valores, Expression<Func<T, string?>> selector)
    {
        ParameterExpression parametro = selector.Parameters[0];
        Expression? cuerpo = null;
        foreach (string valor in valores)
        {
            MethodCallExpression contiene = Expression.Call(selector.Body, ConsultaExpedientesReadStore.MetodoContains, Expression.Constant(valor));
            cuerpo = cuerpo is null ? contiene : Expression.OrElse(cuerpo, contiene);
        }

        return Expression.Lambda<Func<T, bool>>(cuerpo!, parametro);
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
            expediente.Motivo ?? expediente.DescripcionAdicional,
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

}
