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
        // Queryable() (EF crudo) en vez de Query() (wrapper URF) para que el ThenBy del multi-sort traduzca a SQL. No hay filtros globales, trae las mismas filas.
        IQueryable<Expediente> query = _expedienteRepository.Queryable().Where(x => x.TrataId.HasValue);
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

        if (filtro.FiltroFechaUltimoMovimiento is FiltroFecha filtroFechaMovimiento)
        {
            query = query.Where(ConsultaExpedientesReadStore.CumpleFiltroFecha<Expediente>(filtroFechaMovimiento, x => x.HistorialCacheControl!.UltimoMovimientoDetectado!.FechaOperacion));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Caratula))
        {
            string caratulaBuscada = filtro.Caratula.Trim();
            query = query.Where(x =>
                (x.Motivo != null && x.Motivo.Contains(caratulaBuscada)) ||
                (x.DescripcionAdicional != null && x.DescripcionAdicional.Contains(caratulaBuscada)));
        }
        var totalRegistros = await query.CountAsync(cancellationToken);
        // Multi-sort: se aplican los criterios en orden (OrderBy + ThenBy encadenados); el desempate final es el numero de expediente.
        IOrderedQueryable<Expediente>? queryOrdenada = null;
        foreach (CriterioOrdenExpediente criterio in filtro.Criterios)
        {
            queryOrdenada = ConsultaExpedientesReadStore.AplicarCriterioOrden(query, queryOrdenada, criterio, filtro.FechaConsulta);
        }

        queryOrdenada ??= ConsultaExpedientesReadStore.AplicarCriterioOrden(query, null, new CriterioOrdenExpediente("fechaUltimoMovimiento", true), filtro.FechaConsulta);
        // Virtualizacion: si vienen skip/take se usan tal cual (startIndex/chunkSize); si no, se pagina por Pagina/TamanioPagina.
        int saltear = filtro.Skip ?? (filtro.Pagina - 1) * filtro.TamanioPagina;
        int tomar = filtro.Take ?? filtro.TamanioPagina;
        var expedientes = await queryOrdenada.ThenByDescending(x => x.GdebaNumeroCompleto)
            .Skip(saltear)
            .Take(tomar)
            .ToArrayAsync(cancellationToken);

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
        // El ultimo pase (EsUltimoConocido) es lo coherente para mostrar: donde esta parado hoy el expediente y cuando se movio por ultima vez.
        var ultimosPases = await _movimientoRepository.Query()
            .Where(x => expedientesIds.Contains(x.ExpedienteId) && x.EsUltimoConocido)
            .SelectAsync(cancellationToken);
        var ultimoPasePorExpedienteId = ultimosPases
            .GroupBy(x => x.ExpedienteId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(m => m.Orden).First());
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
            ultimoPasePorExpedienteId.GetValueOrDefault(x.Id),
            filtro.FechaConsulta)).ToArray();

        return new ConsultaExpedientesResult(totalRegistros, filtro.Pagina, filtro.TamanioPagina, items);
    }

    public async Task<ConsultaDocumentosPorTrataResult> ConsultarDocumentosAsync(ConsultaDocumentosPorTrataFiltro filtro, CancellationToken cancellationToken)
    {
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(filtro.TrataIds, cancellationToken);

        // Con miles de documentos por tema, materializar los vinculos completos para contar u ordenar en memoria era el cuello de botella:
        // los conteos se resuelven en SQL, el orden usa una proyeccion liviana y solo la pagina visible carga entidades completas.
        // La lista de tratas va como constantes en el SQL y no como parametro JSON (OPENJSON): ver VinculadoAAlgunaTrata.
        IQueryable<ExpedienteDocumento> vinculosTema = _expedienteDocumentoRepository.Queryable()
            .Where(ConsultaExpedientesReadStore.VinculadoAAlgunaTrata(trataIdsConsulta));

        // El resumen del tema (agrupado + totales) solo se calcula cuando la pantalla lo necesita: la busqueda por referencia
        // nunca lo muestra, y la vista por tipo lo pide una unica vez (la paginacion posterior lo conserva del lado cliente).
        bool buscaPorReferencia = !string.IsNullOrWhiteSpace(filtro.ReferenciaContiene) || filtro.SoloSinReferencia;
        ConsultaTipoDocumentoResumenDto[] resumenTipos = Array.Empty<ConsultaTipoDocumentoResumenDto>();
        int totalDocumentos = 0;
        int totalDocumentosConMetadata = 0;
        int totalExpedientes = 0;
        if (!buscaPorReferencia && filtro.IncluirResumen)
        {
            // Resumen en conjuntos, sin subconsultas correlacionadas por grupo: primero los documentos distintos con sus flags (un documento
            // vinculado a varios expedientes cuenta una vez) y sobre eso los contadores en linea. Solo toca tipo y metadata, que estan
            // cubiertos por el indice IX_DocumentosGdeba_ActuacionTipoCodigo: no lee la tabla ancha ni el texto de Referencia.
            var documentosPorTipo = await vinculosTema
                .Select(x => new
                {
                    x.DocumentoId,
                    x.Documento!.ActuacionTipoCodigo,
                    x.Documento!.MetadataCompleta
                })
                .Distinct()
                .GroupBy(x => x.ActuacionTipoCodigo)
                .Select(grupo => new
                {
                    CodigoTipoDocumento = grupo.Key,
                    CantidadDocumentos = grupo.Count(),
                    CantidadDocumentosConMetadata = grupo.Count(x => x.MetadataCompleta)
                })
                .ToArrayAsync(cancellationToken);
            // Los expedientes se repiten entre tipos: se cuentan aparte, distintos por tipo, sin tocar el texto de los documentos.
            Dictionary<string, int> expedientesPorTipo = (await vinculosTema
                .Select(x => new { x.ExpedienteId, x.Documento!.ActuacionTipoCodigo })
                .Distinct()
                .GroupBy(x => x.ActuacionTipoCodigo)
                .Select(grupo => new { CodigoTipoDocumento = grupo.Key, CantidadExpedientes = grupo.Count() })
                .ToArrayAsync(cancellationToken))
                .ToDictionary(x => x.CodigoTipoDocumento ?? string.Empty, x => x.CantidadExpedientes);
            resumenTipos = documentosPorTipo
                .Select(x => new ConsultaTipoDocumentoResumenDto(x.CodigoTipoDocumento, x.CantidadDocumentos, expedientesPorTipo.GetValueOrDefault(x.CodigoTipoDocumento ?? string.Empty), x.CantidadDocumentosConMetadata))
                .OrderByDescending(x => x.CantidadDocumentos)
                .ThenBy(x => x.CodigoTipoDocumento)
                .ToArray();
            // El tipo particiona a los documentos, por lo que los totales de documentos salen del propio resumen; los expedientes se repiten entre tipos.
            totalDocumentos = resumenTipos.Sum(x => x.CantidadDocumentos);
            totalDocumentosConMetadata = resumenTipos.Sum(x => x.CantidadDocumentosConMetadata);
            totalExpedientes = await vinculosTema.Select(x => x.ExpedienteId).Distinct().CountAsync(cancellationToken);
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

        if (filtro.FiltroFechaCreacion is FiltroFecha filtroFechaCreacion) vinculosFiltrados = vinculosFiltrados.Where(ConsultaExpedientesReadStore.CumpleFiltroFecha<ExpedienteDocumento>(filtroFechaCreacion, x => x.Documento!.FechaCreacion));
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

    public async Task<IReadOnlyCollection<string>> ObtenerValoresFiltroCaratulaAsync(ConsultaCaratulaValoresFiltroFiltro filtro, CancellationToken cancellationToken)
    {
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(filtro.TrataIds, cancellationToken);
        IQueryable<Expediente> query = _expedienteRepository.Queryable().Where(x => x.TrataId.HasValue);
        if (trataIdsConsulta.Length > 0) query = query.Where(x => trataIdsConsulta.Contains(x.TrataId!.Value));
        query = query.Where(x =>
            (x.Motivo != null && x.Motivo.Contains(filtro.Texto)) ||
            (x.DescripcionAdicional != null && x.DescripcionAdicional.Contains(filtro.Texto)));

        if (filtro.Campo == "codigoTrata")
        {
            return await query.Where(x => x.Trata!.CodigoTrata != null).Select(x => x.Trata!.CodigoTrata).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);
        }

        // estadoActual
        return await query.Where(x => x.EstadoActual != null).Select(x => x.EstadoActual!).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);
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

    // Cobertura de la busqueda por referencia. Lee el texto de Referencia de todos los documentos del tema (no hay indice posible sobre un
    // texto largo), por eso es una consulta propia que se pide solo al abrir esa solapa y nunca junto al resumen por tipo documental.
    public async Task<ConsultaCoberturaReferenciaResult> ConsultarCoberturaReferenciaAsync(IReadOnlyCollection<Guid> trataIds, CancellationToken cancellationToken)
    {
        Guid[] trataIdsConsulta = await this.ExpandirTrataIdsPorCodigoAsync(trataIds, cancellationToken);
        if (trataIdsConsulta.Length == 0) return new ConsultaCoberturaReferenciaResult(0, 0);

        IQueryable<ExpedienteDocumento> vinculosTema = _expedienteDocumentoRepository.Queryable()
            .Where(ConsultaExpedientesReadStore.VinculadoAAlgunaTrata(trataIdsConsulta));
        int totalDocumentos = await vinculosTema.Select(x => x.DocumentoId).Distinct().CountAsync(cancellationToken);
        int conReferencia = await vinculosTema
            .Where(x => x.Documento!.Referencia != null && x.Documento!.Referencia != string.Empty)
            .Select(x => x.DocumentoId)
            .Distinct()
            .CountAsync(cancellationToken);
        return new ConsultaCoberturaReferenciaResult(conReferencia, Math.Max(0, totalDocumentos - conReferencia));
    }

    // Aplica un criterio de orden encadenando OrderBy/ThenBy segun sea el primero o uno posterior. El ultimo pase y estado detalle usan subconsultas.
    private static IOrderedQueryable<Expediente> AplicarCriterioOrden(IQueryable<Expediente> query, IOrderedQueryable<Expediente>? previa, CriterioOrdenExpediente criterio, DateTimeOffset fechaConsulta)
    {
        return criterio.Campo switch
        {
            "numeroGdebaCompleto" => Aplicar(query, previa, x => x.GdebaNumeroCompleto, criterio.Descendente),
            "codigoTrata" => Aplicar(query, previa, x => x.Trata!.CodigoTrata, criterio.Descendente),
            "descripcionTrata" => Aplicar(query, previa, x => x.Trata!.DescripcionTrata, criterio.Descendente),
            "estadoActual" => Aplicar(query, previa, x => x.EstadoActual, criterio.Descendente),
            "fechaCaratulacion" => Aplicar(query, previa, x => x.FechaCaratulacion, criterio.Descendente),
            "ultimoPaseSectorDestino" => Aplicar(query, previa, x => x.Movimientos.Where(m => m.EsUltimoConocido).Max(m => m.ReparticionDestino), criterio.Descendente),
            "estadoDetalle" => Aplicar(query, previa, x => x.HistorialCacheControl == null || !x.HistorialCacheControl.EstaCompleto, criterio.Descendente),
            // fechaUltimoMovimiento y ultimoPaseFecha: por la fecha del ultimo pase real (EsUltimoConocido).
            _ => Aplicar(query, previa, x => x.Movimientos.Where(m => m.EsUltimoConocido).Max(m => (DateTimeOffset?)m.FechaOperacion), criterio.Descendente)
        };

        static IOrderedQueryable<Expediente> Aplicar<TKey>(IQueryable<Expediente> origen, IOrderedQueryable<Expediente>? ordenada, System.Linq.Expressions.Expression<Func<Expediente, TKey>> selector, bool descendente)
        {
            if (ordenada is null) return descendente ? origen.OrderByDescending(selector) : origen.OrderBy(selector);
            return descendente ? ordenada.ThenByDescending(selector) : ordenada.ThenBy(selector);
        }
    }

    private static readonly MethodInfo MetodoContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    // Filtros de texto de las grillas: cada valor tipeado se busca por "contiene", y varios valores se unen con O (LIKE encadenados, compatible con SQL Server 2008).
    // Filtro por tratas con la lista escrita como constantes en el SQL ([TrataId] = '...' OR ...), no como parametro JSON de OPENJSON.
    // Con OPENJSON el optimizador estima una cantidad fija de tratas y elegia loops anidados sobre los 270k documentos; con la lista
    // real estima bien y arma el hash join sobre el indice. Mismo mecanismo que ContieneAlguno (EF 8 no tiene EF.Constant).
    private static Expression<Func<ExpedienteDocumento, bool>> VinculadoAAlgunaTrata(IReadOnlyCollection<Guid> trataIds)
    {
        ParameterExpression parametro = Expression.Parameter(typeof(ExpedienteDocumento), "x");
        Expression trataId = Expression.Property(Expression.Property(parametro, nameof(ExpedienteDocumento.Expediente)), nameof(Expediente.TrataId));
        Expression? cuerpo = null;
        foreach (Guid id in trataIds)
        {
            Expression igual = Expression.Equal(trataId, Expression.Constant(id, trataId.Type));
            cuerpo = cuerpo is null ? igual : Expression.OrElse(cuerpo, igual);
        }

        return Expression.Lambda<Func<ExpedienteDocumento, bool>>(cuerpo ?? Expression.Constant(false), parametro);
    }

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

    // Filtro de fecha de una columna (arbol Y/O de rangos, como lo arma la grilla) traducido a una unica expresion con el mismo
    // mecanismo que ContieneAlguno: EF lo manda como un solo WHERE, p. ej. "mes pasado O este mes" = (f >= d1 AND f < h1) OR (f >= d2 AND f < h2).
    private static Expression<Func<T, bool>> CumpleFiltroFecha<T>(FiltroFecha filtro, Expression<Func<T, DateTimeOffset?>> selector)
    {
        Expression? cuerpo = ConsultaExpedientesReadStore.ConstruirFiltroFecha(filtro, selector.Body);
        return Expression.Lambda<Func<T, bool>>(cuerpo ?? Expression.Constant(true), selector.Parameters[0]);
    }

    private static Expression? ConstruirFiltroFecha(FiltroFecha filtro, Expression fecha)
    {
        bool esO = string.Equals(filtro.Operador, "or", StringComparison.OrdinalIgnoreCase);
        IEnumerable<Expression?> terminos = (filtro.Rangos ?? Array.Empty<RangoFecha>()).Select(rango => ConsultaExpedientesReadStore.ConstruirRangoFecha(rango, fecha))
            .Concat((filtro.Subfiltros ?? Array.Empty<FiltroFecha>()).Select(subfiltro => ConsultaExpedientesReadStore.ConstruirFiltroFecha(subfiltro, fecha)));
        Expression? cuerpo = null;
        foreach (Expression? termino in terminos)
        {
            if (termino is null) continue;
            cuerpo = cuerpo is null ? termino : esO ? Expression.OrElse(cuerpo, termino) : Expression.AndAlso(cuerpo, termino);
        }

        return cuerpo;
    }

    private static Expression? ConstruirRangoFecha(RangoFecha rango, Expression fecha)
    {
        Expression? condicion = null;
        if (rango.Desde is DateTimeOffset desde) condicion = Expression.GreaterThanOrEqual(fecha, Expression.Constant(desde, fecha.Type));
        if (rango.Hasta is DateTimeOffset hasta)
        {
            Expression menor = Expression.LessThan(fecha, Expression.Constant(hasta, fecha.Type));
            condicion = condicion is null ? menor : Expression.AndAlso(condicion, menor);
        }

        return condicion;
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
        MovimientoExpediente? ultimoPase,
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
            expediente.FechaCaratulacion,
            ultimoPase?.ReparticionDestino,
            ultimoPase?.FechaOperacion);
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
        return HistorialExpedienteCacheControl.CalcularEstadoDetalle(historial, fechaConsulta);
    }

}
