using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;

// Colaborador de la consolidacion de documentos: toma los vinculos recien detectados de un expediente y registra las coincidencias
// con las alertas activas de todos los usuarios. No guarda por su cuenta: participa de la unidad de trabajo del caso de uso que
// consolida el expediente, asi la coincidencia nace junto con el vinculo o no nace.
public interface IDetectorCoincidenciasAlertaDocumental
{
    Task RegistrarCoincidenciasAsync(Expediente expediente, DateTimeOffset fechaDeteccion, CancellationToken cancellationToken);
}
