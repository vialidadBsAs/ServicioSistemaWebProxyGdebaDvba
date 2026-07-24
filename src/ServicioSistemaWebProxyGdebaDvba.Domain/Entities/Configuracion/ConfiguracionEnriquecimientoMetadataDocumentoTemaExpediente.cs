using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

public sealed class ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente : DomainEntity
{
    private ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente()
    {
    }

    public ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente(Guid temaExpedienteId, bool habilitado, int prioridad)
    {
        TemaExpedienteId = temaExpedienteId == Guid.Empty ? throw new ArgumentException("El tema es requerido.", nameof(temaExpedienteId)) : temaExpedienteId;
        Habilitado = habilitado;
        Prioridad = prioridad;
    }

    public Guid TemaExpedienteId { get; private set; }

    public TemaExpediente TemaExpediente { get; private set; } = null!;

    public bool Habilitado { get; private set; }

    public int Prioridad { get; private set; }
}
