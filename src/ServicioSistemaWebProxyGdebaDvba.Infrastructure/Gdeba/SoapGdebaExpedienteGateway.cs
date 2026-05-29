using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class SoapGdebaExpedienteGateway : IGdebaExpedienteGateway
{
    public Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(NumeroExpediente numero, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La integracion SOAP real de GDEBA todavia no fue implementada.");
    }
}
