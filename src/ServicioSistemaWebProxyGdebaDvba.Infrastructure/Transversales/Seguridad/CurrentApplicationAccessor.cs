using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Seguridad;

public sealed class CurrentApplicationAccessor : ICurrentApplicationAccessor
{
    private static readonly CurrentApplication Anonymous = new("unknown", "Aplicacion no identificada");

    public CurrentApplication Current { get; set; } = Anonymous;
}
