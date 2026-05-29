using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class FakeGdebaExpedienteGateway : IGdebaExpedienteGateway
{
    public Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(NumeroExpediente numero, CancellationToken cancellationToken)
    {
        var expediente = new ExpedienteGdebaDto(
            numero.Value,
            CodigoTrata: "FIN0057",
            DescripcionTrata: "Elevacion de Consultas",
            Estado: "Tramitacion");

        return Task.FromResult<ExpedienteGdebaDto?>(expediente);
    }
}
