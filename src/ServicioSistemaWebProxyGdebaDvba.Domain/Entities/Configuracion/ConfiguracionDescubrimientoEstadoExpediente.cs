using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

public sealed class ConfiguracionDescubrimientoEstadoExpediente : DomainEntity
{
    private ConfiguracionDescubrimientoEstadoExpediente()
    {
    }

    public ConfiguracionDescubrimientoEstadoExpediente(Guid estadoExpedienteGdebaId, bool habilitado, int prioridad)
    {
        if (estadoExpedienteGdebaId == Guid.Empty)
        {
            throw new ArgumentException("El estado GDEBA es requerido.", nameof(estadoExpedienteGdebaId));
        }

        EstadoExpedienteGdebaId = estadoExpedienteGdebaId;
        Habilitado = habilitado;
        Prioridad = prioridad;
    }

    public Guid EstadoExpedienteGdebaId { get; private set; }

    public EstadoExpedienteGdeba EstadoExpedienteGdeba { get; private set; } = null!;

    public bool Habilitado { get; private set; }

    public int Prioridad { get; private set; }
}
