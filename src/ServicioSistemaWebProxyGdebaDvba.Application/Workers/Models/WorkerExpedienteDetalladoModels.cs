namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

public sealed record DetallarExpedientesPendientesResult(int Procesados, int Detallados, int Errores, int PendientesRestantes, bool Cancelada = false);
