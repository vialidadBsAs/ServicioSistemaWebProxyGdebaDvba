using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ObtenerExpedienteRecursoRequest(
    string NumeroGdebaCompleto,
    bool ForceRefresh = false);

public sealed record ObtenerExpedienteRecursoResult<T>(
    string NumeroGdebaCompleto,
    T? Datos,
    FuenteRespuesta Fuente,
    bool Exitoso,
    DateTimeOffset ResolvedAt,
    DateTimeOffset? CachedAt);

public sealed record CabeceraExpedienteDto(
    string NumeroGdebaCompleto,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? Estado,
    string? SistemaOrigen,
    string? DescripcionTramite,
    DateTimeOffset? FechaCaratulacion,
    string? UsuarioCaratulador,
    string? UsuarioDestino,
    string? SectorDestino,
    string? ReparticionActual);

public sealed record ExpedienteCompletoDto(
    CabeceraExpedienteDto Cabecera,
    IReadOnlyCollection<DocumentoExpedienteDto> Documentos,
    IReadOnlyCollection<ArchivoAdjuntoExpedienteDto> ArchivosAdjuntos,
    IReadOnlyCollection<MovimientoExpedienteDto> Pases,
    IReadOnlyCollection<RelacionExpedienteDto> Relaciones);
