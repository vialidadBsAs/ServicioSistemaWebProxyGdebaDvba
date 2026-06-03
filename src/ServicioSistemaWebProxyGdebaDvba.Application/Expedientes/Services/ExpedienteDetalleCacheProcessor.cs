using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

public sealed class ExpedienteDetalleCacheProcessor : IExpedienteDetalleCacheProcessor
{
    private readonly IExpedienteService _expedienteService;

    public ExpedienteDetalleCacheProcessor(IExpedienteService expedienteService)
    {
        _expedienteService = expedienteService;
    }

    /// <summary>
    /// Delega el procesamiento de cache de detalle al servicio de aplicacion de expedientes.
    /// </summary>
    /// <param name="detalle">Respuesta detallada normalizada recibida desde GDEBA.</param>
    /// <param name="fechaConsulta">Fecha en que se obtuvo la respuesta desde GDEBA.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Tarea asincronica de procesamiento de cache.</returns>
    public Task ProcesarDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken)
    {
        return _expedienteService.ProcesarCacheDetalleAsync(
            detalle,
            fechaConsulta,
            cancellationToken);
    }
}
