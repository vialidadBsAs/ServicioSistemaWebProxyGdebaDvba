using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class MovimientoExpediente : DomainEntity
{
    private MovimientoExpediente()
    {
    }

    public MovimientoExpediente(Guid expedienteId, int orden)
    {
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        Orden = orden <= 0 ? throw new ArgumentOutOfRangeException(nameof(orden), "El orden debe ser mayor a cero.") : orden;
    }

    public Guid ExpedienteId { get; private set; }

    public Expediente Expediente { get; private set; } = null!;

    public int Orden { get; private set; }

    public DateTimeOffset? FechaOperacion { get; private set; }

    public string? EstadoOrigen { get; private set; }

    public string? EstadoDestino { get; private set; }

    public string? UsuarioOrigen { get; private set; }

    public string? UsuarioDestino { get; private set; }

    public string? Motivo { get; private set; }

    public string? ReparticionOrigen { get; private set; }

    public string? ReparticionDestino { get; private set; }

    public bool EsUltimoConocido { get; private set; }

    public bool CoincideCon(MovimientoExpedienteDetectado movimientoDetectado)
    {
        ArgumentNullException.ThrowIfNull(movimientoDetectado);

        if (FechaOperacion.HasValue && movimientoDetectado.FechaOperacion.HasValue)
        {
            return FechaOperacion == movimientoDetectado.FechaOperacion;
        }

        return !FechaOperacion.HasValue &&
            !movimientoDetectado.FechaOperacion.HasValue &&
            UsuarioOrigen == Normalizar(movimientoDetectado.UsuarioOrigen) &&
            UsuarioDestino == Normalizar(movimientoDetectado.UsuarioDestino) &&
            Motivo == Normalizar(movimientoDetectado.Motivo) &&
            ReparticionOrigen == Normalizar(movimientoDetectado.ReparticionOrigen) &&
            ReparticionDestino == Normalizar(movimientoDetectado.ReparticionDestino);
    }

    public void ActualizarDesde(MovimientoExpedienteDetectado movimientoDetectado)
    {
        ArgumentNullException.ThrowIfNull(movimientoDetectado);

        MarcarComoModificada();
        Orden = movimientoDetectado.Orden;
        FechaOperacion = movimientoDetectado.FechaOperacion ?? FechaOperacion;
        EstadoOrigen = Normalizar(movimientoDetectado.EstadoOrigen) ?? EstadoOrigen;
        EstadoDestino = Normalizar(movimientoDetectado.EstadoDestino) ?? EstadoDestino;
        UsuarioOrigen = Normalizar(movimientoDetectado.UsuarioOrigen) ?? UsuarioOrigen;
        UsuarioDestino = Normalizar(movimientoDetectado.UsuarioDestino) ?? UsuarioDestino;
        Motivo = Normalizar(movimientoDetectado.Motivo) ?? Motivo;
        ReparticionOrigen = Normalizar(movimientoDetectado.ReparticionOrigen) ?? ReparticionOrigen;
        ReparticionDestino = Normalizar(movimientoDetectado.ReparticionDestino) ?? ReparticionDestino;
    }

    public void MarcarComoUltimo()
    {
        MarcarComoModificada();
        EsUltimoConocido = true;
    }

    public void MarcarComoNoUltimo()
    {
        MarcarComoModificada();
        EsUltimoConocido = false;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
