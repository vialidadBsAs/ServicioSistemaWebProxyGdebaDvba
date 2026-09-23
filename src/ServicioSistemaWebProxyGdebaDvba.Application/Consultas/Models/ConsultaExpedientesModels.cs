namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

public sealed record ConsultaExpedientesRequest(IReadOnlyCollection<Guid>? TrataIds, int Pagina = 1, int TamanioPagina = 50, string? CampoOrden = null, string? DireccionOrden = null, IReadOnlyCollection<string>? CodigosTrata = null, IReadOnlyCollection<string>? EstadosActuales = null, IReadOnlyCollection<string>? EstadosDetalle = null, IReadOnlyCollection<string>? NumerosExpediente = null, FiltroFecha? FiltroFechaUltimoMovimiento = null, string? Caratula = null, int? Skip = null, int? Take = null, string? Orden = null);

// Un criterio de ordenamiento (campo + direccion); la lista ordena por multiples columnas en el orden dado.
public sealed record CriterioOrdenExpediente(string Campo, bool Descendente);

public sealed record ConsultaExpedientesFiltro(IReadOnlyCollection<Guid> TrataIds, int Pagina, int TamanioPagina, DateTimeOffset FechaConsulta, IReadOnlyList<CriterioOrdenExpediente> Criterios, IReadOnlyCollection<string> CodigosTrata, IReadOnlyCollection<string> EstadosActuales, IReadOnlyCollection<string> EstadosDetalle, IReadOnlyCollection<string> NumerosExpediente, FiltroFecha? FiltroFechaUltimoMovimiento, string? Caratula, int? Skip, int? Take);

public sealed record ConsultaExpedientesResult(int TotalRegistros, int Pagina, int TamanioPagina, IReadOnlyCollection<ConsultaExpedienteDto> Items);

public sealed record ConsultaExpedienteDto(Guid Id, string NumeroGdebaCompleto, string CodigoTrata, string? DescripcionTrata, string? EstadoActual, DateTimeOffset? FechaUltimoMovimiento, string EstadoDetalle, string? Caratula, DateTimeOffset? FechaCaratulacion, string? UltimoPaseSectorDestino, DateTimeOffset? UltimoPaseFecha);

public sealed record ConsultaCoberturaDetalleResult(int Detallados, int SinDetallar);

// Valores de filtro para la busqueda por caratula: listas completas de baja cardinalidad (trata, estado) sobre el match del texto.
public sealed record ConsultaCaratulaValoresFiltroRequest(string Texto, string Campo, IReadOnlyCollection<Guid>? TrataIds);

public sealed record ConsultaCaratulaValoresFiltroFiltro(string Texto, string Campo, IReadOnlyCollection<Guid> TrataIds);

public sealed record ConsultaDocumentosPorTrataRequest(IReadOnlyCollection<Guid>? TrataIds, int Pagina = 1, int TamanioPagina = 50, string? CodigoTipoDocumento = null, string? CampoOrden = null, string? DireccionOrden = null, IReadOnlyCollection<string>? NumerosExpediente = null, IReadOnlyCollection<string>? CodigosTrata = null, IReadOnlyCollection<string>? NumerosActuacion = null, IReadOnlyCollection<string>? Referencias = null, string? ReferenciaContiene = null, IReadOnlyCollection<string>? TiposDocumento = null, FiltroFecha? FiltroFechaCreacion = null, bool SoloSinReferencia = false, bool IncluirResumen = true);

public sealed record ConsultaDocumentosPorTrataFiltro(IReadOnlyCollection<Guid> TrataIds, int Pagina, int TamanioPagina, string? CodigoTipoDocumento, string CampoOrden, bool OrdenDescendente, IReadOnlyCollection<string> NumerosExpediente, IReadOnlyCollection<string> CodigosTrata, IReadOnlyCollection<string> NumerosActuacion, IReadOnlyCollection<string> Referencias, string? ReferenciaContiene, IReadOnlyCollection<string> TiposDocumento, FiltroFecha? FiltroFechaCreacion, bool SoloSinReferencia, bool IncluirResumen);

public sealed record ConsultaDocumentosPorTrataResult(
    int TotalRegistros,
    int Pagina,
    int TamanioPagina,
    int TotalDocumentos,
    int TotalExpedientes,
    int DocumentosConMetadata,
    int TotalDocumentosFiltrados,
    int TotalExpedientesFiltrados,
    IReadOnlyCollection<ConsultaTipoDocumentoResumenDto> TiposDocumento,
    IReadOnlyCollection<ConsultaDocumentoPorTrataDto> Items);

public sealed record ConsultaTipoDocumentoResumenDto(
    string CodigoTipoDocumento,
    int CantidadDocumentos,
    int CantidadExpedientes,
    int CantidadDocumentosConMetadata);

public sealed record ConsultaDocumentoPorTrataDto(
    IReadOnlyCollection<ConsultaDocumentoExpedienteDto> Expedientes,
    Guid DocumentoId,
    string NumeroActuacionCompleto,
    string CodigoActuacion,
    string? CodigoTipoDocumento,
    string? NombreTipoDocumento,
    string? FamiliaTipoDocumento,
    string? Referencia,
    DateTimeOffset? FechaCreacion,
    bool MetadataCompleta,
    string? UrlArchivo,
    bool? PuedeVerDocumento,
    string? UltimaActividad,
    DateTimeOffset? FechaUltimaActividad);

public sealed record ConsultaDocumentoExpedienteDto(Guid Id, string Numero, string CodigoTrata);

// Filtro de fecha de una columna tal como lo arma la grilla (Infragistics): un arbol con operador Y/O sobre rangos [Desde, Hasta) y sub-arboles anidados.
// Viaja como JSON en la query y el read store lo traduce a una unica expresion; una condicion "despues de" es un rango sin Hasta, "antes de" uno sin Desde.
public sealed record FiltroFecha(string Operador, IReadOnlyCollection<RangoFecha>? Rangos = null, IReadOnlyCollection<FiltroFecha>? Subfiltros = null);

public sealed record RangoFecha(DateTimeOffset? Desde, DateTimeOffset? Hasta);

// Cobertura de la busqueda por referencia: cuantos documentos del tema tienen referencia conocida (los unicos que participan) y cuantos aun no.
public sealed record ConsultaCoberturaReferenciaResult(int ConReferencia, int SinReferencia);
