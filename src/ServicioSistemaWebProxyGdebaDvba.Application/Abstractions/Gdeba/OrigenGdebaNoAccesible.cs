namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

/// <summary>
/// En que paso se corto el acceso a GDEBA. Para el usuario el efecto es el mismo (se trabaja con la copia local),
/// pero el aviso de "sin conexion" lo informa para saber a quien reclamar.
/// </summary>
public enum OrigenGdebaNoAccesible
{
    /// <summary>El servicio GDEBA: el gateway rechazo el pedido (HTTP 401/403) o no hubo respuesta.</summary>
    ServicioGdeba = 1,

    /// <summary>El servicio de tokens de IOP no entrego el token de acceso: el pedido no llego a enviarse.</summary>
    CredencialesIop = 2
}
