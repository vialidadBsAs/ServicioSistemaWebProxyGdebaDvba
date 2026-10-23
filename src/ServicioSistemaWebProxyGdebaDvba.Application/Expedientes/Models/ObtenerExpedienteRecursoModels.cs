using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record ObtenerExpedienteRecursoRequest(
    string NumeroGdebaCompleto,
    bool ForceRefresh = false,
    OrigenInvocacionGdeba? Origen = null);

public sealed record ObtenerExpedienteRecursoResult<T>(
    string NumeroGdebaCompleto,
    T? Datos,
    FuenteRespuesta Fuente,
    bool Exitoso,
    DateTimeOffset ResolvedAt,
    DateTimeOffset? CachedAt,
    bool SinConexion = false);

public sealed record CabeceraExpedienteDto(
    string NumeroGdebaCompleto,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? Estado,
    string? SistemaOrigen,
    string? Motivo,
    string? DescripcionAdicional,
    DateTimeOffset? FechaCaratulacion,
    string? UsuarioCaratulador,
    string? UsuarioDestino,
    string? SectorDestino,
    string? ReparticionActual,
    // Descripciones resueltas por el catalogo local de reparticiones y sectores; la caratuladora sale de la reparticion del numero GDEBA.
    string? ReparticionCaratuladora = null,
    string? ReparticionCaratuladoraDescripcion = null,
    string? ReparticionActualDescripcion = null,
    string? SectorDestinoDescripcion = null);

public sealed record ExpedienteCompletoDto(
    CabeceraExpedienteDto Cabecera,
    IReadOnlyCollection<DocumentoExpedienteDto> Documentos,
    IReadOnlyCollection<ArchivoAdjuntoExpedienteDto> ArchivosAdjuntos,
    IReadOnlyCollection<MovimientoExpedienteDto> Pases,
    IReadOnlyCollection<RelacionExpedienteDto> Relaciones,
    string EstadoDetalle);
