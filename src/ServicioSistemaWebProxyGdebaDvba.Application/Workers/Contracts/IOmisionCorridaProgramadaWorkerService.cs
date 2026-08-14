using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

public interface IOmisionCorridaProgramadaWorkerService
{
    Task<OmisionCorridaProgramadaDto> OmitirCorridaDelDiaAsync(ProcesoWorker proceso, string? omitidaPor, CancellationToken cancellationToken);

    Task QuitarOmisionDelDiaAsync(ProcesoWorker proceso, CancellationToken cancellationToken);

    Task<OmisionCorridaProgramadaDto?> ObtenerOmisionAsync(ProcesoWorker proceso, DateOnly fechaLocal, CancellationToken cancellationToken);
}
