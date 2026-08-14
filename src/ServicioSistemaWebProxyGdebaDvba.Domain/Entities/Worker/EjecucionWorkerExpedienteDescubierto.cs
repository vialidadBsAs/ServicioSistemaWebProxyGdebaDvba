using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed class EjecucionWorkerExpedienteDescubierto : DomainEntity
{
    private EjecucionWorkerExpedienteDescubierto()
    {
    }

    internal EjecucionWorkerExpedienteDescubierto(Guid ejecucionWorkerDescubrimientoTrataEstadoId, Guid expedienteId, TipoDeteccionExpedienteDescubierto tipoDeteccion)
    {
        EjecucionWorkerDescubrimientoTrataEstadoId = ejecucionWorkerDescubrimientoTrataEstadoId;
        ExpedienteId = expedienteId;
        TipoDeteccion = tipoDeteccion;
    }

    public Guid EjecucionWorkerDescubrimientoTrataEstadoId { get; private set; }
    public EjecucionWorkerDescubrimientoTrataEstado EjecucionWorkerDescubrimientoTrataEstado { get; private set; } = null!;
    public Guid ExpedienteId { get; private set; }
    public Expediente Expediente { get; private set; } = null!;
    public TipoDeteccionExpedienteDescubierto TipoDeteccion { get; private set; }
}
