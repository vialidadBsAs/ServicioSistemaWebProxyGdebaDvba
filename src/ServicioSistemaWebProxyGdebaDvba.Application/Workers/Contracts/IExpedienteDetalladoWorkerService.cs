using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IExpedienteDetalladoWorkerService
{
    Task<DetallarExpedientesPendientesResult> DetallarPendientesAsync(int tamanoLote, OrigenInvocacionGdeba origen, Func<CancellationToken, Task<bool>>? cancelacionSolicitada, CancellationToken cancellationToken);
}
