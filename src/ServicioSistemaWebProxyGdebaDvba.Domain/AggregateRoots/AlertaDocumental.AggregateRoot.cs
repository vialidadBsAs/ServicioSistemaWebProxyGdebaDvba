using TrackableEntities.Common.Core;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

// La alerta es el aggregate de sus coincidencias: el detector le pide registrar, la pantalla le pide marcar vistas.
// El alta, la edicion y la baja de la alerta siguen pasando por PerfilUsuario, que es su dueno.
public sealed partial class AlertaDocumental
{
    public void ActualizarCriterios(string tipoDocumentoCodigo, string? tipoEspecificoCodigo, Guid? trataId, string? usuarioGenerador, string? nombre)
    {
        TipoDocumentoCodigo = string.IsNullOrWhiteSpace(tipoDocumentoCodigo)
            ? throw new ArgumentException("El tipo documental es requerido.", nameof(tipoDocumentoCodigo))
            : tipoDocumentoCodigo.Trim().ToUpperInvariant();
        TipoEspecificoCodigo = Normalizar(tipoEspecificoCodigo)?.ToUpperInvariant();
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
    public bool Coincide(string tipoDocumentoCodigo, string? tipoEspecificoCodigo, Guid? trataIdExpediente, string? usuarioGenerador, DateTimeOffset? fechaDocumento)
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

        // Con tipo especifico, el documento tiene que traerlo igual. Sin tipo especifico, alcanza con el acronimo.
        if (TipoEspecificoCodigo is not null && !string.Equals(TipoEspecificoCodigo, tipoEspecificoCodigo?.Trim(), StringComparison.OrdinalIgnoreCase))
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

    // Marca vistas las pendientes detectadas dentro del rango [detectadasDesde, detectadasAntesDe); un extremo nulo no acota. La pantalla
    // de alertas marca asi las novedades (desde el limite de la ventana) y el historico (antes de ese limite) por separado. Las vistas se
    // conservan todas: el historico las muestra. Devuelve cuantas marco.
    public int MarcarCoincidenciasVistas(DateTimeOffset fecha, DateTimeOffset? detectadasDesde, DateTimeOffset? detectadasAntesDe)
    {
        CoincidenciaAlertaDocumental[] pendientes = _coincidencias
            .Where(x => x.EstaPendiente)
            .Where(x => detectadasDesde is null || x.FechaDeteccion >= detectadasDesde)
            .Where(x => detectadasAntesDe is null || x.FechaDeteccion < detectadasAntesDe)
            .ToArray();
        foreach (CoincidenciaAlertaDocumental coincidencia in pendientes)
        {
            coincidencia.MarcarVista(fecha);
        }

        return pendientes.Length;
    }

    // Marca vistas las coincidencias elegidas (la que la persona abrio, o las filas seleccionadas en el historico); las demas pendientes
    // siguen pendientes. Devuelve cuantas marco; las que no pertenecen a esta alerta o ya estaban vistas no cuentan.
    public int MarcarCoincidenciasVistas(IReadOnlyCollection<Guid> coincidenciaIds, DateTimeOffset fecha)
    {
        ArgumentNullException.ThrowIfNull(coincidenciaIds);
        CoincidenciaAlertaDocumental[] elegidas = _coincidencias.Where(x => x.EstaPendiente && coincidenciaIds.Contains(x.Id)).ToArray();
        foreach (CoincidenciaAlertaDocumental coincidencia in elegidas)
        {
            coincidencia.MarcarVista(fecha);
        }

        return elegidas.Length;
    }
}
