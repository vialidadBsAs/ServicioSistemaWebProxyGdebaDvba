namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;

public sealed record GdebaDocumentoPdfDto(string NumeroDocumento, byte[] Contenido);
