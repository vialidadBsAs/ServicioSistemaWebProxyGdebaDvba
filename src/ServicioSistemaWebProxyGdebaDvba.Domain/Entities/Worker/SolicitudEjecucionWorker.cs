using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed class SolicitudEjecucionWorker : DomainEntity
{
    private SolicitudEjecucionWorker()
    {
    }

    public SolicitudEjecucionWorker(ProcesoWorker proceso, string solicitadaPor, DateTimeOffset fechaSolicitud)
    {
        Proceso = proceso;
        SolicitadaPor = string.IsNullOrWhiteSpace(solicitadaPor) ? "Administracion" : solicitadaPor.Trim();
        FechaSolicitud = fechaSolicitud;
        Estado = EstadoSolicitudEjecucionWorker.PendienteDeInicio;
    }

    public ProcesoWorker Proceso { get; private set; }
    public EstadoSolicitudEjecucionWorker Estado { get; private set; }
    public string SolicitadaPor { get; private set; } = string.Empty;
    public DateTimeOffset FechaSolicitud { get; private set; }
    public DateTimeOffset? FechaInicio { get; private set; }
    public DateTimeOffset? FechaFinalizacion { get; private set; }
    public string? Mensaje { get; private set; }
    public Guid? EjecucionWorkerId { get; private set; }

    public void PrepararParaEjecucion()
    {
        if (Estado != EstadoSolicitudEjecucionWorker.PendienteDeInicio)
        {
            throw new InvalidOperationException("La solicitud manual no puede volver a iniciarse.");
        }

        Estado = EstadoSolicitudEjecucionWorker.Pendiente;
        Mensaje = "En cola para ser tomada por el Worker.";
        this.MarcarComoModificada();
    }

    public void Iniciar(Guid ejecucionWorkerId, DateTimeOffset fechaInicio)
    {
        EjecucionWorkerId = ejecucionWorkerId;
        FechaInicio = fechaInicio;
        Estado = EstadoSolicitudEjecucionWorker.EnEjecucion;
        Mensaje = null;
        this.MarcarComoModificada();
    }

    public void Finalizar(EstadoEjecucionWorker estadoEjecucion, string? mensaje, DateTimeOffset fechaFinalizacion)
    {
        Estado = estadoEjecucion == EstadoEjecucionWorker.Fallida
            ? EstadoSolicitudEjecucionWorker.Fallida
            : EstadoSolicitudEjecucionWorker.Finalizada;
        FechaFinalizacion = fechaFinalizacion;
        Mensaje = string.IsNullOrWhiteSpace(mensaje) ? null : mensaje.Trim();
        this.MarcarComoModificada();
    }
}
