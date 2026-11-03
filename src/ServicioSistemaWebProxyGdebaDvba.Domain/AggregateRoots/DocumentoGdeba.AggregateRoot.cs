using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class DocumentoGdeba : IAggregateRoot
{
    public void RegistrarMetadataParcial(string? tipoDocumentoCodigo, string? referencia, DateTimeOffset? fechaCreacion)
    {
        // El detalle del expediente solo conoce el acronimo del numero (ACTA); el historial trae el tipo especifico (ACMED). Un acronimo
        // nunca pisa un tipo especifico ya conocido: las alertas por tipo especifico dependen de conservarlo.
        string? tipoInformado = string.IsNullOrWhiteSpace(tipoDocumentoCodigo) ? null : tipoDocumentoCodigo.Trim();
        bool esSoloAcronimo = tipoInformado is not null && string.Equals(tipoInformado, ActuacionTipoCodigo, StringComparison.OrdinalIgnoreCase);
        string? tipoAConservar = esSoloAcronimo && TipoDocumentoCodigo is not null ? TipoDocumentoCodigo : tipoInformado ?? TipoDocumentoCodigo;
        this.ActualizarMetadata(
            NumeroEspecialCompleto, tipoAConservar,
            tipoDocumentoNombre: null, tipoDocumentoDescripcion: null,
            referencia ?? Referencia, fechaCreacion ?? FechaCreacion,
            ListaFirmantes, UrlArchivo, PuedeVerDocumento,
            FechaUltimoEnriquecimiento,
            metadataCompleta: MetadataCompleta);
    }

    public void EnriquecerDesdeDetalleDocumento(
        string? numeroEspecial, string? tipoDocumentoCodigo,
        string? referencia, DateTimeOffset? fechaCreacion,
        string? listaFirmantes, string? urlArchivo,
        bool? puedeVerDocumento, DateTimeOffset fechaEnriquecimiento)
    {
        this.ActualizarMetadata(
            numeroEspecial, tipoDocumentoCodigo,
            tipoDocumentoNombre: null, tipoDocumentoDescripcion: null,
            referencia, fechaCreacion, listaFirmantes, urlArchivo,
            puedeVerDocumento, fechaEnriquecimiento,
            metadataCompleta: true);
    }

    public void RegistrarActividadHistorial(
        long idGdeba, string? actividad, DateTimeOffset? fechaInicio,
        DateTimeOffset? fechaFin, string? usuario, string? nombreUsuario,
        string? workflowOrigen)
    {
        var historial = _historial.FirstOrDefault(x => x.IdGdeba == idGdeba);
        if (historial is null)
        {
            historial = new HistorialDocumentoGdeba(Id, idGdeba);
            historial.TrackingState = TrackableEntities.Common.Core.TrackingState.Added;
            _historial.Add(historial);
        }

        historial.Actualizar(actividad, fechaInicio, fechaFin, usuario, nombreUsuario, workflowOrigen);
    }

    public void AsignarTipoDocumento(TipoDocumentoGdeba tipoDocumento)
    {
        ArgumentNullException.ThrowIfNull(tipoDocumento);

        this.MarcarComoModificada();
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
