using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed class EjecucionWorkerDescubrimientoTrataEstado : DomainEntity
{
    private readonly List<EjecucionWorkerExpedienteDescubierto> _expedientesDescubiertos = [];

    private EjecucionWorkerDescubrimientoTrataEstado()
    {
    }

    internal EjecucionWorkerDescubrimientoTrataEstado(
        Guid ejecucionWorkerId,
        Guid trataHabilitadaVialidadId,
        Guid estadoExpedienteGdebaId,
        DateTimeOffset fechaResolucion,
        int recibidosGdeba,
        int habilitados,
        int descartados,
        int creados,
        int actualizados,
        int sinCambios)
    {
        EjecucionWorkerId = ejecucionWorkerId;
        TrataHabilitadaVialidadId = trataHabilitadaVialidadId;
        EstadoExpedienteGdebaId = estadoExpedienteGdebaId;
        FechaResolucion = fechaResolucion;
        RecibidosGdeba = recibidosGdeba;
        Habilitados = habilitados;
        Descartados = descartados;
        Creados = creados;
        Actualizados = actualizados;
        SinCambios = sinCambios;
    }

    public Guid EjecucionWorkerId { get; private set; }
    public EjecucionWorker EjecucionWorker { get; private set; } = null!;
    public Guid TrataHabilitadaVialidadId { get; private set; }
    public TrataHabilitadaVialidad TrataHabilitadaVialidad { get; private set; } = null!;
    public Guid EstadoExpedienteGdebaId { get; private set; }
    public EstadoExpedienteGdeba EstadoExpedienteGdeba { get; private set; } = null!;
    public DateTimeOffset FechaResolucion { get; private set; }
    public int RecibidosGdeba { get; private set; }
    public int Habilitados { get; private set; }
    public int Descartados { get; private set; }
    public int Creados { get; private set; }
    public int Actualizados { get; private set; }
    public int SinCambios { get; private set; }
    public IReadOnlyCollection<EjecucionWorkerExpedienteDescubierto> ExpedientesDescubiertos => _expedientesDescubiertos;

    internal void RegistrarExpedienteDescubierto(Guid expedienteId)
    {
        if (_expedientesDescubiertos.Any(x => x.ExpedienteId == expedienteId))
        {
            return;
        }

        var expedienteDescubierto = new EjecucionWorkerExpedienteDescubierto(Id, expedienteId)
        {
            TrackingState = TrackableEntities.Common.Core.TrackingState.Added
        };
        _expedientesDescubiertos.Add(expedienteDescubierto);
    }
}
