using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Services;

public sealed class EstadisticasService : IEstadisticasService
{
    private readonly IEstadisticasReadStore _estadisticasReadStore;

    public EstadisticasService(IEstadisticasReadStore estadisticasReadStore)
    {
        _estadisticasReadStore = estadisticasReadStore;
    }

    public async Task<EstadisticaExpedientesPorTrataResult> ObtenerTotalesExpedientesPorTrataAsync(
        EstadisticaExpedientesPorTrataRequest request,
        CancellationToken cancellationToken)
    {
        var filtro = this.CrearFiltro(request);
        var tratas = await _estadisticasReadStore.ConsultarTotalesExpedientesPorTrataAsync(
            filtro,
            cancellationToken);

        return new EstadisticaExpedientesPorTrataResult(
            DateTimeOffset.Now,
            tratas.Sum(x => x.TotalExpedientes),
            tratas);
    }

    private EstadisticaExpedientesPorTrataFiltro CrearFiltro(
        EstadisticaExpedientesPorTrataRequest request)
    {
        return new EstadisticaExpedientesPorTrataFiltro(
            EstadisticasService.Normalizar(request.CodigoTrata),
            this.CrearInicioDia(request.FechaDesde),
            this.CrearInicioDia(request.FechaHasta?.AddDays(1)),
            EstadisticasService.Normalizar(request.Estado));
    }

    private DateTimeOffset? CrearInicioDia(DateOnly? fecha)
    {
        return fecha.HasValue
            ? new DateTimeOffset(
                fecha.Value.ToDateTime(TimeOnly.MinValue),
                DateTimeOffset.Now.Offset)
            : null;
    }

    private static string? Normalizar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
