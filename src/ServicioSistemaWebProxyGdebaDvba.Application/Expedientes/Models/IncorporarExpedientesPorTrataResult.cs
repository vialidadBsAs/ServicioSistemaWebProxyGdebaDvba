namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record IncorporarExpedientesPorTrataResult(
    string CodigoTrata,
    string EstadoDestino,
    DateTimeOffset ResolvedAt,
    int RecibidosGdeba,
    int Incorporados,
    int Descartados,
    int Creados,
    int Actualizados);
