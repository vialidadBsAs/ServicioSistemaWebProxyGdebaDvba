namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;

/// <summary>
/// Sensor de conexion con GDEBA (patron circuit breaker). Estado compartido en el proceso:
/// las consultas leen <see cref="Accesible"/> para decidir GDEBA vs cache sin esperar,
/// y alimentan el estado con el resultado de cada intento real. La reconexion se detecta con
/// una sonda unica en segundo plano (single-flight + freno), no con polling.
/// </summary>
public interface ISensorConexionGdeba
{
    /// <summary>GDEBA se considera accesible. Arranca en true (optimista) hasta el primer fallo.</summary>
    bool Accesible { get; }

    /// <summary>Un intento real a GDEBA respondio: se marca accesible.</summary>
    void RegistrarExito();

    /// <summary>Un intento real a GDEBA fallo por transporte (enlace caido / timeout): se marca caido.</summary>
    void RegistrarFalloConexion();

    /// <summary>
    /// Estando caido, dispara en segundo plano una unica sonda de reconexion para el expediente indicado,
    /// respetando el freno configurado. No bloquea ni espera; a lo sumo corre una sonda a la vez.
    /// </summary>
    void SondearSiCorresponde(string numeroGdebaCompleto);
}
