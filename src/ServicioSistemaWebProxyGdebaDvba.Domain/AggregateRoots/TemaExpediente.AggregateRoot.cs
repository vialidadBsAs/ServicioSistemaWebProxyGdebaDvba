namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class TemaExpediente
{
    public void Actualizar(string codigo, string nombre, string? descripcion)
    {
        Codigo = NormalizarRequerido(codigo, nameof(codigo)).ToUpperInvariant();
        Nombre = NormalizarRequerido(nombre, nameof(nombre));
        Descripcion = Normalizar(descripcion);
        MarcarComoModificada();
    }

    public TemaExpedienteTrata AsignarTrata(TrataHabilitadaVialidad trata)
    {
        ArgumentNullException.ThrowIfNull(trata);
        var existente = _tratas.FirstOrDefault(x => x.TrataHabilitadaVialidadId == trata.Id);
        if (existente is not null) return existente;
        var asignacion = new TemaExpedienteTrata(Id, trata.Id) { TrackingState = TrackableEntities.Common.Core.TrackingState.Added };
        _tratas.Add(asignacion);
        return asignacion;
    }

    public bool QuitarTrata(Guid trataHabilitadaVialidadId)
    {
        var asignacion = _tratas.FirstOrDefault(x => x.TrataHabilitadaVialidadId == trataHabilitadaVialidadId);
        if (asignacion is null) return false;

        asignacion.TrackingState = TrackableEntities.Common.Core.TrackingState.Deleted;
        _tratas.Remove(asignacion);
        MarcarComoModificada();
        return true;
    }
}
