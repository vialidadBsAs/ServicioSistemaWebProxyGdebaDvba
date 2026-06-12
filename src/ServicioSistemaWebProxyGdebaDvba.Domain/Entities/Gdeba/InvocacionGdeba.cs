using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class InvocacionGdeba : DomainEntity
{
    private InvocacionGdeba()
    {
    }

    public InvocacionGdeba(Guid operacionId, AmbienteGdeba ambiente, DateTimeOffset fecha, bool exitosa, int? estadoHttp)
    {
        OperacionId = operacionId == Guid.Empty
            ? throw new ArgumentException("La operacion es requerida.", nameof(operacionId))
            : operacionId;
        Ambiente = ambiente;
        Fecha = fecha;
        Exitosa = exitosa;
        EstadoHttp = estadoHttp;
    }

    public Guid OperacionId { get; private set; }

    public OperacionGdeba Operacion { get; private set; } = null!;

    public AmbienteGdeba Ambiente { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public bool Exitosa { get; private set; }

    public int? EstadoHttp { get; private set; }
}
