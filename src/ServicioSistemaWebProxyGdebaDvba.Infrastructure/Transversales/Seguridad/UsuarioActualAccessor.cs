using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Seguridad;

public sealed class UsuarioActualAccessor : IUsuarioActualAccessor
{
    public string? UsuarioInstitucional { get; set; }

    public string? UsuarioGdeba { get; set; }
}
