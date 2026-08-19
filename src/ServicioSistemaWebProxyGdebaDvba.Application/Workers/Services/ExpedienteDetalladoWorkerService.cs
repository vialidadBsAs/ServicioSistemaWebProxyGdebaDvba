using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

public sealed class ExpedienteDetalladoWorkerService : IExpedienteDetalladoWorkerService
{
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IExpedienteService _expedienteService;
    private readonly ILogger<ExpedienteDetalladoWorkerService> _logger;

    public ExpedienteDetalladoWorkerService(IRepository<Expediente> expedienteRepository, IExpedienteService expedienteService, ILogger<ExpedienteDetalladoWorkerService> logger)
    {
        _expedienteRepository = expedienteRepository;
        _expedienteService = expedienteService;
        _logger = logger;
    }

    public async Task<DetallarExpedientesPendientesResult> DetallarPendientesAsync(int tamanoLote, OrigenInvocacionGdeba origen, CancellationToken cancellationToken)
    {
        string[] numerosPendientes = (await _expedienteRepository.Query()
            .Where(x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null)
            .OrderBy(x => x.CacheControl == null ? DateTimeOffset.MaxValue : x.CacheControl.FechaPrimeraDeteccion)
            .Take(Math.Max(1, tamanoLote))
            .SelectAsync(cancellationToken))
            .Select(x => x.GdebaNumeroCompleto)
            .ToArray();

        int detallados = 0;
        int errores = 0;
        foreach (string numeroExpediente in numerosPendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                ObtenerExpedienteRecursoResult<ExpedienteCompletoDto> resultado = await _expedienteService.ObtenerCompletoAsync(
                    new ObtenerExpedienteRecursoRequest(numeroExpediente, ForceRefresh: false, Origen: origen), cancellationToken);
                if (resultado.Exitoso)
                {
                    detallados++;
                }
                else
                {
                    errores++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                errores++;
                _logger.LogError(exception, "Fallo el detalle programado del expediente {NumeroExpediente}.", numeroExpediente);
            }
        }

        int pendientesRestantes = await _expedienteRepository.Query()
            .Where(x => x.HistorialCacheControl == null || x.HistorialCacheControl.FechaUltimaConsultaGdeba == null)
            .CountAsync(cancellationToken);
        return new DetallarExpedientesPendientesResult(numerosPendientes.Length, detallados, errores, pendientesRestantes);
    }
}
