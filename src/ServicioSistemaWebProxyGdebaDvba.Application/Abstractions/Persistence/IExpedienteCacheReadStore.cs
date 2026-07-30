using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;

public interface IExpedienteCacheReadStore
{
    Task<Expediente?> CargarExpedienteAsync(
        string numeroGdebaCompleto,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, Expediente>> CargarExpedientesPorNumeroAsync(
        IEnumerable<string> numerosGdebaCompletos,
        CancellationToken cancellationToken);

    Task<IReadOnlySet<string>> CargarCodigosReparticionHabilitadosAsync(
        CancellationToken cancellationToken);

    Task<TrataHabilitadaVialidad?> BuscarTrataPorCodigoAsync(
        string codigo,
        string? codigoReparticion,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> CargarDescripcionesTrataAsync(
        IEnumerable<string> codigosTrata,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, DocumentoGdeba>> BuscarDocumentosPorNumeroActuacionAsync(
        IEnumerable<string> numerosActuacionCompletos,
        CancellationToken cancellationToken);
}
