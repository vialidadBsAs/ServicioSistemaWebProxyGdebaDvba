using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class DocumentoArchivoLocal : DomainEntity
{
    private DocumentoArchivoLocal()
    {
    }

    public DocumentoArchivoLocal(Guid documentoId)
    {
        DocumentoId = documentoId == Guid.Empty
            ? throw new ArgumentException("El documento es requerido.", nameof(documentoId))
            : documentoId;
    }

    public Guid DocumentoId { get; private set; }

    public DocumentoGdeba Documento { get; private set; } = null!;

    public string? StorageProvider { get; private set; }

    public string? StorageKey { get; private set; }

    public string? RutaRelativa { get; private set; }

    public string? ContentType { get; private set; }

    public string? ExtensionArchivo { get; private set; }

    public string? HashContenido { get; private set; }

    public long? LongitudBytes { get; private set; }

    public DateTimeOffset? FechaDescarga { get; private set; }

    public DateTimeOffset? FechaUltimaVerificacion { get; private set; }

    public void RegistrarUbicacion(
        string? storageProvider,
        string? storageKey,
        string? rutaRelativa,
        string? contentType,
        string? extensionArchivo,
        string? hashContenido,
        long? longitudBytes,
        DateTimeOffset fechaDescarga)
    {
        StorageProvider = Normalizar(storageProvider);
        StorageKey = Normalizar(storageKey);
        RutaRelativa = Normalizar(rutaRelativa);
        ContentType = Normalizar(contentType);
        ExtensionArchivo = Normalizar(extensionArchivo);
        HashContenido = Normalizar(hashContenido);
        LongitudBytes = longitudBytes;
        FechaDescarga = fechaDescarga;
        FechaUltimaVerificacion = fechaDescarga;
    }

    public void RegistrarVerificacion(DateTimeOffset fechaVerificacion)
    {
        FechaUltimaVerificacion = fechaVerificacion;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
