namespace ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;

public sealed record GuardarTemaExpedienteRequest(string Codigo, string Nombre, string? Descripcion, IReadOnlyCollection<Guid>? TratasHabilitadasVialidadIds);
