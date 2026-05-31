using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class RegistroAuditoria : DomainEntity
{
    private RegistroAuditoria()
    {
    }

    public RegistroAuditoria(
        Guid aplicacionConsumidoraId,
        string operacion,
        string? recurso,
        AmbienteGdeba ambiente,
        FuenteRespuesta? fuente,
        bool exitoso,
        DateTimeOffset fecha)
    {
        AplicacionConsumidoraId = aplicacionConsumidoraId == Guid.Empty
            ? throw new ArgumentException("La aplicacion consumidora es requerida.", nameof(aplicacionConsumidoraId))
            : aplicacionConsumidoraId;
        Operacion = operacion;
        Recurso = recurso;
        Ambiente = ambiente;
        Fuente = fuente;
        Exitoso = exitoso;
        Fecha = fecha;
    }

    public Guid AplicacionConsumidoraId { get; private set; }

    public AplicacionConsumidora AplicacionConsumidora { get; private set; } = null!;

    public string Operacion { get; private set; } = string.Empty;

    public string? Recurso { get; private set; }

    public AmbienteGdeba Ambiente { get; private set; }

    public FuenteRespuesta? Fuente { get; private set; }

    public bool Exitoso { get; private set; }

    public DateTimeOffset Fecha { get; private set; }
}
