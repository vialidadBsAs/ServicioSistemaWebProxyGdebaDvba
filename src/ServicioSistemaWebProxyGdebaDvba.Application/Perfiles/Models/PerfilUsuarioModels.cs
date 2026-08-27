namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

public sealed record PerfilUsuarioDto(string UsuarioInstitucional, string? UsuarioGdeba);

public sealed record GuardarUsuarioGdebaRequest(string? UsuarioGdeba);

public sealed record AperturaSeguimientoDto(
    bool Siguiendo,
    bool NovedadCabecera,
    bool NovedadMovimientos,
    bool NovedadDocumentos,
    bool NovedadAdjuntos);

public sealed record SeguimientoExpedienteDto(
    Guid ExpedienteId,
    string NumeroGdebaCompleto,
    string? CodigoTrata,
    string? DescripcionTrata,
    string? DescripcionTramite,
    string? EstadoActual,
    DateTimeOffset FechaAgregado,
    DateTimeOffset? FechaUltimaNovedad,
    bool TieneNovedades);
