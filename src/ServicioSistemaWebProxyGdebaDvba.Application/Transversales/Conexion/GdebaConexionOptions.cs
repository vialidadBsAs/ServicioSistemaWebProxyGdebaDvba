namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion;

/// <summary>
/// Configuracion del sensor de conexion con GDEBA (circuit breaker).
/// </summary>
public sealed class GdebaConexionOptions
{
    public const string SectionName = "GdebaConexion";

    /// <summary>
    /// Segundos minimos entre sondas de reconexion mientras GDEBA esta marcado como caido.
    /// </summary>
    public int SegundosReintentoSonda { get; set; } = 30;
}
