namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

public sealed record ConsultaExpedientesRequest(IReadOnlyCollection<Guid>? TrataIds, int Pagina = 1, int TamanioPagina = 50, string? CampoOrden = null, string? DireccionOrden = null, IReadOnlyCollection<string>? CodigosTrata = null, IReadOnlyCollection<string>? EstadosActuales = null, IReadOnlyCollection<string>? EstadosDetalle = null, IReadOnlyCollection<string>? NumerosExpediente = null, DateTimeOffset? FechaUltimoMovimientoDesde = null, DateTimeOffset? FechaUltimoMovimientoHasta = null, string? Caratula = null);

public sealed record ConsultaExpedientesFiltro(IReadOnlyCollection<Guid> TrataIds, int Pagina, int TamanioPagina, DateTimeOffset FechaConsulta, string CampoOrden, bool OrdenDescendente, IReadOnlyCollection<string> CodigosTrata, IReadOnlyCollection<string> EstadosActuales, IReadOnlyCollection<string> EstadosDetalle, IReadOnlyCollection<string> NumerosExpediente, DateTimeOffset? FechaUltimoMovimientoDesde, DateTimeOffset? FechaUltimoMovimientoHasta, string? Caratula);

public sealed record ConsultaExpedientesResult(int TotalRegistros, int Pagina, int TamanioPagina, IReadOnlyCollection<ConsultaExpedienteDto> Items);

public sealed record ConsultaExpedienteDto(Guid Id, string NumeroGdebaCompleto, string CodigoTrata, string? DescripcionTrata, string? EstadoActual, DateTimeOffset? FechaUltimoMovimiento, string EstadoDetalle, string? Caratula, DateTimeOffset? FechaCaratulacion);

public sealed record ConsultaCoberturaDetalleResult(int Detallados, int SinDetallar);

public sealed record ConsultaExpedientesValoresFiltroRequest(IReadOnlyCollection<Guid>? TrataIds, string Campo);

public sealed record ConsultaDocumentosPorTrataRequest(IReadOnlyCollection<Guid>? TrataIds, int Pagina = 1, int TamanioPagina = 50, string? CodigoTipoDocumento = null, string? CampoOrden = null, string? DireccionOrden = null, IReadOnlyCollection<string>? NumerosExpediente = null, IReadOnlyCollection<string>? CodigosTrata = null, IReadOnlyCollection<string>? NumerosActuacion = null, IReadOnlyCollection<string>? Referencias = null);

public sealed record ConsultaDocumentosPorTrataFiltro(IReadOnlyCollection<Guid> TrataIds, int Pagina, int TamanioPagina, string? CodigoTipoDocumento, string CampoOrden, bool OrdenDescendente, IReadOnlyCollection<string> NumerosExpediente, IReadOnlyCollection<string> CodigosTrata, IReadOnlyCollection<string> NumerosActuacion, IReadOnlyCollection<string> Referencias);

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
