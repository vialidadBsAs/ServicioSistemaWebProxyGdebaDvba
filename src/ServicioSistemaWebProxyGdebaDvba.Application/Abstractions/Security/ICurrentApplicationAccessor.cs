namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Security;

public interface ICurrentApplicationAccessor
{
    CurrentApplication Current { get; set; }
}
