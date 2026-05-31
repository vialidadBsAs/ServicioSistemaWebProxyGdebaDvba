using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class FakeGdebaExpedienteGateway : IGdebaExpedienteGateway
{
    public Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(NumeroGdebaCompleto numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        var expediente = new ExpedienteGdebaDto(
            numeroGdebaCompleto.Valor,
            CodigoTrata: "FIN0057",
            DescripcionTrata: "Elevacion de Consultas",
            Estado: "Tramitacion");

        return Task.FromResult<ExpedienteGdebaDto?>(expediente);
    }
}
