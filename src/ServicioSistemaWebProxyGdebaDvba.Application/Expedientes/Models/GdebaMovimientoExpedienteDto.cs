namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record GdebaMovimientoExpedienteDto(
    int Orden,
    DateTimeOffset? FechaOperacion,
    string? EstadoOrigen,
    string? EstadoDestino,
    string? UsuarioOrigen,
    string? UsuarioDestino,
    string? Motivo,
    string? ReparticionOrigen,
    string? ReparticionDestino);
