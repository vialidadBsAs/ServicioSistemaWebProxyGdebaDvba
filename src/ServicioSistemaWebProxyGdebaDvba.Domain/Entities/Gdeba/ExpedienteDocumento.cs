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
        // Solo escribe si algun dato del vinculo cambio o es la primera deteccion. Antes cada consulta reescribia todos los vinculos del
        // expediente (fecha y fuente de deteccion) aunque nada hubiera cambiado, y esas escrituras dominaban el costo de refrescar un
        // expediente ya detallado. Fuente y fecha de ultima deteccion quedan como la ultima deteccion que aporto datos.
        DateTimeOffset? fechaVinculacionInformada = fechaVinculacion ?? FechaVinculacion;
        int? ordenRespuestaInformado = ordenRespuesta ?? OrdenRespuesta;
        string? usuarioAsociacionInformado = Normalizar(usuarioAsociacion) ?? UsuarioAsociacion;
        string? usuarioGeneradorInformado = Normalizar(usuarioGenerador) ?? UsuarioGenerador;
        bool esPrimeraDeteccion = FechaPrimeraDeteccion == default;
        bool hayCambios = esPrimeraDeteccion ||
            fechaVinculacionInformada != FechaVinculacion ||
            ordenRespuestaInformado != OrdenRespuesta ||
            !string.Equals(usuarioAsociacionInformado, UsuarioAsociacion, StringComparison.Ordinal) ||
            !string.Equals(usuarioGeneradorInformado, UsuarioGenerador, StringComparison.Ordinal);
        if (!hayCambios)
        {
            return;
        }

        MarcarComoModificada();
        FechaVinculacion = fechaVinculacionInformada;
        OrdenRespuesta = ordenRespuestaInformado;
        UsuarioAsociacion = usuarioAsociacionInformado;
        UsuarioGenerador = usuarioGeneradorInformado;
        FuenteDeteccion = fuenteDeteccion;

        if (esPrimeraDeteccion)
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
