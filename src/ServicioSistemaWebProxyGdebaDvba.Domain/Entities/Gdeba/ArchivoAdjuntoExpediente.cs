using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class ArchivoAdjuntoExpediente : DomainEntity
{
    private ArchivoAdjuntoExpediente()
    {
    }

    public ArchivoAdjuntoExpediente(
        Guid expedienteId,
        string nombreArchivo,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        NombreArchivo = string.IsNullOrWhiteSpace(nombreArchivo)
            ? throw new ArgumentException("El nombre del archivo adjunto es requerido.", nameof(nombreArchivo))
            : nombreArchivo.Trim();
        FuenteDeteccion = fuenteDeteccion;
        FechaPrimeraDeteccion = fechaDeteccion;
        FechaUltimaDeteccion = fechaDeteccion;
    }

    public Guid ExpedienteId { get; private set; }

    public Expediente Expediente { get; private set; } = null!;

    public string NombreArchivo { get; private set; } = string.Empty;

    public FuenteDeteccionGdeba FuenteDeteccion { get; private set; }

    public DateTimeOffset FechaPrimeraDeteccion { get; private set; }

    public DateTimeOffset FechaUltimaDeteccion { get; private set; }

    public void RegistrarDeteccion(FuenteDeteccionGdeba fuenteDeteccion, DateTimeOffset fechaDeteccion)
    {
        FuenteDeteccion = fuenteDeteccion;
        FechaUltimaDeteccion = fechaDeteccion;
    }
}
