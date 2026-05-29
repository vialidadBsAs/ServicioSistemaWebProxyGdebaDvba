using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class RegistroAuditoria : Entity
{
    private RegistroAuditoria()
    {
    }

    public RegistroAuditoria(
        string aplicacion,
        string operacion,
        string? recurso,
        AmbienteGdeba ambiente,
        FuenteRespuesta? fuente,
        bool exitoso,
        DateTimeOffset fecha)
    {
        Aplicacion = aplicacion;
        Operacion = operacion;
        Recurso = recurso;
        Ambiente = ambiente;
        Fuente = fuente;
        Exitoso = exitoso;
        Fecha = fecha;
    }

    public string Aplicacion { get; private set; } = string.Empty;

    public string Operacion { get; private set; } = string.Empty;

    public string? Recurso { get; private set; }

    public AmbienteGdeba Ambiente { get; private set; }

    public FuenteRespuesta? Fuente { get; private set; }

    public bool Exitoso { get; private set; }

    public DateTimeOffset Fecha { get; private set; }
}
