using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Contracts;

public interface ITemaExpedienteAdminService
{
    Task<IReadOnlyCollection<TemaExpedienteDto>> ObtenerTemasAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TrataHabilitadaVialidadDto>> ObtenerTratasHabilitadasAsync(CancellationToken cancellationToken);

    Task<TemaExpedienteDto> CrearTemaAsync(GuardarTemaExpedienteRequest request, CancellationToken cancellationToken);

    Task<TemaExpedienteDto> ActualizarTemaAsync(Guid temaId, GuardarTemaExpedienteRequest request, CancellationToken cancellationToken);

    Task EliminarTemaAsync(Guid temaId, CancellationToken cancellationToken);
}
