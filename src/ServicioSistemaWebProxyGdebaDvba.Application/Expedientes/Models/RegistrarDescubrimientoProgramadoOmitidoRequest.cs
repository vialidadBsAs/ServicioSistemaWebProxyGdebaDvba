namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record RegistrarDescubrimientoProgramadoOmitidoRequest(int? LimiteDiario, int InvocacionesRegistradas, int CupoReservaDiaria, string Motivo);
