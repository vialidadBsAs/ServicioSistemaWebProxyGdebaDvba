namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record GdebaExpedienteDetalladoDto(
    string NumeroGdebaCompleto,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? Estado,
    string? SistemaOrigen,
    string? DescripcionTramite,
    DateTimeOffset? FechaCaratulacion,
    string? UsuarioCaratulador,
    string? UsuarioDestino,
    IReadOnlyCollection<GdebaDocumentoExpedienteDto> Documentos,
    IReadOnlyCollection<string> ArchivosAdjuntos,
    IReadOnlyCollection<GdebaRelacionExpedienteDto> Relaciones);

public sealed record GdebaDocumentoExpedienteDto(
    string NumeroActuacionCompleto,
    string? TipoDocumentoCodigo,
    string? Referencia,
    DateTimeOffset? FechaCreacion,
    DateTimeOffset? FechaVinculacion,
    string? UsuarioAsociacion,
    string? UsuarioGenerador);

public sealed record GdebaRelacionExpedienteDto(
    string NumeroExpedienteRelacionado,
    string TipoRelacion,
    bool? EsCabecera);
