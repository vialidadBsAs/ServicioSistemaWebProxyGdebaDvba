using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Security;
using ServicioSistemaWebProxyGdebaDvba.Application.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

public sealed class ConsultarExpedienteService : IConsultarExpedienteService
{
    private readonly IGdebaExpedienteGateway _gdebaExpedienteGateway;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;

    public ConsultarExpedienteService(
        IGdebaExpedienteGateway gdebaExpedienteGateway,
        IGdebaExecutionContext gdebaExecutionContext,
        IAuditoriaService auditoriaService,
        ICurrentApplicationAccessor currentApplicationAccessor)
    {
        _gdebaExpedienteGateway = gdebaExpedienteGateway;
        _gdebaExecutionContext = gdebaExecutionContext;
        _auditoriaService = auditoriaService;
        _currentApplicationAccessor = currentApplicationAccessor;
    }

    public async Task<ConsultarExpedienteResult> ConsultarAsync(ConsultarExpedienteRequest request, CancellationToken cancellationToken)
    {
        var numeroGdebaCompleto = NumeroGdebaCompleto.Create(request.NumeroGdebaCompleto);
        var expediente = await _gdebaExpedienteGateway.BuscarExpedienteAsync(numeroGdebaCompleto, cancellationToken);
        var resolvedAt = DateTimeOffset.UtcNow;

        await _auditoriaService.RegistrarAsync(
            new RegistrarAuditoriaRequest(
                _currentApplicationAccessor.Current.ApplicationId,
                "ConsultarExpediente",
                numeroGdebaCompleto.Valor,
                _gdebaExecutionContext.Ambiente,
                FuenteRespuesta.Gdeba,
                expediente is not null,
                resolvedAt),
            cancellationToken);

        return new ConsultarExpedienteResult(expediente, FuenteRespuesta.Gdeba, resolvedAt, CachedAt: null);
    }
}
