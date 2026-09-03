using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed partial class EjecucionWorker : DomainEntity
{
    private readonly List<EjecucionWorkerDescubrimientoTrataEstado> _resultadosDescubrimientoTrataEstado = [];
    private EjecucionWorker()
    {
    }

    public EjecucionWorker(ProcesoWorker proceso, OrigenInvocacionGdeba origen, Guid? solicitudEjecucionWorkerId, DateTimeOffset fechaInicio)
    {
        Proceso = proceso;
        Origen = origen;
        SolicitudEjecucionWorkerId = solicitudEjecucionWorkerId;
        FechaInicio = fechaInicio;
        Estado = EstadoEjecucionWorker.EnEjecucion;
    }

    public ProcesoWorker Proceso { get; private set; }
    public OrigenInvocacionGdeba Origen { get; private set; }
    public EstadoEjecucionWorker Estado { get; private set; }
    public Guid? SolicitudEjecucionWorkerId { get; private set; }
    public DateTimeOffset FechaInicio { get; private set; }
    public DateTimeOffset? FechaFinalizacion { get; private set; }
    public string? Resumen { get; private set; }
    // El lote se sella al iniciar la corrida: es el denominador del avance aunque la configuracion cambie en el medio.
    public int? TamanoLote { get; private set; }
    public DateTimeOffset? FechaCancelacionSolicitada { get; private set; }
    public int? Procesados { get; private set; }
    public int? Creados { get; private set; }
    public int? Enriquecidos { get; private set; }
    public int? SinDatos { get; private set; }
    public int? Errores { get; private set; }
    public IReadOnlyCollection<EjecucionWorkerDescubrimientoTrataEstado> ResultadosDescubrimientoTrataEstado => _resultadosDescubrimientoTrataEstado;

    public void SellarLote(int tamanoLote)
    {
        TamanoLote = tamanoLote;
        this.MarcarComoModificada();
    }

    // Avance aproximado durante la corrida (por sub-lote); el cierre siempre escribe el conteo exacto.
    public void RegistrarAvance(int procesados)
    {
        Procesados = procesados;
        this.MarcarComoModificada();
    }

    public void SolicitarCancelacion(DateTimeOffset fecha)
    {
        if (Estado != EstadoEjecucionWorker.EnEjecucion)
        {
            throw new InvalidOperationException("Solo se puede cancelar una ejecucion que esta en ejecucion.");
        }

        if (FechaCancelacionSolicitada is null)
        {
            FechaCancelacionSolicitada = fecha;
            this.MarcarComoModificada();
        }
    }

    public void Finalizar(EstadoEjecucionWorker estado, string? resumen, int? procesados, int? creados, int? enriquecidos, int? sinDatos, int? errores, DateTimeOffset fechaFinalizacion)
    {
        Estado = estado;
        Resumen = string.IsNullOrWhiteSpace(resumen) ? null : resumen.Trim();
        Procesados = procesados;
        Creados = creados;
        Enriquecidos = enriquecidos;
        SinDatos = sinDatos;
        Errores = errores;
        FechaFinalizacion = fechaFinalizacion;
        this.MarcarComoModificada();
    }
}
