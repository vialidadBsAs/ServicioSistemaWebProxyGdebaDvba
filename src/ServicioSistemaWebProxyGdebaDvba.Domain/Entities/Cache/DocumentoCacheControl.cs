using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class DocumentoCacheControl : DomainEntity
{
    private DocumentoCacheControl()
    {
    }

    public DocumentoCacheControl(Guid documentoId, DateTimeOffset fechaPrimeraDeteccion)
    {
        DocumentoId = documentoId == Guid.Empty
            ? throw new ArgumentException("El documento es requerido.", nameof(documentoId))
            : documentoId;
        FechaPrimeraDeteccion = fechaPrimeraDeteccion;
    }

    public Guid DocumentoId { get; private set; }

    public DocumentoGdeba Documento { get; private set; } = null!;

    public DateTimeOffset FechaPrimeraDeteccion { get; private set; }

    public DateTimeOffset? FechaUltimaConsultaGdeba { get; private set; }

    public DateTimeOffset? FechaUltimaActualizacionLocal { get; private set; }

    public DateTimeOffset? FechaVencimiento { get; private set; }

    public FuenteRespuesta? FuenteUltimaRespuesta { get; private set; }

    public bool EstaCompleto { get; private set; }

    public bool TieneDatosParciales { get; private set; }

    public string? UltimoErrorConsulta { get; private set; }
}
