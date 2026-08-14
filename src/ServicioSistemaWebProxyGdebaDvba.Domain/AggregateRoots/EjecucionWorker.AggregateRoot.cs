using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed partial class EjecucionWorker : IAggregateRoot
{
    public EjecucionWorkerDescubrimientoTrataEstado RegistrarResultadoDescubrimiento(
        Guid trataHabilitadaVialidadId,
        Guid estadoExpedienteGdebaId,
        DateTimeOffset fechaResolucion,
        int recibidosGdeba,
        int habilitados,
        int descartados,
        int creados,
        int actualizados,
        int sinCambios,
        IEnumerable<Guid> expedientesNuevosIds,
        IEnumerable<Guid> expedientesActualizadosIds)
    {
        if (Proceso != ProcesoWorker.DescubrimientoExpedientes)
        {
            throw new InvalidOperationException("Solo una ejecucion de descubrimiento de expedientes puede registrar resultados por trata y estado.");
        }

        if (_resultadosDescubrimientoTrataEstado.Any(x =>
            x.TrataHabilitadaVialidadId == trataHabilitadaVialidadId &&
            x.EstadoExpedienteGdebaId == estadoExpedienteGdebaId))
        {
            throw new InvalidOperationException("La ejecucion ya contiene un resultado para la trata y el estado indicados.");
        }

        var resultado = new EjecucionWorkerDescubrimientoTrataEstado(
            Id, trataHabilitadaVialidadId, estadoExpedienteGdebaId, fechaResolucion,
            recibidosGdeba, habilitados, descartados, creados, actualizados, sinCambios)
        {
            TrackingState = TrackableEntities.Common.Core.TrackingState.Added
        };
        foreach (Guid expedienteId in expedientesNuevosIds.Distinct())
        {
            resultado.RegistrarExpedienteDescubierto(expedienteId, TipoDeteccionExpedienteDescubierto.Nuevo);
        }

        foreach (Guid expedienteId in expedientesActualizadosIds.Distinct())
        {
            resultado.RegistrarExpedienteDescubierto(expedienteId, TipoDeteccionExpedienteDescubierto.Actualizado);
        }

        _resultadosDescubrimientoTrataEstado.Add(resultado);
        this.MarcarComoModificada();
        return resultado;
    }
}
