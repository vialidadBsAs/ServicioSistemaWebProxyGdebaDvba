using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public interface IGdebaTipoDocumentoGateway
{
    Task<GdebaTipoDocumentoDto?> ConsultarTipoDocumentoAsync(string acronimo, ContextoInvocacionGdeba contextoInvocacion, CancellationToken cancellationToken);
}
