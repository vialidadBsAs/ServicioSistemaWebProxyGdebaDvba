using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;

/// <summary>
/// Mantiene el catalogo local <see cref="TipoDocumentoGdeba"/> a partir de lo que informa <c>consultarTipoDocumento</c>.
/// Es la unica pieza que traduce la respuesta de GDEBA a una fila del catalogo; la usan el enriquecimiento documental y las
/// alertas, que son quienes deciden cuando consultar. No guarda: el caso de uso que lo invoca confirma los cambios.
/// </summary>
public interface ICatalogoTiposDocumentoGdebaService
{
    /// <summary>
    /// Crea la fila del catalogo si no existe o completa la existente (sigla, nombre, familia, estado y banderas) con la
    /// respuesta de GDEBA. Conserva <c>EsResolucion</c>, que es una regla local. Devuelve la fila resultante.
    /// </summary>
    TipoDocumentoGdeba IncorporarRespuestaGdeba(TipoDocumentoGdeba? tipoLocal, string codigoConsultado, GdebaTipoDocumentoDto detalle);
}
