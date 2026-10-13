using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

namespace ServicioSistemaWebProxyGdebaDvba.Worker;

/// <summary>
/// Cierre de una corrida que se corto porque GDEBA no es accesible desde este servidor.
/// No hay reintentos fuera de agenda: la proxima corrida programada vuelve a intentar en su horario.
/// </summary>
public static class CorteGdebaNoAccesible
{
    /// <summary>Registra la advertencia (sin stack trace) y devuelve el resumen de la ejecucion.</summary>
    public static string Registrar(ILogger logger, Exception causa, string corrida)
    {
        string motivo = GdebaNoAccesibleException.DescribirMotivo(causa);
        logger.LogWarning("GDEBA no accesible desde este servidor ({Motivo}). Se interrumpe {Corrida}.", motivo, corrida);
        return $"GDEBA no accesible desde este servidor: {motivo}. Se interrumpió la corrida; se vuelve a intentar en la próxima corrida programada.";
    }
}
