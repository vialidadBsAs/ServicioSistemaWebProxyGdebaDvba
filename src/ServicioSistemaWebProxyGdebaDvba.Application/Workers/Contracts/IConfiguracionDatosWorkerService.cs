using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IConfiguracionDatosWorkerService
{
    Task<ConfiguracionDatosWorkerDto> ConsultarAsync(ProcesoWorker proceso, CancellationToken cancellationToken);

    Task<ConfiguracionTemaWorkerDto> GuardarTemaAsync(GuardarConfiguracionTemaWorkerRequest request, CancellationToken cancellationToken);

    Task QuitarTemaAsync(ProcesoWorker proceso, Guid temaExpedienteId, CancellationToken cancellationToken);

    Task<ConfiguracionTrataDescubrimientoWorkerDto> GuardarTrataDescubrimientoAsync(GuardarConfiguracionTrataDescubrimientoWorkerRequest request, CancellationToken cancellationToken);

    Task QuitarTrataDescubrimientoAsync(string codigoTrata, CancellationToken cancellationToken);
}
