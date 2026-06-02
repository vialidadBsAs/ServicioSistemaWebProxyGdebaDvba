using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class Expediente : IAggregateRoot
{
    public void AplicarCabeceraDetallada(
        Guid? trataId,
        string? estadoActual,
        string? sistemaOrigen,
        string? descripcionTramite,
        DateTimeOffset? fechaCaratulacion,
        string? usuarioCaratulador,
        string? usuarioDestino,
        string? sectorDestino,
        string? reparticionActual)
    {
        ActualizarCabecera(
            trataId,
            estadoActual,
            sistemaOrigen,
            descripcionTramite,
            fechaCaratulacion,
            usuarioCaratulador,
            usuarioDestino,
            sectorDestino,
            reparticionActual);
    }

    public ExpedienteDocumento RegistrarDocumentoDetectado(
        DocumentoGdeba documentoGdeba,
        DateTimeOffset? fechaVinculacion,
        int? ordenRespuesta,
        string? usuarioAsociacion,
        string? usuarioGenerador,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        ArgumentNullException.ThrowIfNull(documentoGdeba);

        var expedienteDocumento = _documentos.FirstOrDefault(x => x.DocumentoId == documentoGdeba.Id);
        if (expedienteDocumento is null)
        {
            expedienteDocumento = new ExpedienteDocumento(Id, documentoGdeba);
            _documentos.Add(expedienteDocumento);
        }

        expedienteDocumento.RegistrarDeteccion(
            fechaVinculacion,
            ordenRespuesta,
            usuarioAsociacion,
            usuarioGenerador,
            fuenteDeteccion,
            fechaDeteccion);

        return expedienteDocumento;
    }

    public ArchivoAdjuntoExpediente RegistrarArchivoAdjuntoDetectado(
        string nombreArchivo,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        var nombreNormalizado = NormalizarRequerido(nombreArchivo, nameof(nombreArchivo));
        var archivo = _archivosAdjuntos.FirstOrDefault(x =>
            string.Equals(x.NombreArchivo, nombreNormalizado, StringComparison.OrdinalIgnoreCase));

        if (archivo is null)
        {
            archivo = new ArchivoAdjuntoExpediente(Id, nombreNormalizado, fuenteDeteccion, fechaDeteccion);
            _archivosAdjuntos.Add(archivo);
            return archivo;
        }

        archivo.RegistrarDeteccion(fuenteDeteccion, fechaDeteccion);
        return archivo;
    }

    public ExpedienteRelacion RegistrarRelacionDetectada(
        string numeroExpedienteRelacionado,
        TipoRelacionExpediente tipoRelacion,
        Guid? expedienteRelacionadoId,
        string? codigoTrataRelacionado,
        string? descripcionTrataRelacionado,
        DateTimeOffset? fechaRelacion,
        string? usuarioRelacion,
        bool? esCabecera,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        var numeroNormalizado = NormalizarRequerido(
            numeroExpedienteRelacionado,
            nameof(numeroExpedienteRelacionado));

        var relacion = _relaciones.FirstOrDefault(x =>
            x.TipoRelacion == tipoRelacion &&
            string.Equals(x.NumeroExpedienteRelacionado, numeroNormalizado, StringComparison.OrdinalIgnoreCase));

        if (relacion is null)
        {
            relacion = new ExpedienteRelacion(
                Id,
                numeroNormalizado,
                tipoRelacion,
                fuenteDeteccion,
                fechaDeteccion);
            _relaciones.Add(relacion);
        }

        relacion.RegistrarDeteccion(
            expedienteRelacionadoId,
            codigoTrataRelacionado,
            descripcionTrataRelacionado,
            fechaRelacion,
            usuarioRelacion,
            esCabecera,
            fuenteDeteccion,
            fechaDeteccion);

        return relacion;
    }

    public void MarcarDetalleConsultadoCorrectamente(
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        DateTimeOffset? fechaVencimiento,
        bool estaCompleto,
        bool tieneDatosParciales)
    {
        CacheControl ??= new ExpedienteCacheControl(Id, fechaActualizacionLocal);
        CacheControl.RegistrarConsulta(
            fechaConsulta,
            fechaActualizacionLocal,
            fechaVencimiento,
            FuenteRespuesta.Gdeba,
            estaCompleto,
            tieneDatosParciales,
            ultimoErrorConsulta: null);
    }

    public void MarcarDetalleConsultadoConError(
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        string? error)
    {
        CacheControl ??= new ExpedienteCacheControl(Id, fechaActualizacionLocal);
        CacheControl.RegistrarConsulta(
            fechaConsulta,
            fechaActualizacionLocal,
            CacheControl.FechaVencimiento,
            FuenteRespuesta.FallbackCache,
            CacheControl.EstaCompleto,
            tieneDatosParciales: true,
            error);
    }

    public void MarcarHistorialConsultadoCorrectamente(
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        DateTimeOffset? fechaVencimiento,
        Guid? ultimoMovimientoDetectadoId,
        bool estaCompleto,
        bool tieneDatosParciales)
    {
        HistorialCacheControl ??= new HistorialExpedienteCacheControl(Id, fechaActualizacionLocal);
        HistorialCacheControl.RegistrarConsulta(
            fechaConsulta,
            fechaActualizacionLocal,
            fechaVencimiento,
            FuenteRespuesta.Gdeba,
            ultimoMovimientoDetectadoId,
            estaCompleto,
            tieneDatosParciales,
            ultimoErrorConsulta: null);
    }

    private static string NormalizarRequerido(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", parameterName)
            : value.Trim();
    }
}
