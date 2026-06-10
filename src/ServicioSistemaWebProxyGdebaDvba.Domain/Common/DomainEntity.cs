using TrackableEntities.Common.Core;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Common;

public abstract class DomainEntity : URF.Core.EF.Trackable.Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    protected void MarcarComoAgregada()
    {
        TrackingState = TrackingState.Added;
    }

    protected void MarcarComoModificada()
    {
        if (TrackingState == TrackingState.Unchanged)
        {
            TrackingState = TrackingState.Modified;
        }
    }
}
