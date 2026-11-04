using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;

/// <summary>
/// Operacion administrativa que completa con <c>consultarTipoDocumento</c> las filas del catalogo que nacieron de la semilla GEDO
/// sin sigla (<c>CodigoTipoDocumentoGdeba</c>). Se corre por tandas: cada llamada procesa como maximo una cantidad de tipos y
/// confirma sus cambios una sola vez, asi una corrida larga no depende de un unico pedido HTTP ni pierde lo ya consultado.
/// </summary>
public interface ICompletadoCatalogoTiposDocumentoService
{
    Task<CompletadoCatalogoTiposDocumentoDto> CompletarAsync(int maximoTipos, CancellationToken cancellationToken);
}
