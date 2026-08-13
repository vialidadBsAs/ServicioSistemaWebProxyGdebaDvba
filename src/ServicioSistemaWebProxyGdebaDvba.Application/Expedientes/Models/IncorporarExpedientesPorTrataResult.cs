namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record IncorporarExpedientesPorTrataResult(
    string CodigoTrata,
    Guid TrataHabilitadaVialidadId,
    string EstadoDestino,
    DateTimeOffset ResolvedAt,
    int RecibidosGdeba,
    int Habilitados,
    int Descartados,
    int Creados,
    int Actualizados,
    int SinCambios,
    IReadOnlyCollection<Guid> ExpedientesNuevosIds);
