namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed record MovimientoExpedienteDetectado(
    int Orden,
    DateTimeOffset? FechaOperacion,
    string? EstadoOrigen,
    string? EstadoDestino,
    string? UsuarioOrigen,
    string? UsuarioDestino,
    string? Motivo,
    string? ReparticionOrigen,
    string? ReparticionDestino);
