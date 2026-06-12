using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;

public interface IExpedienteCacheReadStore
{
    Task<Expediente?> CargarExpedienteAsync(
        string numeroGdebaCompleto,
        CancellationToken cancellationToken);

    Task<TrataGdeba?> BuscarTrataPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, DocumentoGdeba>> BuscarDocumentosPorNumeroActuacionAsync(
        IEnumerable<string> numerosActuacionCompletos,
        CancellationToken cancellationToken);
}
