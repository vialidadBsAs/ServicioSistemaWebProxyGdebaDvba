using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Auditoria;

public sealed record RegistrarAuditoriaRequest(
    string AplicacionConsumidoraCodigo,
    string Operacion,
    string? Recurso,
    AmbienteGdeba Ambiente,
    FuenteRespuesta? Fuente,
    bool Exitoso,
    DateTimeOffset Fecha);
