using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

public sealed class OmisionCorridaProgramadaWorkerService : IOmisionCorridaProgramadaWorkerService
{
    private readonly ITrackableRepository<OmisionCorridaProgramadaWorker> _omisionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OmisionCorridaProgramadaWorkerService(ITrackableRepository<OmisionCorridaProgramadaWorker> omisionRepository, IUnitOfWork unitOfWork)
    {
        _omisionRepository = omisionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OmisionCorridaProgramadaDto> OmitirCorridaDelDiaAsync(ProcesoWorker proceso, string? omitidaPor, CancellationToken cancellationToken)
    {
        if (proceso != ProcesoWorker.DescubrimientoExpedientes)
        {
            throw new InvalidOperationException("Solo la corrida diaria de descubrimiento admite omision; los procesos por intervalo se pausan desde su configuracion programada.");
        }

        DateOnly fechaLocal = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime);
        OmisionCorridaProgramadaWorker? omisionExistente = await _omisionRepository.Query().FirstOrDefaultAsync(x => x.Proceso == proceso && x.FechaLocal == fechaLocal, cancellationToken);
        if (omisionExistente is not null)
        {
            return OmisionCorridaProgramadaWorkerService.Mapear(omisionExistente);
        }

        OmisionCorridaProgramadaWorker omision = new OmisionCorridaProgramadaWorker(proceso, fechaLocal, omitidaPor ?? "Administracion", DateTimeOffset.Now);
        _omisionRepository.Insert(omision);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OmisionCorridaProgramadaWorkerService.Mapear(omision);
    }

    public async Task QuitarOmisionDelDiaAsync(ProcesoWorker proceso, CancellationToken cancellationToken)
    {
        DateOnly fechaLocal = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime);
        OmisionCorridaProgramadaWorker? omision = await _omisionRepository.Query().FirstOrDefaultAsync(x => x.Proceso == proceso && x.FechaLocal == fechaLocal, cancellationToken);
        if (omision is null)
        {
            return;
        }

        _omisionRepository.Delete(omision);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<OmisionCorridaProgramadaDto?> ObtenerOmisionAsync(ProcesoWorker proceso, DateOnly fechaLocal, CancellationToken cancellationToken)
    {
        OmisionCorridaProgramadaWorker? omision = await _omisionRepository.Query().FirstOrDefaultAsync(x => x.Proceso == proceso && x.FechaLocal == fechaLocal, cancellationToken);
        return omision is null ? null : OmisionCorridaProgramadaWorkerService.Mapear(omision);
    }

    private static OmisionCorridaProgramadaDto Mapear(OmisionCorridaProgramadaWorker omision)
    {
        return new OmisionCorridaProgramadaDto(omision.Proceso, omision.FechaLocal, omision.OmitidaPor, omision.FechaRegistro);
    }
}
