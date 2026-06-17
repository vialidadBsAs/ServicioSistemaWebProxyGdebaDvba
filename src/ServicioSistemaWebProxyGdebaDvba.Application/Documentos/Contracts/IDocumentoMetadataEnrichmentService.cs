using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;

public interface IDocumentoMetadataEnrichmentService
{
    Task<DocumentoMetadataEnrichmentItemResult> EnriquecerDocumentoAsync(Guid documentoId, OrigenInvocacionGdeba origenInvocacion, CancellationToken cancellationToken);

    Task<DocumentoMetadataEnrichmentResult> EnriquecerPendientesAsync(int loteMaximo, OrigenInvocacionGdeba origenInvocacion, CancellationToken cancellationToken);
}
