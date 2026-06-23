using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public interface IGdebaExpedienteGateway
{
    Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken);

    Task<GdebaExpedienteDetalladoDto?> ConsultarExpedienteDetalladoAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken);

    Task<GdebaHistorialExpedienteDto?> BuscarHistorialPasesExpedienteAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<GdebaExpedientePorTrataDto>> BuscarDatosExpedientePorCodigosTrataAsync(
        string codigoTrata,
        string estadoDestino,
        string? usuario,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken);
}
