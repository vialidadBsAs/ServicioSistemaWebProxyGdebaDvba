namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class TemaExpediente
{
    public TemaExpedienteTrata AsignarTrata(TrataHabilitadaVialidad trata)
    {
        ArgumentNullException.ThrowIfNull(trata);
        var existente = _tratas.FirstOrDefault(x => x.TrataHabilitadaVialidadId == trata.Id);
        if (existente is not null) return existente;
        var asignacion = new TemaExpedienteTrata(Id, trata.Id) { TrackingState = TrackableEntities.Common.Core.TrackingState.Added };
        _tratas.Add(asignacion);
        return asignacion;
    }
}
