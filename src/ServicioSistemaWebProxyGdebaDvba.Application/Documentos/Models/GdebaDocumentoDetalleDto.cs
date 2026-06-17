namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record GdebaDocumentoDetalleDto(
    string NumeroDocumento,
    string? NumeroEspecial,
    string? TipoDocumentoCodigo,
    string? TipoDocumentoNombre,
    string? TipoDocumentoDescripcion,
    string? Referencia,
    DateTimeOffset? FechaCreacion,
    string? ListaFirmantes,
    string? UrlArchivo,
    bool? PuedeVerDocumento,
    IReadOnlyCollection<GdebaHistorialDocumentoDto> Historial);

public sealed record GdebaHistorialDocumentoDto(
    long IdGdeba,
    string? Actividad,
    DateTimeOffset? FechaInicio,
    DateTimeOffset? FechaFin,
    string? Usuario,
    string? NombreUsuario,
    string? WorkflowOrigen);
