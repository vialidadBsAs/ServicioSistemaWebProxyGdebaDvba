using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class ExpedienteDocumento : DomainEntity
{
    private ExpedienteDocumento()
    {
    }

    public ExpedienteDocumento(Guid expedienteId, Guid documentoId)
    {
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        DocumentoId = documentoId == Guid.Empty
            ? throw new ArgumentException("El documento es requerido.", nameof(documentoId))
            : documentoId;
    }

    public ExpedienteDocumento(Guid expedienteId, DocumentoGdeba documento)
    {
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        Documento = documento ?? throw new ArgumentNullException(nameof(documento));
        DocumentoId = documento.Id;
    }

    public Guid ExpedienteId { get; private set; }

    public Expediente Expediente { get; private set; } = null!;

    public Guid DocumentoId { get; private set; }

    public DocumentoGdeba Documento { get; private set; } = null!;

    public DateTimeOffset? FechaVinculacion { get; private set; }

    public int? OrdenRespuesta { get; private set; }

    public string? UsuarioAsociacion { get; private set; }

    public string? UsuarioGenerador { get; private set; }

    public FuenteDeteccionGdeba FuenteDeteccion { get; private set; }

    public DateTimeOffset FechaPrimeraDeteccion { get; private set; }

    public DateTimeOffset FechaUltimaDeteccion { get; private set; }

    public void RegistrarDeteccion(
        DateTimeOffset? fechaVinculacion,
        int? ordenRespuesta,
        string? usuarioAsociacion,
        string? usuarioGenerador,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        FechaVinculacion = fechaVinculacion;
        OrdenRespuesta = ordenRespuesta;
        UsuarioAsociacion = Normalizar(usuarioAsociacion);
        UsuarioGenerador = Normalizar(usuarioGenerador);
        FuenteDeteccion = fuenteDeteccion;

        if (FechaPrimeraDeteccion == default)
        {
            FechaPrimeraDeteccion = fechaDeteccion;
        }

        FechaUltimaDeteccion = fechaDeteccion;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
