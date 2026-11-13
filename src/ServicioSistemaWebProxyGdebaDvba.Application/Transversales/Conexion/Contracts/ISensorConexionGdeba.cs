using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

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

    /// <summary>Paso en el que se corto el acceso mientras no es accesible; null cuando lo es.</summary>
    OrigenGdebaNoAccesible? OrigenFalla { get; }

    /// <summary>Un intento real no llego a GDEBA (sin token de IOP, rechazo del gateway o sin respuesta): se marca caido con su origen.</summary>
    void RegistrarFalloConexion(OrigenGdebaNoAccesible origen);

    /// <summary>
    /// Estando caido, dispara en segundo plano una unica sonda de reconexion para el expediente indicado,
    /// respetando el freno configurado. No bloquea ni espera; a lo sumo corre una sonda a la vez.
    /// </summary>
    void SondearSiCorresponde(string numeroGdebaCompleto);
}
