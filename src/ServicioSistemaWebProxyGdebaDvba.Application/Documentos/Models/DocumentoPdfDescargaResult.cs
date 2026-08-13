namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record DocumentoPdfDescargaResult(bool DocumentoEncontrado, bool DisponibleParaDescarga, string? NumeroDocumento, byte[]? Contenido);
