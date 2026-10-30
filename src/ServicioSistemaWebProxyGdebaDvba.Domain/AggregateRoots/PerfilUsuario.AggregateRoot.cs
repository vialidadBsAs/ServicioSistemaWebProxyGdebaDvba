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

    // Las alertas documentales son del perfil: el alta, la edicion y la baja pasan por aca. Sus coincidencias las administra la propia alerta.
    public AlertaDocumental CrearAlertaDocumental(string tipoDocumentoCodigo, Guid? trataId, string? usuarioGenerador, string? nombre, DateTimeOffset fecha)
    {
        AlertaDocumental alerta = new(Id, tipoDocumentoCodigo, trataId, usuarioGenerador, nombre, fecha) { TrackingState = TrackableEntities.Common.Core.TrackingState.Added };
        _alertasDocumentales.Add(alerta);
        return alerta;
    }

    public AlertaDocumental? ActualizarAlertaDocumental(Guid alertaId, string tipoDocumentoCodigo, Guid? trataId, string? usuarioGenerador, string? nombre, bool activa)
    {
        AlertaDocumental? alerta = _alertasDocumentales.FirstOrDefault(x => x.Id == alertaId);
        if (alerta is null) return null;

        alerta.ActualizarCriterios(tipoDocumentoCodigo, trataId, usuarioGenerador, nombre);
        alerta.CambiarActiva(activa);
        return alerta;
    }

    public bool EliminarAlertaDocumental(Guid alertaId)
    {
        AlertaDocumental? alerta = _alertasDocumentales.FirstOrDefault(x => x.Id == alertaId);
        if (alerta is null) return false;

        alerta.TrackingState = TrackableEntities.Common.Core.TrackingState.Deleted;
        _alertasDocumentales.Remove(alerta);
        MarcarComoModificada();
        return true;
    }
}
