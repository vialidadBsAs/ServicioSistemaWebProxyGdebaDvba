using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;

public interface IExpedienteCacheReadStore
{
    Expediente? BuscarExpedienteParaDetalle(string numeroGdebaCompleto);

    TrataGdeba? BuscarTrataPorCodigo(string codigo);

    IReadOnlyDictionary<string, DocumentoGdeba> BuscarDocumentosPorNumeroActuacion(
        IEnumerable<string> numerosActuacionCompletos);
}
