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
    bool Cancelada = false);

public sealed record ResultadoDescubrimientoProgramadoTrataEstado(
    Guid TrataHabilitadaVialidadId,
    Guid EstadoExpedienteGdebaId,
    IncorporarExpedientesPorTrataResult Resultado);
