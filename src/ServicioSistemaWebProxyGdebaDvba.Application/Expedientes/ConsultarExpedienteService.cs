using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Auditoria;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Security;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;

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
        var numero = NumeroExpediente.Create(request.NumeroExpediente);
        var expediente = await _gdebaExpedienteGateway.BuscarExpedienteAsync(numero, cancellationToken);
        var resolvedAt = DateTimeOffset.UtcNow;

        await _auditoriaService.RegistrarAsync(
            new RegistroAuditoria(
                _currentApplicationAccessor.Current.ApplicationId,
                "ConsultarExpediente",
                numero.Value,
                _gdebaExecutionContext.Ambiente,
                FuenteRespuesta.Gdeba,
                expediente is not null,
                resolvedAt),
            cancellationToken);

        return new ConsultarExpedienteResult(expediente, FuenteRespuesta.Gdeba, resolvedAt, CachedAt: null);
    }
}
