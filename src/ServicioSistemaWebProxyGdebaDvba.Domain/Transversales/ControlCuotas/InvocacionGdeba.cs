using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class InvocacionGdeba : DomainEntity
{
    private InvocacionGdeba()
    {
    }

    public InvocacionGdeba(
        Guid operacionId,
        AmbienteGdeba ambiente,
        DateTimeOffset fecha,
        OrigenInvocacionGdeba origen,
        Guid solicitudId,
        int numeroIntento,
        bool servidorRespondio,
        bool exitosa,
        int? estadoHttp,
        long? duracionMilisegundos)
    {
        OperacionId = operacionId == Guid.Empty
            ? throw new ArgumentException("La operacion es requerida.", nameof(operacionId))
            : operacionId;
        SolicitudId = solicitudId == Guid.Empty
            ? throw new ArgumentException("La solicitud es requerida.", nameof(solicitudId))
            : solicitudId;
        NumeroIntento = numeroIntento > 0
            ? numeroIntento
            : throw new ArgumentOutOfRangeException(nameof(numeroIntento), "El numero de intento debe ser mayor a cero.");
        Ambiente = ambiente;
        Fecha = fecha;
        Origen = origen;
        ServidorRespondio = servidorRespondio;
        Exitosa = exitosa;
        EstadoHttp = estadoHttp;
        DuracionMilisegundos = duracionMilisegundos;
    }

    public Guid OperacionId { get; private set; }

    public OperacionGdeba Operacion { get; private set; } = null!;

    public AmbienteGdeba Ambiente { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public OrigenInvocacionGdeba Origen { get; private set; }

    public Guid SolicitudId { get; private set; }

    public int NumeroIntento { get; private set; }

    public bool ServidorRespondio { get; private set; }

    public bool Exitosa { get; private set; }

    public int? EstadoHttp { get; private set; }

    public long? DuracionMilisegundos { get; private set; }
}
