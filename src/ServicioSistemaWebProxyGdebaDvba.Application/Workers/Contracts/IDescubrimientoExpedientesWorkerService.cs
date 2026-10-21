using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;

/// <summary>
/// Descubrimiento programado en dos pasos: preparar el plan de consultas trata-estado y procesar cada consulta como una unidad de trabajo
/// independiente. El host recorre el plan con un scope por consulta, asi el contexto de datos no acumula lo incorporado en las anteriores.
/// </summary>
public interface IDescubrimientoExpedientesWorkerService
{
    /// <summary>Selecciona y ordena las consultas de la corrida y sella el lote de la ejecucion.</summary>
    Task<PlanDescubrimientoProgramado> PrepararAsync(Guid ejecucionWorkerId, DescubrirExpedientesProgramadosRequest request, CancellationToken cancellationToken);

    /// <summary>Consulta GDEBA para una trata-estado, incorpora lo recibido y confirma el resultado en la ejecucion.</summary>
    Task<ResultadoConsultaDescubrimiento> ConsultarAsync(Guid ejecucionWorkerId, ConsultaDescubrimientoProgramada consulta, DescubrirExpedientesProgramadosRequest request, CancellationToken cancellationToken);
}
