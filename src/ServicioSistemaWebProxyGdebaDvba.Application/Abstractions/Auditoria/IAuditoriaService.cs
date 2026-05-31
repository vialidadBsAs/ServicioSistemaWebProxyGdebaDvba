using ServicioSistemaWebProxyGdebaDvba.Application.Auditoria;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;

public interface IAuditoriaService
{
    Task RegistrarAsync(RegistrarAuditoriaRequest request, CancellationToken cancellationToken);
}
