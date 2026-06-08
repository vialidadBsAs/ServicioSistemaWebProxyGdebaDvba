using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Messaging;

public interface IExpedienteDetalleCacheDispatcher
{
    Task PublicarCacheDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken);
}
