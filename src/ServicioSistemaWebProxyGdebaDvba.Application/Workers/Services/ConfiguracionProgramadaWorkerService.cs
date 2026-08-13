using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

public sealed class ConfiguracionProgramadaWorkerService : IConfiguracionProgramadaWorkerService
{
    private readonly ITrackableRepository<ConfiguracionProgramadaWorker> _configuracionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfiguracionProgramadaWorkerService(ITrackableRepository<ConfiguracionProgramadaWorker> configuracionRepository, IUnitOfWork unitOfWork)
    {
        _configuracionRepository = configuracionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<ConfiguracionProgramadaWorkerDto>> ConsultarAsync(CancellationToken cancellationToken)
    {
        var configuraciones = await _configuracionRepository.Query().OrderBy(x => x.Proceso).SelectAsync(cancellationToken);
        return configuraciones.Select(ConfiguracionProgramadaWorkerService.Mapear).ToArray();
    }

    public async Task<ConfiguracionProgramadaWorkerDto> ObtenerAsync(ProcesoWorker proceso, CancellationToken cancellationToken)
    {
        var configuracion = await _configuracionRepository.Query().FirstOrDefaultAsync(x => x.Proceso == proceso, cancellationToken);
        return configuracion is null
            ? throw new InvalidOperationException($"No existe configuración programada para el Worker '{proceso}'.")
            : ConfiguracionProgramadaWorkerService.Mapear(configuracion);
    }

    public async Task<ConfiguracionProgramadaWorkerDto> GuardarAsync(GuardarConfiguracionProgramadaWorkerRequest request, CancellationToken cancellationToken)
    {
        var configuracion = await _configuracionRepository.Query().FirstOrDefaultAsync(x => x.Proceso == request.Proceso, cancellationToken);
        if (configuracion is null) throw new InvalidOperationException($"No existe configuración programada para el Worker '{request.Proceso}'.");

        configuracion.Actualizar(
            request.Habilitado,
            request.HoraInicioLocal,
            request.HoraFinLocal,
            request.CupoReservaDiaria,
            request.IntervaloMinutos,
            request.EjecutarAlIniciar,
            request.TamanoLote,
            request.ConsultasVaciasParaPausa,
            request.DiasPausaSinResultados,
            request.OmitirConsultasRealizadasEnElDia);
        _configuracionRepository.Update(configuracion);
        _configuracionRepository.ApplyChanges(configuracion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ConfiguracionProgramadaWorkerService.Mapear(configuracion);
    }

    private static ConfiguracionProgramadaWorkerDto Mapear(ConfiguracionProgramadaWorker configuracion)
    {
        return new ConfiguracionProgramadaWorkerDto(
            configuracion.Id,
            configuracion.Proceso,
            configuracion.Habilitado,
            configuracion.HoraInicioLocal,
            configuracion.HoraFinLocal,
            configuracion.CupoReservaDiaria,
            configuracion.IntervaloMinutos,
            configuracion.EjecutarAlIniciar,
            configuracion.TamanoLote,
            configuracion.ConsultasVaciasParaPausa,
            configuracion.DiasPausaSinResultados,
            configuracion.OmitirConsultasRealizadasEnElDia);
    }
}
