using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.ReadStores;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Services;

public sealed class ConsultaExpedientesService : IConsultaExpedientesService
{
    private readonly IConsultaExpedientesReadStore _consultaExpedientesReadStore;

    public ConsultaExpedientesService(IConsultaExpedientesReadStore consultaExpedientesReadStore)
    {
        _consultaExpedientesReadStore = consultaExpedientesReadStore;
    }

    public async Task<ConsultaExpedientesResult> ConsultarAsync(ConsultaExpedientesRequest request, CancellationToken cancellationToken)
    {
        var trataIds = (request.TrataIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        if (trataIds.Length == 0 && string.IsNullOrWhiteSpace(request.Caratula)) throw new ArgumentException("Debe seleccionar al menos una trata.", nameof(request));

        var pagina = Math.Max(request.Pagina, 1);
        var tamanioPagina = Math.Clamp(request.TamanioPagina, 1, 100);
        var campoOrden = request.CampoOrden?.Trim() switch { "numeroGdebaCompleto" or "codigoTrata" or "descripcionTrata" or "estadoActual" or "fechaUltimoMovimiento" or "estadoDetalle" => request.CampoOrden.Trim(), _ => "fechaUltimoMovimiento" };
        var descendente = !string.Equals(request.DireccionOrden, "asc", StringComparison.OrdinalIgnoreCase);
        string? caratula = string.IsNullOrWhiteSpace(request.Caratula) ? null : request.Caratula.Trim();
        return await _consultaExpedientesReadStore.ConsultarAsync(new ConsultaExpedientesFiltro(trataIds, pagina, tamanioPagina, DateTimeOffset.Now, campoOrden, descendente, ConsultaExpedientesService.Normalizar(request.CodigosTrata), ConsultaExpedientesService.Normalizar(request.EstadosActuales), ConsultaExpedientesService.Normalizar(request.EstadosDetalle), ConsultaExpedientesService.Normalizar(request.NumerosExpediente), request.FechaUltimoMovimientoDesde, request.FechaUltimoMovimientoHasta, caratula), cancellationToken);
    }

    public async Task<ConsultaCoberturaDetalleResult> ConsultarCoberturaDetalleAsync(IReadOnlyCollection<Guid>? trataIds, CancellationToken cancellationToken)
    {
        Guid[] trataIdsValidos = (trataIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        return await _consultaExpedientesReadStore.ConsultarCoberturaDetalleAsync(trataIdsValidos, cancellationToken);
    }

    public async Task<ConsultaDocumentosPorTrataResult> ConsultarDocumentosAsync(ConsultaDocumentosPorTrataRequest request, CancellationToken cancellationToken)
    {
        var trataIds = (request.TrataIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        if (trataIds.Length == 0) throw new ArgumentException("Debe seleccionar al menos una trata.", nameof(request));

        var pagina = Math.Max(request.Pagina, 1);
        var tamanioPagina = Math.Clamp(request.TamanioPagina, 1, 100);
        var codigoTipoDocumento = string.IsNullOrWhiteSpace(request.CodigoTipoDocumento) ? null : request.CodigoTipoDocumento.Trim().ToUpperInvariant();
        // Listar los pendientes de referencia es excluyente con buscar por texto: sin referencia no hay donde buscar.
        var referenciaContiene = request.SoloSinReferencia || string.IsNullOrWhiteSpace(request.ReferenciaContiene) ? null : request.ReferenciaContiene.Trim();
        var campoOrden = request.CampoOrden?.Trim() switch { "numeroExpediente" or "codigoTrata" or "numeroActuacionCompleto" or "fechaCreacion" or "ultimaActividad" or "fechaUltimaActividad" or "referencia" => request.CampoOrden.Trim(), _ => "fechaVinculacion" };
        var descendente = !string.Equals(request.DireccionOrden, "asc", StringComparison.OrdinalIgnoreCase);
        // La fecha hasta llega exclusiva desde los filtros de grilla, la misma convencion que fechaUltimoMovimientoHasta en expedientes.
        return await _consultaExpedientesReadStore.ConsultarDocumentosAsync(new ConsultaDocumentosPorTrataFiltro(trataIds, pagina, tamanioPagina, codigoTipoDocumento, campoOrden, descendente, ConsultaExpedientesService.Normalizar(request.NumerosExpediente), ConsultaExpedientesService.Normalizar(request.CodigosTrata), ConsultaExpedientesService.Normalizar(request.NumerosActuacion), ConsultaExpedientesService.Normalizar(request.Referencias), referenciaContiene, ConsultaExpedientesService.Normalizar(request.TiposDocumento), request.FechaCreacionDesde, request.FechaCreacionHasta, request.SoloSinReferencia, request.IncluirResumen), cancellationToken);
    }

    private static IReadOnlyCollection<string> Normalizar(IEnumerable<string>? valores) => (valores ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
