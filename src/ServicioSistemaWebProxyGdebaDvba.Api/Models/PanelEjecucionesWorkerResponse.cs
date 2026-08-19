using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Models;

public sealed record SolicitarEjecucionManualWorkerApiRequest(DateTimeOffset? FechaInicioProgramada);

public sealed record ProyeccionCorridaAutomaticaResponse(string Estado, DateTimeOffset? ProximaCorrida, string? OmitidaPor)
{
    public static ProyeccionCorridaAutomaticaResponse Create(ProyeccionCorridaAutomaticaDto proyeccion)
    {
        return new ProyeccionCorridaAutomaticaResponse(proyeccion.Estado.ToString(), proyeccion.ProximaCorrida, proyeccion.OmitidaPor);
    }
}

public sealed record PanelEjecucionesWorkerResponse(
    string Proceso,
    ConfiguracionProgramadaWorkerResponse Configuracion,
    ProyeccionCorridaAutomaticaResponse CorridaAutomatica,
    int? PendientesDeProceso,
    IReadOnlyCollection<SolicitudEjecucionWorkerResponse> OrdenesManualesVivas,
    IReadOnlyCollection<EjecucionWorkerResponse> EjecucionesDelDia,
    IReadOnlyCollection<EjecucionWorkerResponse> Historico)
{
    public static PanelEjecucionesWorkerResponse Create(ConsultaPanelEjecucionesWorkerResult resultado)
    {
        return new PanelEjecucionesWorkerResponse(
            resultado.Proceso.ToString(),
            ConfiguracionProgramadaWorkerResponse.Create(resultado.Configuracion),
            ProyeccionCorridaAutomaticaResponse.Create(resultado.CorridaAutomatica),
            resultado.PendientesDeProceso,
            resultado.OrdenesManualesVivas.Select(SolicitudEjecucionWorkerResponse.Create).ToArray(),
            resultado.EjecucionesDelDia.Select(EjecucionWorkerResponse.Create).ToArray(),
            resultado.Historico.Select(EjecucionWorkerResponse.Create).ToArray());
    }
}
