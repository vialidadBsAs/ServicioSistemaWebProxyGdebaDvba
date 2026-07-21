namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record DescubrirExpedientesPorTrataResult(
    string CodigoTrata,
    DateTimeOffset ResolvedAt,
    IReadOnlyCollection<IncorporarExpedientesPorTrataResult> Estados,
    int RecibidosGdeba,
    int Habilitados,
    int Descartados,
    int Creados,
    int Actualizados,
    int SinCambios);
