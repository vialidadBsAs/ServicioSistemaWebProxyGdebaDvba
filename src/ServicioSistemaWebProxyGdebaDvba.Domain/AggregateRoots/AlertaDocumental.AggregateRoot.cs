using TrackableEntities.Common.Core;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

// La alerta es el aggregate de sus coincidencias: el detector le pide registrar, la pantalla le pide marcar vistas y podar.
// El alta, la edicion y la baja de la alerta siguen pasando por PerfilUsuario, que es su dueno.
public sealed partial class AlertaDocumental
{
    public const int CantidadMaximaCoincidenciasVistas = 10;

    public void ActualizarCriterios(string tipoDocumentoCodigo, Guid? trataId, string? usuarioGenerador, string? nombre)
    {
        TipoDocumentoCodigo = string.IsNullOrWhiteSpace(tipoDocumentoCodigo)
            ? throw new ArgumentException("El tipo documental es requerido.", nameof(tipoDocumentoCodigo))
            : tipoDocumentoCodigo.Trim().ToUpperInvariant();
        TrataId = trataId == Guid.Empty ? null : trataId;
        UsuarioGenerador = Normalizar(usuarioGenerador);
        Nombre = Normalizar(nombre);
        MarcarComoModificada();
    }

    public void CambiarActiva(bool activa)
    {
        if (Activa == activa)
        {
            return;
        }

        Activa = activa;
        MarcarComoModificada();
    }

    // Decide si un documento recien detectado es una coincidencia. La fecha que manda es la de creacion del documento en GDEBA (la firma):
    // una resolucion vieja que alguien vincula hoy no es una resolucion nueva. Sin fecha de creacion no se puede decidir y no se alerta.
    // El corte es el dia de creacion de la alerta completo: quien la define a la tarde tambien quiere la resolucion firmada esa manana.
    public bool Coincide(string tipoDocumentoCodigo, Guid? trataIdExpediente, string? usuarioGenerador, DateTimeOffset? fechaDocumento)
    {
        DateTimeOffset inicioDelDiaDeCreacion = new(FechaCreacion.Date, FechaCreacion.Offset);
        if (!Activa || fechaDocumento is null || fechaDocumento < inicioDelDiaDeCreacion)
        {
            return false;
        }

        if (!string.Equals(TipoDocumentoCodigo, tipoDocumentoCodigo?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TrataId is not null && TrataId != trataIdExpediente)
        {
            return false;
        }

        return UsuarioGenerador is null ||
            string.Equals(UsuarioGenerador, usuarioGenerador?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public CoincidenciaAlertaDocumental RegistrarCoincidencia(Guid expedienteDocumentoId, DateTimeOffset fechaDeteccion)
    {
        CoincidenciaAlertaDocumental? existente = _coincidencias.FirstOrDefault(x => x.ExpedienteDocumentoId == expedienteDocumentoId);
        if (existente is not null)
        {
            return existente;
        }

        CoincidenciaAlertaDocumental coincidencia = new(Id, expedienteDocumentoId, fechaDeteccion) { TrackingState = TrackingState.Added };
        _coincidencias.Add(coincidencia);
        return coincidencia;
    }

    // Marca vistas todas las pendientes y deja solo las ultimas N vistas; las pendientes nunca se podan. Devuelve las eliminadas
    // para que el servicio pueda registrarlas si hace falta.
    public IReadOnlyCollection<CoincidenciaAlertaDocumental> MarcarCoincidenciasVistas(DateTimeOffset fecha)
    {
        foreach (CoincidenciaAlertaDocumental coincidencia in _coincidencias.Where(x => x.EstaPendiente))
        {
            coincidencia.MarcarVista(fecha);
        }

        CoincidenciaAlertaDocumental[] excedentes = _coincidencias
            .Where(x => !x.EstaPendiente)
            .OrderByDescending(x => x.FechaDeteccion)
            .Skip(CantidadMaximaCoincidenciasVistas)
            .ToArray();
        foreach (CoincidenciaAlertaDocumental excedente in excedentes)
        {
            excedente.TrackingState = TrackingState.Deleted;
            _coincidencias.Remove(excedente);
        }

        if (excedentes.Length > 0)
        {
            MarcarComoModificada();
        }

        return excedentes;
    }
}
