using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;

public sealed record RegistrarAuditoriaRequest(string AplicacionConsumidoraCodigo, string OperacionSolicitada, string OperacionGdeba, string? Recurso, AmbienteGdeba Ambiente, FuenteRespuesta? Fuente, bool Exitoso, string? Mensaje, DateTimeOffset Fecha);
