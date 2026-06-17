using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;

public interface IAuditoriaService
{
    Task RegistrarAsync(RegistrarAuditoriaRequest request, CancellationToken cancellationToken);
}
