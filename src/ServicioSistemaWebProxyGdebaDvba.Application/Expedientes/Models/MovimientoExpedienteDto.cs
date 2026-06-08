namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record MovimientoExpedienteDto(
    int Orden,
    DateTimeOffset? FechaOperacion,
    string? EstadoOrigen,
    string? EstadoDestino,
    string? UsuarioOrigen,
    string? UsuarioDestino,
    string? Motivo,
    string? ReparticionOrigen,
    string? ReparticionDestino,
    bool EsUltimoConocido);
