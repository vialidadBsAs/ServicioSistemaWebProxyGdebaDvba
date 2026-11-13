using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

public sealed record GuardarAlertaDocumentalRequest(
    string TipoDocumentoCodigo, string? TipoEspecificoCodigo, Guid? TrataId, string? UsuarioGenerador, string? Nombre, bool Activa = true);

public sealed record AlertaDocumentalDto(
    Guid Id,
    string? Nombre,
    string Descripcion,
    string TipoDocumentoCodigo,
    string? TipoDocumentoNombre,
    string? TipoEspecificoCodigo,
    string? TipoEspecificoNombre,
    Guid? TrataId,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? UsuarioGenerador,
    bool Activa,
    DateTimeOffset FechaCreacion,
    int Pendientes);

public sealed record CoincidenciaAlertaDocumentalDto(
    Guid Id,
    Guid AlertaId,
    string DescripcionAlerta,
    Guid DocumentoId,
    string NumeroDocumento,
    bool? PuedeVerDocumento,
    string TipoDocumentoCodigo,
    string? TipoDocumentoNombre,
    string? Referencia,
    DateTimeOffset? FechaDocumento,
    // Cuando el documento entro al expediente; puede ser dias posterior a la firma y explica por que se detecto recien entonces.
    DateTimeOffset? FechaVinculacion,
    string? UsuarioGenerador,
    Guid ExpedienteId,
    string NumeroExpediente,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? CaratulaExpediente,
    DateTimeOffset FechaDeteccion,
    bool Pendiente,
    IReadOnlyCollection<ExpedienteVinculadoDto> OtrosExpedientes);

// Otros expedientes donde el mismo documento esta vinculado: la coincidencia es una por documento, pero la persona puede querer ir a cualquiera.
public sealed record ExpedienteVinculadoDto(Guid ExpedienteId, string NumeroExpediente);

// Pestana "Alertas": las novedades son lo detectado hoy (visto o no) mas lo no revisado de los ultimos DiasSinRevisar dias. Todo lo
// demas vive en el historico; SinRevisarEnHistorico es lo que paso al historico sin haberse revisado (alimenta el contador de su pestana).
public sealed record NovedadesAlertasDocumentalesDto(int DiasSinRevisar, IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> Coincidencias, int SinRevisarEnHistorico);

// Pagina del historico de alertas tal como la pide la grilla: paginado, orden y filtros de columna resueltos en el servidor. Los
// filtros de texto son "contiene" (O entre valores de una misma columna); SoloSinRevisar nulo no filtra.
public sealed record ConsultaHistoricoAlertasRequest(
    int Pagina = 1,
    int TamanioPagina = 50,
    string? CampoOrden = null,
    string? DireccionOrden = null,
    IReadOnlyCollection<string>? Alertas = null,
    IReadOnlyCollection<string>? NumerosDocumento = null,
    IReadOnlyCollection<string>? Referencias = null,
    IReadOnlyCollection<string>? NumerosExpediente = null,
    IReadOnlyCollection<string>? CodigosTrata = null,
    IReadOnlyCollection<string>? Generadores = null,
    FiltroFecha? FiltroFechaDocumento = null,
    FiltroFecha? FiltroFechaDeteccion = null,
    bool? SoloSinRevisar = null,
    // Detalle de una alerta en la vista agrupada: solo sus coincidencias.
    Guid? AlertaId = null);

// SinRevisar cuenta todo el historico del usuario, no solo lo filtrado: es el contador de la pestana.
public sealed record HistoricoAlertasDocumentalesDto(int TotalRegistros, int SinRevisar, int Pagina, int TamanioPagina, IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> Items);

// Historico agrupado por alerta (una fila por alerta, como los expedientes caratulados por trata en estadisticas): cuantos documentos
// tiene cada una en el historico, cuantos quedaron sin revisar y cuando fue la ultima deteccion. El detalle se pide al desplegar la fila.
public sealed record ResumenHistoricoAlertasDto(int SinRevisar, IReadOnlyCollection<ResumenHistoricoAlertaDto> Alertas);

public sealed record ResumenHistoricoAlertaDto(Guid AlertaId, string DescripcionAlerta, string TipoDocumentoCodigo, int TotalDocumentos, int SinRevisar, DateTimeOffset UltimaDeteccion);

// "Marcar como revisadas" del historico: las filas elegidas; sin filas, todo lo no revisado del historico.
public sealed record MarcarHistoricoRevisadoRequest(IReadOnlyCollection<Guid>? CoincidenciaIds);

// Lectura liviana para el badge y el aviso en pantalla: total de pendientes y las detectadas despues de la lectura anterior.
public sealed record PendientesAlertasDocumentalesDto(int Total, DateTimeOffset Consultado, IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> Nuevas);

// Opciones del selector de tipo documental, en dos niveles: la sigla entera (CodigoEspecifico nulo: "todas las RS") y, debajo, cada
// tipo especifico (ACMED, DICTA, RESO...) con el nombre del catalogo cuando lo tiene y la cantidad de documentos observados (0 si solo
// esta en el catalogo con sigla confirmada por GDEBA).
public sealed record TipoDocumentoAlertaDto(string Acronimo, string? CodigoEspecifico, string? Nombre, int CantidadDocumentos);
