using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

public interface IExpedienteDetalleCacheProcessor
{
    Task ProcesarDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken);
}
