using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class SoapGdebaExpedienteGateway : IGdebaExpedienteGateway
{
    public Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(NumeroGdebaCompleto numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La integracion SOAP real de GDEBA todavia no fue implementada.");
    }

    public Task<GdebaExpedienteDetalladoDto?> ConsultarExpedienteDetalladoAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La integracion SOAP real de GDEBA todavia no fue implementada.");
    }
}
