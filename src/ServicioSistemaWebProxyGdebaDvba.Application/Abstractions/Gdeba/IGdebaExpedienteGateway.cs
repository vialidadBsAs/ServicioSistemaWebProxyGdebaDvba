using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public interface IGdebaExpedienteGateway
{
    Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(NumeroGdebaCompleto numeroGdebaCompleto, CancellationToken cancellationToken);

    Task<GdebaExpedienteDetalladoDto?> ConsultarExpedienteDetalladoAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        CancellationToken cancellationToken);

    Task<GdebaHistorialExpedienteDto?> BuscarHistorialPasesExpedienteAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        CancellationToken cancellationToken);
}
