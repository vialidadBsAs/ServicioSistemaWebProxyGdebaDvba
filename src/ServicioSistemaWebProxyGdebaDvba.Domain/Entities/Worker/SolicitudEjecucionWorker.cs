using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed class SolicitudEjecucionWorker : DomainEntity
{
    private SolicitudEjecucionWorker()
    {
    }

    public SolicitudEjecucionWorker(ProcesoWorker proceso, string solicitadaPor, DateTimeOffset fechaSolicitud, DateTimeOffset? fechaInicioProgramada = null)
    {
        if (fechaInicioProgramada is DateTimeOffset horario && horario <= fechaSolicitud)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaInicioProgramada), "El horario de inicio programado debe ser posterior al momento de la solicitud.");
        }

        Proceso = proceso;
        SolicitadaPor = string.IsNullOrWhiteSpace(solicitadaPor) ? "Administracion" : solicitadaPor.Trim();
        FechaSolicitud = fechaSolicitud;
        FechaInicioProgramada = fechaInicioProgramada;
        Estado = fechaInicioProgramada is null ? EstadoSolicitudEjecucionWorker.PendienteDeInicio : EstadoSolicitudEjecucionWorker.Programada;
        Mensaje = fechaInicioProgramada is null ? null : "Se encolara automaticamente al llegar el horario programado.";
    }

    public ProcesoWorker Proceso { get; private set; }
    public EstadoSolicitudEjecucionWorker Estado { get; private set; }
    public string SolicitadaPor { get; private set; } = string.Empty;
    public DateTimeOffset FechaSolicitud { get; private set; }
    public DateTimeOffset? FechaInicioProgramada { get; private set; }
    public DateTimeOffset? FechaInicio { get; private set; }
    public DateTimeOffset? FechaFinalizacion { get; private set; }
    public string? CanceladaPor { get; private set; }
    public DateTimeOffset? FechaCancelacion { get; private set; }
    public string? Mensaje { get; private set; }
    public Guid? EjecucionWorkerId { get; private set; }

    public void PrepararParaEjecucion()
    {
        if (Estado != EstadoSolicitudEjecucionWorker.PendienteDeInicio && Estado != EstadoSolicitudEjecucionWorker.Programada)
        {
            throw new InvalidOperationException("La solicitud manual no puede volver a iniciarse.");
        }

        Estado = EstadoSolicitudEjecucionWorker.Pendiente;
        Mensaje = "En cola para ser tomada por el Worker.";
        this.MarcarComoModificada();
    }

    public void EncolarPorHorario(DateTimeOffset ahora)
    {
        if (Estado != EstadoSolicitudEjecucionWorker.Programada)
        {
            throw new InvalidOperationException("Solo una solicitud programada puede encolarse por horario.");
        }

        if (FechaInicioProgramada is not DateTimeOffset horario || horario > ahora)
        {
            throw new InvalidOperationException("El horario programado de la solicitud todavia no llego.");
        }

        Estado = EstadoSolicitudEjecucionWorker.Pendiente;
        Mensaje = "En cola para ser tomada por el Worker.";
        this.MarcarComoModificada();
    }

    public void Cancelar(string canceladaPor, DateTimeOffset fechaCancelacion)
    {
        if (Estado != EstadoSolicitudEjecucionWorker.PendienteDeInicio &&
            Estado != EstadoSolicitudEjecucionWorker.Programada &&
            Estado != EstadoSolicitudEjecucionWorker.Pendiente)
        {
            throw new InvalidOperationException("Solo puede cancelarse una solicitud que el Worker todavia no tomo.");
        }

        Estado = EstadoSolicitudEjecucionWorker.Cancelada;
        CanceladaPor = string.IsNullOrWhiteSpace(canceladaPor) ? "Administracion" : canceladaPor.Trim();
        FechaCancelacion = fechaCancelacion;
        Mensaje = "Cancelada antes de ser tomada por el Worker.";
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
