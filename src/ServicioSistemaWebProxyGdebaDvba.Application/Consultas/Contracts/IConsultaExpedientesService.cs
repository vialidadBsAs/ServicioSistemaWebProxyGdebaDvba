using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Contracts;

public interface IConsultaExpedientesService
{
    Task<ConsultaExpedientesResult> ConsultarAsync(ConsultaExpedientesRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> ObtenerValoresFiltroAsync(ConsultaExpedientesValoresFiltroRequest request, CancellationToken cancellationToken);
}
