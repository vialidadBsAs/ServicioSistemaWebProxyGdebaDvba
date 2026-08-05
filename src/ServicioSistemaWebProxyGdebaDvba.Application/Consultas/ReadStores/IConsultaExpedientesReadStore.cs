using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.ReadStores;

public interface IConsultaExpedientesReadStore
{
    Task<ConsultaExpedientesResult> ConsultarAsync(ConsultaExpedientesFiltro filtro, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> ObtenerValoresFiltroAsync(IReadOnlyCollection<Guid> trataIds, string campo, DateTimeOffset fechaConsulta, CancellationToken cancellationToken);
}
