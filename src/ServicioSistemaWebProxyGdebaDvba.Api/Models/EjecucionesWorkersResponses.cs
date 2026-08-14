using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Models;

public sealed record EjecucionWorkerResponse(
    Guid Id,
    string Proceso,
    string Origen,
    string Estado,
    Guid? SolicitudEjecucionWorkerId,
    DateTimeOffset FechaInicio,
    DateTimeOffset? FechaFinalizacion,
    string? Resumen,
    int? Procesados,
    int? Creados,
    int? Enriquecidos,
    int? SinDatos,
    int? Errores,
    SolicitudManualAsociadaEjecucionWorkerResponse? SolicitudManual)
{
    public static EjecucionWorkerResponse Create(EjecucionWorkerDto ejecucion)
    {
        return new EjecucionWorkerResponse(
            ejecucion.Id, ejecucion.Proceso.ToString(), ejecucion.Origen.ToString(), ejecucion.Estado.ToString(),
            ejecucion.SolicitudEjecucionWorkerId, ejecucion.FechaInicio, ejecucion.FechaFinalizacion,
            ejecucion.Resumen, ejecucion.Procesados, ejecucion.Creados, ejecucion.Enriquecidos, ejecucion.SinDatos, ejecucion.Errores,
            ejecucion.SolicitudManual is null ? null : SolicitudManualAsociadaEjecucionWorkerResponse.Create(ejecucion.SolicitudManual));
    }
}

public sealed record SolicitudManualAsociadaEjecucionWorkerResponse(Guid Id, string SolicitadaPor, DateTimeOffset FechaSolicitud)
{
    public static SolicitudManualAsociadaEjecucionWorkerResponse Create(SolicitudManualAsociadaEjecucionWorkerDto solicitud)
    {
        return new SolicitudManualAsociadaEjecucionWorkerResponse(solicitud.Id, solicitud.SolicitadaPor, solicitud.FechaSolicitud);
    }
}

public sealed record SolicitudEjecucionWorkerResponse(
    Guid Id,
    string Proceso,
    string Estado,
    string SolicitadaPor,
    DateTimeOffset FechaSolicitud,
    DateTimeOffset? FechaInicioProgramada,
    DateTimeOffset? FechaInicio,
    DateTimeOffset? FechaFinalizacion,
    string? CanceladaPor,
    DateTimeOffset? FechaCancelacion,
    string? Mensaje,
    Guid? EjecucionWorkerId)
{
    public static SolicitudEjecucionWorkerResponse Create(SolicitudEjecucionWorkerDto solicitud)
    {
        return new SolicitudEjecucionWorkerResponse(
            solicitud.Id, solicitud.Proceso.ToString(), solicitud.Estado.ToString(), solicitud.SolicitadaPor,
            solicitud.FechaSolicitud, solicitud.FechaInicioProgramada, solicitud.FechaInicio, solicitud.FechaFinalizacion,
            solicitud.CanceladaPor, solicitud.FechaCancelacion, solicitud.Mensaje, solicitud.EjecucionWorkerId);
    }
}

public sealed record DetalleEjecucionDescubrimientoResponse(
    EjecucionWorkerResponse Ejecucion,
    IReadOnlyCollection<ResultadoEjecucionDescubrimientoTrataEstadoResponse> ResultadosPorTrataEstado)
{
    public static DetalleEjecucionDescubrimientoResponse Create(ConsultaDetalleEjecucionDescubrimientoResult resultado)
    {
        return new DetalleEjecucionDescubrimientoResponse(
            EjecucionWorkerResponse.Create(resultado.Ejecucion),
            resultado.ResultadosPorTrataEstado.Select(ResultadoEjecucionDescubrimientoTrataEstadoResponse.Create).ToArray());
    }
}

public sealed record ResultadoEjecucionDescubrimientoTrataEstadoResponse(
    Guid Id,
    string CodigoTrata,
    string? DescripcionTrata,
    string EstadoDestino,
    DateTimeOffset FechaResolucion,
    int RecibidosGdeba,
    int Habilitados,
    int Descartados,
    int Creados,
    int Actualizados,
    int SinCambios,
    IReadOnlyCollection<ExpedienteDescubiertoPorEjecucionResponse> ExpedientesDetectados)
{
    public static ResultadoEjecucionDescubrimientoTrataEstadoResponse Create(ResultadoEjecucionDescubrimientoTrataEstadoDto resultado)
    {
        return new ResultadoEjecucionDescubrimientoTrataEstadoResponse(
            resultado.Id, resultado.CodigoTrata, resultado.DescripcionTrata, resultado.EstadoDestino,
            resultado.FechaResolucion, resultado.RecibidosGdeba, resultado.Habilitados, resultado.Descartados,
            resultado.Creados, resultado.Actualizados, resultado.SinCambios,
            resultado.ExpedientesDetectados.Select(ExpedienteDescubiertoPorEjecucionResponse.Create).ToArray());
    }
}

public sealed record ExpedienteDescubiertoPorEjecucionResponse(Guid Id, string NumeroExpediente, string TipoDeteccion)
{
    public static ExpedienteDescubiertoPorEjecucionResponse Create(ExpedienteDescubiertoPorEjecucionDto expediente)
    {
        return new ExpedienteDescubiertoPorEjecucionResponse(expediente.Id, expediente.NumeroExpediente, expediente.TipoDeteccion.ToString());
    }
}
