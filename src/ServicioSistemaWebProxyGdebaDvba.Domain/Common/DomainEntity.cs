namespace ServicioSistemaWebProxyGdebaDvba.Domain.Common;

public abstract class DomainEntity : URF.Core.EF.Trackable.Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
