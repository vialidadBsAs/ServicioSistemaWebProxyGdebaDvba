using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Services;

internal sealed class RegistroInvocacionesGdeba : IRegistroInvocacionesGdeba
{
    private readonly IRepository<OperacionGdeba> _operacionRepository;
    private readonly IRepository<InvocacionGdeba> _invocacionRepository;
    private readonly IGdebaExecutionContext _executionContext;
    private readonly ILogger<RegistroInvocacionesGdeba> _logger;

    public RegistroInvocacionesGdeba(
        IRepository<OperacionGdeba> operacionRepository,
        IRepository<InvocacionGdeba> invocacionRepository,
        IGdebaExecutionContext executionContext,
        ILogger<RegistroInvocacionesGdeba> logger)
    {
        _operacionRepository = operacionRepository;
        _invocacionRepository = invocacionRepository;
        _executionContext = executionContext;
        _logger = logger;
    }

    public async Task AgregarInvocacionAsync(
        string servicio,
        string metodo,
        ContextoInvocacionGdeba contextoInvocacion,
        bool servidorRespondio,
        bool exitosa,
        int? estadoHttp,
        long? duracionMilisegundos,
        CancellationToken cancellationToken)
    {
        try
        {
            var operacion = await _operacionRepository.Query()
                .FirstOrDefaultAsync(
                    x => x.Servicio == servicio && x.Metodo == metodo,
                    cancellationToken);

            if (operacion is null)
            {
                _logger.LogWarning(
                    "No existe configuracion de cuota para la operacion GDEBA {Servicio}.{Metodo}.",
                    servicio,
                    metodo);
                return;
            }

            _invocacionRepository.Insert(
                new InvocacionGdeba(
                    operacion.Id,
                    _executionContext.Ambiente,
                    DateTimeOffset.Now,
                    contextoInvocacion.Origen,
                    contextoInvocacion.SolicitudId,
                    contextoInvocacion.NumeroIntento,
                    servidorRespondio,
                    exitosa,
                    estadoHttp,
                    duracionMilisegundos));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo preparar el registro de consumo GDEBA para {Servicio}.{Metodo}.",
                servicio,
                metodo);
        }
    }
}
