namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class PerfilUsuario
{
    public void ActualizarUsuarioGdeba(string? usuarioGdeba)
    {
        UsuarioGdeba = Normalizar(usuarioGdeba);
        MarcarComoModificada();
    }

    public SeguimientoExpediente SeguirExpediente(Guid expedienteId, DateTimeOffset fecha)
    {
        SeguimientoExpediente? existente = _seguimientos.FirstOrDefault(x => x.ExpedienteId == expedienteId);
        if (existente is not null) return existente;

        SeguimientoExpediente seguimiento = new(Id, expedienteId, fecha) { TrackingState = TrackableEntities.Common.Core.TrackingState.Added };
        _seguimientos.Add(seguimiento);
        return seguimiento;
    }

    public bool DejarDeSeguir(Guid expedienteId)
    {
        SeguimientoExpediente? seguimiento = _seguimientos.FirstOrDefault(x => x.ExpedienteId == expedienteId);
        if (seguimiento is null) return false;

        seguimiento.TrackingState = TrackableEntities.Common.Core.TrackingState.Deleted;
        _seguimientos.Remove(seguimiento);
        MarcarComoModificada();
        return true;
    }

    public bool MarcarVisto(Guid expedienteId, DateTimeOffset fecha)
    {
        SeguimientoExpediente? seguimiento = _seguimientos.FirstOrDefault(x => x.ExpedienteId == expedienteId);
        if (seguimiento is null) return false;

        seguimiento.RegistrarVista(fecha);
        return true;
    }
}
