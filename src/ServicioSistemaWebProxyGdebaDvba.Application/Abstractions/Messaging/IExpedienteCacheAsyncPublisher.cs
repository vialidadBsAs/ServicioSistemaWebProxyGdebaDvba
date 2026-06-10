using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Messaging;

public interface IExpedienteCacheAsyncPublisher
{
    Task<bool> SolicitarCacheDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken);
}
