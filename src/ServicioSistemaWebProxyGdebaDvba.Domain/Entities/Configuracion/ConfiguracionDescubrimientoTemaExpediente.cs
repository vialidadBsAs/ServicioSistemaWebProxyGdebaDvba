using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

public sealed class ConfiguracionDescubrimientoTemaExpediente : DomainEntity
{
    private ConfiguracionDescubrimientoTemaExpediente()
    {
    }

    public ConfiguracionDescubrimientoTemaExpediente(Guid temaExpedienteId, bool habilitado, int prioridad)
    {
        TemaExpedienteId = temaExpedienteId == Guid.Empty ? throw new ArgumentException("El tema es requerido.", nameof(temaExpedienteId)) : temaExpedienteId;
        Habilitado = habilitado;
        Prioridad = prioridad;
    }

    public Guid TemaExpedienteId { get; private set; }

    public TemaExpediente TemaExpediente { get; private set; } = null!;

    public bool Habilitado { get; private set; }

    public int Prioridad { get; private set; }

    public void Actualizar(bool habilitado, int prioridad)
    {
        Habilitado = habilitado;
        Prioridad = prioridad;
        this.MarcarComoModificada();
    }
}
