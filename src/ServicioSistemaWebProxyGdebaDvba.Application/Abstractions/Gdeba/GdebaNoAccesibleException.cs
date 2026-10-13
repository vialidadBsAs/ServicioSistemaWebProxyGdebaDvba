using System.Net.Http;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

/// <summary>
/// GDEBA no es accesible desde este servidor: el pedido no llego a ser procesado por GDEBA.
/// Es la unica definicion del criterio; los procesos la usan para cortar en lugar de insistir.
/// </summary>
public sealed class GdebaNoAccesibleException : Exception
{
    public GdebaNoAccesibleException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Solo lo inequivoco: rechazo de acceso del gateway (HTTP 401/403) o ausencia de respuesta (falla de red o timeout de transporte).
    /// Un error devuelto por GDEBA (500, 404, SOAP Fault), aunque tarde ~30 s, es una falla de ese pedido y no de acceso.
    /// La cancelacion explicita debe atenderse antes de clasificar.
    /// </summary>
    public static bool EsNoAccesible(Exception exception)
    {
        return exception switch
        {
            GdebaNoAccesibleException => true,
            HttpRequestException => true,
            OperationCanceledException => true,
            GdebaOperationException { StatusCode: 401 or 403 } => true,
            { InnerException: Exception causa } => GdebaNoAccesibleException.EsNoAccesible(causa),
            _ => false
        };
    }

    /// <summary>Motivo breve para resumenes y logs, sin stack trace.</summary>
    public static string DescribirMotivo(Exception exception)
    {
        return exception switch
        {
            GdebaOperationException { StatusCode: int estadoHttp } => $"el gateway rechazo el pedido con HTTP {estadoHttp}",
            HttpRequestException => "sin respuesta del servidor",
            OperationCanceledException => "sin respuesta del servidor dentro del tiempo de espera",
            GdebaNoAccesibleException => exception.Message,
            { InnerException: Exception causa } => GdebaNoAccesibleException.DescribirMotivo(causa),
            _ => exception.Message
        };
    }
}
