namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

public sealed record GuardarAlertaDocumentalRequest(string TipoDocumentoCodigo, Guid? TrataId, string? UsuarioGenerador, string? Nombre, bool Activa = true);

public sealed record AlertaDocumentalDto(
    Guid Id,
    string? Nombre,
    string Descripcion,
    string TipoDocumentoCodigo,
    string? TipoDocumentoNombre,
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
    bool Pendiente);

// Lectura liviana para el badge y el aviso en pantalla: total de pendientes y las detectadas despues de la lectura anterior.
public sealed record PendientesAlertasDocumentalesDto(int Total, DateTimeOffset Consultado, IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> Nuevas);

// Opciones del selector de tipo documental: lo observado en los documentos conocidos, con el nombre del catalogo cuando existe.
public sealed record TipoDocumentoAlertaDto(string Codigo, string? Nombre, int CantidadDocumentos);
