using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Security;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Security;

public sealed class CurrentApplicationAccessor : ICurrentApplicationAccessor
{
    private static readonly CurrentApplication Anonymous = new("unknown", "Aplicacion no identificada");

    public CurrentApplication Current { get; set; } = Anonymous;
}
