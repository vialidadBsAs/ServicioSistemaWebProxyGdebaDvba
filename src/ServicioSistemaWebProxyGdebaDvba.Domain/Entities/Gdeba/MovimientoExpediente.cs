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

    public bool TieneMismosDatos(
        MovimientoExpedienteDetectado movimientoDetectado,
        bool esUltimoConocido)
    {
        ArgumentNullException.ThrowIfNull(movimientoDetectado);

        return FechaOperacion == movimientoDetectado.FechaOperacion &&
            EstadoOrigen == Normalizar(movimientoDetectado.EstadoOrigen) &&
            EstadoDestino == Normalizar(movimientoDetectado.EstadoDestino) &&
            UsuarioOrigen == Normalizar(movimientoDetectado.UsuarioOrigen) &&
            UsuarioDestino == Normalizar(movimientoDetectado.UsuarioDestino) &&
            Motivo == Normalizar(movimientoDetectado.Motivo) &&
            ReparticionOrigen == Normalizar(movimientoDetectado.ReparticionOrigen) &&
            ReparticionDestino == Normalizar(movimientoDetectado.ReparticionDestino) &&
            EsUltimoConocido == esUltimoConocido;
    }

    public void ActualizarDesde(
        MovimientoExpedienteDetectado movimientoDetectado,
        bool esUltimoConocido)
    {
        ArgumentNullException.ThrowIfNull(movimientoDetectado);

        FechaOperacion = movimientoDetectado.FechaOperacion;
        EstadoOrigen = Normalizar(movimientoDetectado.EstadoOrigen);
        EstadoDestino = Normalizar(movimientoDetectado.EstadoDestino);
        UsuarioOrigen = Normalizar(movimientoDetectado.UsuarioOrigen);
        UsuarioDestino = Normalizar(movimientoDetectado.UsuarioDestino);
        Motivo = Normalizar(movimientoDetectado.Motivo);
        ReparticionOrigen = Normalizar(movimientoDetectado.ReparticionOrigen);
        ReparticionDestino = Normalizar(movimientoDetectado.ReparticionDestino);
        EsUltimoConocido = esUltimoConocido;
    }

    public void MarcarComoNoUltimo()
    {
        EsUltimoConocido = false;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
