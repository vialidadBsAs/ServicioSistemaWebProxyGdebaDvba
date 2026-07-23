using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class TemaExpedienteTrata : DomainEntity
{
    private TemaExpedienteTrata()
    {
    }

    internal TemaExpedienteTrata(Guid temaExpedienteId, Guid trataHabilitadaVialidadId)
    {
        TemaExpedienteId = temaExpedienteId == Guid.Empty ? throw new ArgumentException("El tema es requerido.", nameof(temaExpedienteId)) : temaExpedienteId;
        TrataHabilitadaVialidadId = trataHabilitadaVialidadId == Guid.Empty ? throw new ArgumentException("La trata habilitada es requerida.", nameof(trataHabilitadaVialidadId)) : trataHabilitadaVialidadId;
    }

    public Guid TemaExpedienteId { get; private set; }

    public TemaExpediente TemaExpediente { get; private set; } = null!;

    public Guid TrataHabilitadaVialidadId { get; private set; }

    public TrataHabilitadaVialidad TrataHabilitadaVialidad { get; private set; } = null!;
}
