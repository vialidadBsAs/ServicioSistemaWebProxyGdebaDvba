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

// Lectura liviana para el badge y el aviso en pantalla: total de pendientes y las detectadas despues de la lectura anterior.
public sealed record PendientesAlertasDocumentalesDto(int Total, DateTimeOffset Consultado, IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> Nuevas);

// Opciones del selector de tipo documental, en dos niveles: el acronimo entero (CodigoEspecifico nulo: "todas las RS") y, debajo, cada
// tipo especifico observado en los documentos conocidos (ACMED, DICTA, RESO...), con el nombre del catalogo cuando lo tiene. Lo
// observado sale de los documentos; el historial de pases informa el tipo especifico de cada uno.
public sealed record TipoDocumentoAlertaDto(string Acronimo, string? CodigoEspecifico, string? Nombre, int CantidadDocumentos);
