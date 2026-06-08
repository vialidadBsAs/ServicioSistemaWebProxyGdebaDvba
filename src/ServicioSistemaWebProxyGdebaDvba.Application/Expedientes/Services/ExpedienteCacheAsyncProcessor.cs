using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

public sealed class ExpedienteCacheAsyncProcessor : IExpedienteCacheAsyncProcessor
{
    private readonly IExpedienteService _expedienteService;

    public ExpedienteCacheAsyncProcessor(IExpedienteService expedienteService)
    {
        _expedienteService = expedienteService;
    }

    /// <summary>
    /// Ejecuta trabajos asincronicos de cache de expedientes recibidos por mensajeria u otros disparadores.
    /// </summary>
    /// <param name="detalle">Respuesta detallada normalizada recibida desde GDEBA.</param>
    /// <param name="fechaConsulta">Fecha en que se obtuvo la respuesta desde GDEBA.</param>
    /// <param name="cancellationToken">Token de cancelacion de la operacion asincronica.</param>
    /// <returns>Tarea asincronica de procesamiento de cache.</returns>
    public Task CachearDetalleAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken)
    {
        return _expedienteService.ConsolidarDetalleEnCacheAsync(
            detalle,
            fechaConsulta,
            cancellationToken);
    }
}
