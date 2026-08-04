namespace ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;

public sealed record TemaExpedienteDto(Guid Id, string Codigo, string Nombre, string? Descripcion, IReadOnlyCollection<TrataHabilitadaVialidadDto> Tratas);
