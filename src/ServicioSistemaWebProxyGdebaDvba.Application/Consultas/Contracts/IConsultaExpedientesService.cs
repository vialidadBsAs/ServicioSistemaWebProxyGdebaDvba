using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Contracts;

public interface IConsultaExpedientesService
{
    Task<ConsultaExpedientesResult> ConsultarAsync(ConsultaExpedientesRequest request, CancellationToken cancellationToken);
    Task<ConsultaDocumentosPorTrataResult> ConsultarDocumentosAsync(ConsultaDocumentosPorTrataRequest request, CancellationToken cancellationToken);

    Task<ConsultaCoberturaDetalleResult> ConsultarCoberturaDetalleAsync(IReadOnlyCollection<Guid>? trataIds, CancellationToken cancellationToken);
}
