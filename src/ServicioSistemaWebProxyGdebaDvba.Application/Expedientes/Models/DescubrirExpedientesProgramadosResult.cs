namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record DescubrirExpedientesProgramadosResult(
    int ConsultasRealizadas,
    int RecibidosGdeba,
    int Habilitados,
    int Descartados,
    int Creados,
    int Actualizados,
    int SinCambios,
    int OmitidasPorConsultaDelDia,
    int OmitidasPorPausa,
    int OmitidasPorLimiteOperativo,
    IReadOnlyCollection<ResultadoDescubrimientoProgramadoTrataEstado> ResultadosPorTrataEstado,
    int ConsultasSeleccionadas = 0,
    bool Cancelada = false,
    int ConsultasConError = 0)
{
    /// <summary>Consolida el plan y los resultados de cada consulta en el resumen de la corrida; lo arma el host al terminar el recorrido.</summary>
    public static DescubrirExpedientesProgramadosResult Desde(PlanDescubrimientoProgramado plan, IReadOnlyCollection<ResultadoConsultaDescubrimiento> consultas)
    {
        IReadOnlyCollection<ResultadoConsultaDescubrimiento> realizadas = consultas.Where(x => x.Incorporacion is not null).ToArray();
        IncorporarExpedientesPorTrataResult[] incorporaciones = realizadas.Select(x => x.Incorporacion!).ToArray();

        return new DescubrirExpedientesProgramadosResult(
            incorporaciones.Length,
            incorporaciones.Sum(x => x.RecibidosGdeba),
            incorporaciones.Sum(x => x.Habilitados),
            incorporaciones.Sum(x => x.Descartados),
            incorporaciones.Sum(x => x.Creados),
            incorporaciones.Sum(x => x.Actualizados),
            incorporaciones.Sum(x => x.SinCambios),
            plan.OmitidasPorConsultaDelDia,
            plan.OmitidasPorPausa,
            plan.OmitidasPorLimiteOperativo,
            realizadas.Select(x => new ResultadoDescubrimientoProgramadoTrataEstado(x.Consulta.TrataHabilitadaVialidadId, x.Consulta.EstadoExpedienteGdebaId, x.Incorporacion!)).ToArray(),
            plan.Consultas.Count,
            consultas.Any(x => x.Estado == EstadoConsultaDescubrimiento.CanceladaAntesDeConsultar || x.CancelacionSolicitada),
            consultas.Count(x => x.Estado is EstadoConsultaDescubrimiento.RechazadaPorGdeba or EstadoConsultaDescubrimiento.RechazoDeAcceso));
    }
}

public sealed record ResultadoDescubrimientoProgramadoTrataEstado(
    Guid TrataHabilitadaVialidadId,
    Guid EstadoExpedienteGdebaId,
    IncorporarExpedientesPorTrataResult Resultado);

/// <summary>
/// Plan de una corrida: las consultas trata-estado seleccionadas, ya ordenadas y recortadas por el limite operativo, mas lo que quedo afuera y por que.
/// Son datos simples: el host las recorre y cada consulta se procesa en su propia unidad de trabajo.
/// </summary>
public sealed record PlanDescubrimientoProgramado(
    IReadOnlyList<ConsultaDescubrimientoProgramada> Consultas,
    int OmitidasPorConsultaDelDia,
    int OmitidasPorPausa,
    int OmitidasPorLimiteOperativo);

public sealed record ConsultaDescubrimientoProgramada(Guid TrataHabilitadaVialidadId, string CodigoTrata, Guid EstadoExpedienteGdebaId, string NombreEstado);

public enum EstadoConsultaDescubrimiento
{
    /// <summary>GDEBA respondio y la incorporacion quedo confirmada.</summary>
    Realizada,
    /// <summary>GDEBA proceso el pedido y lo rechazo: falla de esta consulta, la corrida sigue.</summary>
    RechazadaPorGdeba,
    /// <summary>El gateway rechazo el acceso (401/403) o no hubo respuesta. Aislado es una falla de esta consulta; varios seguidos, GDEBA no accesible.</summary>
    RechazoDeAcceso,
    /// <summary>La cancelacion ya estaba pedida: no se invoco GDEBA.</summary>
    CanceladaAntesDeConsultar
}

/// <summary>Resultado de una consulta trata-estado. CancelacionSolicitada indica que la corrida no debe iniciar la siguiente.</summary>
public sealed record ResultadoConsultaDescubrimiento(
    ConsultaDescubrimientoProgramada Consulta,
    EstadoConsultaDescubrimiento Estado,
    IncorporarExpedientesPorTrataResult? Incorporacion,
    bool CancelacionSolicitada);
