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
        if (trataIds.Length == 0) throw new ArgumentException("Debe seleccionar al menos una trata.", nameof(request));

        var pagina = Math.Max(request.Pagina, 1);
        var tamanioPagina = Math.Clamp(request.TamanioPagina, 1, 100);
        var campoOrden = request.CampoOrden?.Trim() switch { "numeroGdebaCompleto" or "codigoTrata" or "descripcionTrata" or "estadoActual" or "fechaUltimoMovimiento" or "estadoDetalle" => request.CampoOrden.Trim(), _ => "fechaUltimoMovimiento" };
        var descendente = !string.Equals(request.DireccionOrden, "asc", StringComparison.OrdinalIgnoreCase);
        return await _consultaExpedientesReadStore.ConsultarAsync(new ConsultaExpedientesFiltro(trataIds, pagina, tamanioPagina, DateTimeOffset.Now, campoOrden, descendente, Normalizar(request.CodigosTrata), Normalizar(request.EstadosActuales), Normalizar(request.EstadosDetalle)), cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ObtenerValoresFiltroAsync(ConsultaExpedientesValoresFiltroRequest request, CancellationToken cancellationToken)
    {
        var trataIds = (request.TrataIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        if (trataIds.Length == 0) throw new ArgumentException("Debe seleccionar al menos una trata.", nameof(request));

        var campo = request.Campo?.Trim() switch { "codigoTrata" or "estadoActual" or "estadoDetalle" => request.Campo.Trim(), _ => throw new ArgumentException("El campo de filtro no es válido.", nameof(request)) };
        return await _consultaExpedientesReadStore.ObtenerValoresFiltroAsync(trataIds, campo, DateTimeOffset.Now, cancellationToken);
    }

    private static IReadOnlyCollection<string> Normalizar(IEnumerable<string>? valores) => (valores ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
