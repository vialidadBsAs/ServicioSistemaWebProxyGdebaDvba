using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed class EjecucionWorkerExpedienteDescubierto : DomainEntity
{
    private EjecucionWorkerExpedienteDescubierto()
    {
    }

    internal EjecucionWorkerExpedienteDescubierto(Guid ejecucionWorkerDescubrimientoTrataEstadoId, Guid expedienteId)
    {
        EjecucionWorkerDescubrimientoTrataEstadoId = ejecucionWorkerDescubrimientoTrataEstadoId;
        ExpedienteId = expedienteId;
    }

    public Guid EjecucionWorkerDescubrimientoTrataEstadoId { get; private set; }
    public EjecucionWorkerDescubrimientoTrataEstado EjecucionWorkerDescubrimientoTrataEstado { get; private set; } = null!;
    public Guid ExpedienteId { get; private set; }
    public Expediente Expediente { get; private set; } = null!;
}
