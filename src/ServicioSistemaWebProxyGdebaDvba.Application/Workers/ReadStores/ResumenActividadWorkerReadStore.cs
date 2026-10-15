using Microsoft.EntityFrameworkCore;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.ReadStores;

/// <summary>
/// Lectura liviana de la actividad de los workers para el latido del log: no proyecta corridas ni cuenta pendientes
/// (eso es del panel de ejecuciones), solo agenda configurada, cantidades por estado y la ultima ejecucion.
/// </summary>
public sealed class ResumenActividadWorkerReadStore : IResumenActividadWorkerReadStore
{
    private readonly IRepository<ConfiguracionProgramadaWorker> _configuracionRepository;
    private readonly IRepository<EjecucionWorker> _ejecucionRepository;

    public ResumenActividadWorkerReadStore(IRepository<ConfiguracionProgramadaWorker> configuracionRepository, IRepository<EjecucionWorker> ejecucionRepository)
    {
        _configuracionRepository = configuracionRepository;
        _ejecucionRepository = ejecucionRepository;
    }

    public async Task<IReadOnlyCollection<ResumenActividadWorkerDto>> ConsultarAsync(DateTimeOffset desde, CancellationToken cancellationToken)
    {
        ConfiguracionProgramadaWorker[] configuraciones = (await _configuracionRepository.Query().SelectAsync(cancellationToken)).OrderBy(x => x.Proceso).ToArray();

        var cantidades = await _ejecucionRepository.Queryable()
            .Where(x => x.FechaInicio >= desde)
            .GroupBy(x => new { x.Proceso, x.Estado })
            .Select(x => new { x.Key.Proceso, x.Key.Estado, Cantidad = x.Count() })
            .ToListAsync(cancellationToken);

        List<ResumenActividadWorkerDto> resumenes = new(configuraciones.Length);
        foreach (ConfiguracionProgramadaWorker configuracion in configuraciones)
        {
            UltimaEjecucionWorkerDto? ultimaEjecucion = await _ejecucionRepository.Queryable()
                .Where(x => x.Proceso == configuracion.Proceso)
                .OrderByDescending(x => x.FechaInicio)
                .Select(x => new UltimaEjecucionWorkerDto(x.FechaInicio, x.Estado, x.Origen, x.Resumen))
                .FirstOrDefaultAsync(cancellationToken);

            Dictionary<EstadoEjecucionWorker, int> ejecucionesPorEstado = cantidades.Where(x => x.Proceso == configuracion.Proceso).ToDictionary(x => x.Estado, x => x.Cantidad);
            resumenes.Add(new ResumenActividadWorkerDto(
                configuracion.Proceso, configuracion.Habilitado, configuracion.HoraInicioLocal, configuracion.HoraFinLocal, configuracion.IntervaloMinutos,
                ejecucionesPorEstado, ultimaEjecucion));
        }

        return resumenes;
    }
}
