using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.ReadStores;

public interface IConsultaExpedientesReadStore
{
    Task<ConsultaExpedientesResult> ConsultarAsync(ConsultaExpedientesFiltro filtro, CancellationToken cancellationToken);
    Task<ConsultaDocumentosPorTrataResult> ConsultarDocumentosAsync(ConsultaDocumentosPorTrataFiltro filtro, CancellationToken cancellationToken);

    Task<ConsultaCoberturaDetalleResult> ConsultarCoberturaDetalleAsync(IReadOnlyCollection<Guid> trataIds, CancellationToken cancellationToken);
}
