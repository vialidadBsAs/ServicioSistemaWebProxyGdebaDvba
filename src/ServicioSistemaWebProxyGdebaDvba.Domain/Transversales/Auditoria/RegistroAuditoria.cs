using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class RegistroAuditoria : DomainEntity
{
    private RegistroAuditoria()
    {
    }

    public RegistroAuditoria(Guid aplicacionConsumidoraId, string operacionSolicitada, string operacionGdeba, string? recurso, AmbienteGdeba ambiente, FuenteRespuesta? fuente, bool exitoso, string? mensaje, DateTimeOffset fecha)
    {
        AplicacionConsumidoraId = aplicacionConsumidoraId == Guid.Empty
            ? throw new ArgumentException("La aplicacion consumidora es requerida.", nameof(aplicacionConsumidoraId))
            : aplicacionConsumidoraId;
        OperacionSolicitada = NormalizarOperacion(operacionSolicitada, nameof(operacionSolicitada));
        OperacionGdeba = NormalizarOperacion(operacionGdeba, nameof(operacionGdeba));
        Recurso = recurso;
        Ambiente = ambiente;
        Fuente = fuente;
        Exitoso = exitoso;
        Mensaje = NormalizarMensaje(mensaje);
        Fecha = fecha;
    }

    public Guid AplicacionConsumidoraId { get; private set; }

    public AplicacionConsumidora AplicacionConsumidora { get; private set; } = null!;

    public string OperacionSolicitada { get; private set; } = string.Empty;

    public string OperacionGdeba { get; private set; } = string.Empty;

    public string? Recurso { get; private set; }

    public AmbienteGdeba Ambiente { get; private set; }

    public FuenteRespuesta? Fuente { get; private set; }

    public bool Exitoso { get; private set; }

    public string? Mensaje { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    private static string? NormalizarMensaje(string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return null;
        }

        var mensajeNormalizado = mensaje.Trim();
        return mensajeNormalizado.Length <= 1000 ? mensajeNormalizado : mensajeNormalizado[..1000];
    }

    private static string NormalizarOperacion(string operacion, string parameterName)
    {
        return string.IsNullOrWhiteSpace(operacion) ? throw new ArgumentException("La operacion es requerida.", parameterName) : operacion.Trim();
    }
}
