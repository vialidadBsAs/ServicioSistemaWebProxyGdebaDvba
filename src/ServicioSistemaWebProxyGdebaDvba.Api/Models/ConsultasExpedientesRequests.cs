using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Models;

// Cuerpos de las consultas de la pantalla de temas y tratas. Viajan por POST con JSON porque la seleccion de tratas es una lista
// (hasta todo el catalogo, 280 ids) que no entra en una query string: Kestrel corta la linea de pedido en 8 KB e IIS la query en 2 KB.
// Son lecturas; POST es solo el transporte del filtro.

public sealed record ConsultarExpedientesPorTratasApiRequest(
    Guid[]? TrataIds,
    int Pagina = 1,
    int TamanioPagina = 50,
    string? CampoOrden = null,
    string? DireccionOrden = null,
    string[]? CodigosTrata = null,
    string[]? EstadosActuales = null,
    string[]? EstadosDetalle = null,
    string[]? NumerosExpediente = null,
    FiltroFecha? FiltroFechaUltimoMovimiento = null,
    string? Caratula = null);

public sealed record BuscarExpedientesPorCaratulaApiRequest(
    string Texto,
    Guid[]? TrataIds = null,
    int Pagina = 1,
    int TamanioPagina = 50,
    string? Orden = null,
    string[]? CodigosTrata = null,
    string[]? EstadosActuales = null,
    int? Skip = null,
    int? Take = null);

public sealed record ValoresFiltroCaratulaApiRequest(string Texto, string Campo, Guid[]? TrataIds = null);

public sealed record SeleccionTratasApiRequest(Guid[]? TrataIds);

public sealed record ConsultarDocumentosPorTratasApiRequest(
    Guid[]? TrataIds,
    int Pagina = 1,
    int TamanioPagina = 50,
    string? CodigoTipoDocumento = null,
    string? CampoOrden = null,
    string? DireccionOrden = null,
    string[]? NumerosExpediente = null,
    string[]? CodigosTrata = null,
    string[]? NumerosActuacion = null,
    string[]? Referencias = null,
    string? ReferenciaContiene = null,
    string[]? TiposDocumento = null,
    FiltroFecha? FiltroFechaCreacion = null,
    bool SoloSinReferencia = false,
    bool IncluirResumen = true,
    bool IncluirTotal = true);
