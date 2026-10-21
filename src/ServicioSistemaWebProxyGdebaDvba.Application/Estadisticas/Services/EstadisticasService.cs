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
        var filtrosAplicados = this.CrearFiltrosAplicados(request);
        var tratas = await _estadisticasReadStore.ConsultarTotalesExpedientesPorTrataAsync(
            filtro,
            cancellationToken);

        return new EstadisticaExpedientesPorTrataResult(
            DateTimeOffset.Now,
            filtrosAplicados,
            tratas.Sum(x => x.TotalExpedientes),
            tratas);
    }

    // Tope de expedientes por trata en el detalle: evita respuestas enormes en rangos amplios; el resultado informa si se recorto.
    private const int MaximoExpedientesCaratulados = 2000;

    public async Task<EstadisticaExpedientesCaratuladosResult> ObtenerExpedientesCaratuladosAsync(EstadisticaExpedientesCaratuladosRequest request, CancellationToken cancellationToken)
    {
        string codigoTrata = EstadisticasService.Normalizar(request.CodigoTrata) ?? throw new ArgumentException("Debe indicar el codigo de trata.", nameof(request));
        EstadisticaExpedientesCaratuladosFiltro filtro = new(codigoTrata, this.CrearInicioDia(request.FechaDesde), this.CrearInicioDia(request.FechaHasta?.AddDays(1)));
        IReadOnlyCollection<ExpedienteCaratuladoDto> expedientes = await _estadisticasReadStore.ConsultarExpedientesCaratuladosAsync(filtro, EstadisticasService.MaximoExpedientesCaratulados + 1, cancellationToken);
        bool limitado = expedientes.Count > EstadisticasService.MaximoExpedientesCaratulados;

        return new EstadisticaExpedientesCaratuladosResult(DateTimeOffset.Now, codigoTrata, limitado, limitado ? expedientes.Take(EstadisticasService.MaximoExpedientesCaratulados).ToArray() : expedientes);
    }

    private EstadisticaExpedientesPorTrataFiltrosAplicadosDto CrearFiltrosAplicados(
        EstadisticaExpedientesPorTrataRequest request)
    {
        return new EstadisticaExpedientesPorTrataFiltrosAplicadosDto(
            EstadisticasService.Normalizar(request.CodigoTrata),
            request.FechaDesde,
            request.FechaHasta,
            EstadisticasService.Normalizar(request.Estado));
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
