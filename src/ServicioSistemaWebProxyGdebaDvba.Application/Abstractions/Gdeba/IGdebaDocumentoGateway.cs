using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public interface IGdebaDocumentoGateway
{
    Task<GdebaDocumentoDetalleDto?> BuscarDetallePorNumeroAsync(string numeroDocumento, ContextoInvocacionGdeba contextoInvocacion, CancellationToken cancellationToken);
}
