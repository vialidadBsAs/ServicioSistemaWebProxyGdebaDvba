namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;

/// <summary>
/// Identidad humana del request en curso: el usuario institucional del token DVBA-Auth y su usuario GDEBA de perfil.
/// Los workers y la mensajeria no la establecen, por lo que ambos valores quedan nulos fuera de una consulta interactiva.
/// </summary>
public interface IUsuarioActualAccessor
{
    string? UsuarioInstitucional { get; set; }

    string? UsuarioGdeba { get; set; }
}
