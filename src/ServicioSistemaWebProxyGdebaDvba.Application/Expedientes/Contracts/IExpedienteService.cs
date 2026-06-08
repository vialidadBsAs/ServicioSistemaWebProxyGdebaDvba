using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

public interface IExpedienteService
{
    Task<ConsultarExpedienteDetalladoResult> ConsultarDetalleAsync(
        ConsultarExpedienteDetalladoRequest request,
        CancellationToken cancellationToken);

    Task<ConsultarMovimientosExpedienteResult> ConsultarMovimientosAsync(
        ConsultarMovimientosExpedienteRequest request,
        CancellationToken cancellationToken);

    Task ProcesarCacheDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken);

    Task<ConsultarExpedienteSinCacheResult> ConsultarExpedienteSinCacheAsync(
        ConsultarExpedienteSinCacheRequest request,
        CancellationToken cancellationToken);
}
