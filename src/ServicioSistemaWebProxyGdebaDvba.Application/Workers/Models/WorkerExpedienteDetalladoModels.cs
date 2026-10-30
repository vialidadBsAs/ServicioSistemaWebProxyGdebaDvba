namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

// Refrescados: expedientes ya detallados que se volvieron a consultar porque GDEBA informo una modificacion posterior al ultimo detalle.
// RefrescosPendientes: los que siguen en esa condicion al terminar la corrida; los toma la siguiente, antes del drenaje.
public sealed record DetallarExpedientesPendientesResult(int Procesados, int Detallados, int Errores, int PendientesRestantes, int Refrescados, int RefrescosPendientes, bool Cancelada = false);
