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
    string? ReparticionDestino,
    string? SectorDestino,
    // GDEBA informa en cada pase la descripcion de reparticion y sector junto con el codigo; alimentan el catalogo local, no se guardan por pase.
    string? ReparticionOrigenDescripcion = null,
    string? ReparticionDestinoDescripcion = null,
    string? SectorOrigen = null,
    string? SectorOrigenDescripcion = null,
    string? SectorDestinoDescripcion = null);
