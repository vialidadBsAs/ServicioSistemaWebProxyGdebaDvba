using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

/// <summary>
/// Prepara la incorporacion local de una respuesta GDEBA por trata y estado.
/// El llamador coordina la confirmacion de la unidad de trabajo.
/// </summary>
public interface IIncorporacionExpedientesPorTrataService
{
    Task<IncorporarExpedientesPorTrataResult> PrepararAsync(
        IncorporarExpedientesPorTrataRequest request,
        CancellationToken cancellationToken);
}
