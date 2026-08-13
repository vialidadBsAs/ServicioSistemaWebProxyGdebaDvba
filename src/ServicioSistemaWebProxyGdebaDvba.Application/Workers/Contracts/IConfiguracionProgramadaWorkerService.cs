using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IConfiguracionProgramadaWorkerService
{
    Task<IReadOnlyCollection<ConfiguracionProgramadaWorkerDto>> ConsultarAsync(CancellationToken cancellationToken);

    Task<ConfiguracionProgramadaWorkerDto> ObtenerAsync(ProcesoWorker proceso, CancellationToken cancellationToken);

    Task<ConfiguracionProgramadaWorkerDto> GuardarAsync(GuardarConfiguracionProgramadaWorkerRequest request, CancellationToken cancellationToken);
}
