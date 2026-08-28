using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;

public interface IDocumentoDetailEnrichmentService
{
    Task<DocumentoDetailEnrichmentItemResult> EnriquecerDocumentoAsync(Guid documentoId, OrigenInvocacionGdeba origenInvocacion, CancellationToken cancellationToken);

    Task<DocumentoDetailEnrichmentItemResult> ObtenerDetalleLocalAsync(Guid documentoId, CancellationToken cancellationToken);

    Task<DocumentoDetailEnrichmentResult> EnriquecerPendientesAsync(int loteMaximo, OrigenInvocacionGdeba origenInvocacion, CancellationToken cancellationToken);
}
