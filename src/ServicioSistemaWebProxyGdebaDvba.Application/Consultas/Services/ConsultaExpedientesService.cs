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
        var criterios = ConsultaExpedientesService.ConstruirCriterios(request.Orden, request.CampoOrden, request.DireccionOrden);
        string? caratula = string.IsNullOrWhiteSpace(request.Caratula) ? null : request.Caratula.Trim();
        // Virtualizacion: skip/take arbitrarios (startIndex/chunkSize). Si no vienen, se pagina por Pagina/TamanioPagina.
        int? skip = request.Skip is int s ? Math.Max(0, s) : null;
        int? take = request.Take is int t ? Math.Clamp(t, 1, 200) : null;
        return await _consultaExpedientesReadStore.ConsultarAsync(new ConsultaExpedientesFiltro(trataIds, pagina, tamanioPagina, DateTimeOffset.Now, criterios, ConsultaExpedientesService.Normalizar(request.CodigosTrata), ConsultaExpedientesService.Normalizar(request.EstadosActuales), ConsultaExpedientesService.Normalizar(request.EstadosDetalle), ConsultaExpedientesService.Normalizar(request.NumerosExpediente), request.FechaUltimoMovimientoDesde, request.FechaUltimoMovimientoHasta, caratula, skip, take), cancellationToken);
    }

    public async Task<ConsultaCoberturaDetalleResult> ConsultarCoberturaDetalleAsync(IReadOnlyCollection<Guid>? trataIds, CancellationToken cancellationToken)
    {
        Guid[] trataIdsValidos = (trataIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        return await _consultaExpedientesReadStore.ConsultarCoberturaDetalleAsync(trataIdsValidos, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ObtenerValoresFiltroCaratulaAsync(ConsultaCaratulaValoresFiltroRequest request, CancellationToken cancellationToken)
    {
        string texto = request.Texto?.Trim() ?? string.Empty;
        if (texto.Length < 3) throw new ArgumentException("Indique al menos 3 caracteres del texto de la caratula.", nameof(request));

        string campo = request.Campo?.Trim() switch { "codigoTrata" or "estadoActual" => request.Campo.Trim(), _ => throw new ArgumentException("El campo de filtro no es válido.", nameof(request)) };
        Guid[] trataIds = (request.TrataIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        return await _consultaExpedientesReadStore.ObtenerValoresFiltroCaratulaAsync(new ConsultaCaratulaValoresFiltroFiltro(texto, campo, trataIds), cancellationToken);
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

    private static readonly string[] CamposOrdenValidos = ["numeroGdebaCompleto", "codigoTrata", "descripcionTrata", "estadoActual", "fechaUltimoMovimiento", "fechaCaratulacion", "ultimoPaseSectorDestino", "ultimoPaseFecha", "estadoDetalle"];

    // Multi-sort: 'Orden' es "campo:dir,campo:dir" en el orden de prioridad; si no viene, cae al single CampoOrden/DireccionOrden.
    private static IReadOnlyList<CriterioOrdenExpediente> ConstruirCriterios(string? orden, string? campoOrdenSingle, string? direccionSingle)
    {
        List<CriterioOrdenExpediente> criterios = new();
        if (!string.IsNullOrWhiteSpace(orden))
        {
            foreach (string parte in orden.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] campoDir = parte.Split(':', StringSplitOptions.TrimEntries);
                string campo = campoDir[0];
                if (!ConsultaExpedientesService.CamposOrdenValidos.Contains(campo)) continue;
                bool desc = campoDir.Length > 1 && string.Equals(campoDir[1], "desc", StringComparison.OrdinalIgnoreCase);
                if (!criterios.Any(x => x.Campo == campo)) criterios.Add(new CriterioOrdenExpediente(campo, desc));
            }
        }

        if (criterios.Count == 0)
        {
            string campo = ConsultaExpedientesService.CamposOrdenValidos.Contains(campoOrdenSingle?.Trim() ?? string.Empty) ? campoOrdenSingle!.Trim() : "fechaUltimoMovimiento";
            bool desc = !string.Equals(direccionSingle, "asc", StringComparison.OrdinalIgnoreCase);
            criterios.Add(new CriterioOrdenExpediente(campo, desc));
        }

        return criterios;
    }
}
