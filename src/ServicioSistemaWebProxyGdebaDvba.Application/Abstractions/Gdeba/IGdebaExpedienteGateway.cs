using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public interface IGdebaExpedienteGateway
{
    Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(NumeroExpediente numero, CancellationToken cancellationToken);
}
