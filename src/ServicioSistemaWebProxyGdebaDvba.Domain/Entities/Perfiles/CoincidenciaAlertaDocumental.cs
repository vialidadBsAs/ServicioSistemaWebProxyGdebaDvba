using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

// Hecho detectado: un vinculo expediente-documento que cumplio los criterios de una alerta. Nace junto con el vinculo, en la misma
// unidad de trabajo que lo persiste, y queda pendiente hasta que el usuario pasa por la pantalla de alertas.
public sealed class CoincidenciaAlertaDocumental : DomainEntity
{
    private CoincidenciaAlertaDocumental()
    {
    }

    public CoincidenciaAlertaDocumental(Guid alertaDocumentalId, Guid expedienteDocumentoId, DateTimeOffset fechaDeteccion)
    {
        AlertaDocumentalId = alertaDocumentalId == Guid.Empty
            ? throw new ArgumentException("La alerta es requerida.", nameof(alertaDocumentalId))
            : alertaDocumentalId;
        ExpedienteDocumentoId = expedienteDocumentoId == Guid.Empty
            ? throw new ArgumentException("El vinculo expediente-documento es requerido.", nameof(expedienteDocumentoId))
            : expedienteDocumentoId;
        FechaDeteccion = fechaDeteccion;
    }

    public Guid AlertaDocumentalId { get; private set; }

    public AlertaDocumental AlertaDocumental { get; private set; } = null!;

    public Guid ExpedienteDocumentoId { get; private set; }

    public ExpedienteDocumento ExpedienteDocumento { get; private set; } = null!;

    public DateTimeOffset FechaDeteccion { get; private set; }

    public DateTimeOffset? FechaVista { get; private set; }

    public bool EstaPendiente => FechaVista is null;

    public void MarcarVista(DateTimeOffset fecha)
    {
        if (FechaVista is not null)
        {
            return;
        }

        MarcarComoModificada();
        FechaVista = fecha;
    }
}
