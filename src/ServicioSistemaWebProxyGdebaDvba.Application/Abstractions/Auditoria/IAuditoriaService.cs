using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;

public interface IAuditoriaService
{
    Task RegistrarAsync(RegistroAuditoria registro, CancellationToken cancellationToken);
}
