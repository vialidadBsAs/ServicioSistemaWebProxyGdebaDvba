using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Contracts;

public interface ITemaExpedienteAdminService
{
    Task<IReadOnlyCollection<TemaExpedienteDto>> ObtenerTemasAsync(string usuarioPropietario, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TrataHabilitadaVialidadDto>> ObtenerTratasHabilitadasAsync(CancellationToken cancellationToken);

    Task<TemaExpedienteDto> CrearTemaAsync(GuardarTemaExpedienteRequest request, string usuarioPropietario, CancellationToken cancellationToken);

    Task<TemaExpedienteDto> ActualizarTemaAsync(Guid temaId, GuardarTemaExpedienteRequest request, string usuarioPropietario, CancellationToken cancellationToken);

    Task EliminarTemaAsync(Guid temaId, string usuarioPropietario, CancellationToken cancellationToken);
}
