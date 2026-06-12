using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class DocumentoGdeba : IAggregateRoot
{
    public void RegistrarMetadataParcial(
        string? tipoDocumentoCodigo,
        string? referencia,
        DateTimeOffset? fechaCreacion)
    {
        ActualizarMetadata(
            NumeroEspecialCompleto,
            tipoDocumentoCodigo ?? TipoDocumentoCodigo,
            TipoDocumentoNombre,
            TipoDocumentoDescripcion,
            referencia ?? Referencia,
            fechaCreacion ?? FechaCreacion,
            ListaFirmantes,
            UrlArchivo,
            PuedeVerDocumento,
            FechaUltimoEnriquecimiento,
            metadataCompleta: false);
    }

    public void EnriquecerDesdeDetalleDocumento(
        string? numeroEspecial,
        string? tipoDocumentoCodigo,
        string? tipoDocumentoNombre,
        string? tipoDocumentoDescripcion,
        string? referencia,
        DateTimeOffset? fechaCreacion,
        string? listaFirmantes,
        string? urlArchivo,
        bool? puedeVerDocumento,
        DateTimeOffset fechaEnriquecimiento)
    {
        ActualizarMetadata(
            numeroEspecial,
            tipoDocumentoCodigo,
            tipoDocumentoNombre,
            tipoDocumentoDescripcion,
            referencia,
            fechaCreacion,
            listaFirmantes,
            urlArchivo,
            puedeVerDocumento,
            fechaEnriquecimiento,
            metadataCompleta: true);
    }

    public void AsignarTipoDocumento(TipoDocumentoGdeba tipoDocumento)
    {
        ArgumentNullException.ThrowIfNull(tipoDocumento);

        MarcarComoModificada();
        TipoDocumento = tipoDocumento;
        TipoDocumentoId = tipoDocumento.Id;
    }

    public bool EsPotencialResolucion()
    {
        return string.Equals(ActuacionTipoCodigo, "RS", StringComparison.OrdinalIgnoreCase);
    }

    public bool EsResolucionConfirmada(TipoDocumentoGdeba? tipoDocumento)
    {
        if (tipoDocumento is not null)
        {
            return tipoDocumento.EsResolucion;
        }

        return EspecialTipoCodigo is not null &&
            EspecialTipoCodigo.StartsWith("RES", StringComparison.OrdinalIgnoreCase);
    }
}
