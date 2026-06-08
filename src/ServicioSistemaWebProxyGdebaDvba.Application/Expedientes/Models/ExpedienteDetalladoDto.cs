namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ExpedienteDetalladoDto(
    string NumeroGdebaCompleto,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? Estado,
    string? SistemaOrigen,
    string? DescripcionTramite,
    DateTimeOffset? FechaCaratulacion,
    string? UsuarioCaratulador,
    string? UsuarioDestino,
    IReadOnlyCollection<DocumentoExpedienteDto> Documentos,
    IReadOnlyCollection<ArchivoAdjuntoExpedienteDto> ArchivosAdjuntos,
    IReadOnlyCollection<RelacionExpedienteDto> Relaciones);

public sealed record DocumentoExpedienteDto(
    string NumeroActuacionCompleto,
    string? TipoDocumentoCodigo,
    string? Referencia,
    DateTimeOffset? FechaCreacion,
    DateTimeOffset? FechaVinculacion,
    string? UsuarioAsociacion,
    string? UsuarioGenerador);

public sealed record ArchivoAdjuntoExpedienteDto(string NombreArchivo);

public sealed record RelacionExpedienteDto(
    string NumeroExpedienteRelacionado,
    string TipoRelacion,
    bool? EsCabecera);
