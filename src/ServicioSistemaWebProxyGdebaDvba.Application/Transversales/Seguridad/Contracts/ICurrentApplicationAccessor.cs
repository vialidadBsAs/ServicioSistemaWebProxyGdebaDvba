using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;

public interface ICurrentApplicationAccessor
{
    CurrentApplication Current { get; set; }
}
