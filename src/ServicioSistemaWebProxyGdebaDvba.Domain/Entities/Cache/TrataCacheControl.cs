using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class TrataCacheControl : DomainEntity
{
    private TrataCacheControl()
    {
    }

    public TrataCacheControl(Guid trataId, DateTimeOffset fechaPrimeraDeteccion)
    {
        TrataId = trataId == Guid.Empty
            ? throw new ArgumentException("La trata es requerida.", nameof(trataId))
            : trataId;
        FechaPrimeraDeteccion = fechaPrimeraDeteccion;
    }

    public Guid TrataId { get; private set; }

    public TrataHabilitadaVialidad Trata { get; private set; } = null!;

    public DateTimeOffset FechaPrimeraDeteccion { get; private set; }

    public DateTimeOffset? FechaUltimaConsultaGdeba { get; private set; }

    public DateTimeOffset? FechaUltimaActualizacionLocal { get; private set; }

    public DateTimeOffset? FechaVencimiento { get; private set; }

    public FuenteRespuesta? FuenteUltimaRespuesta { get; private set; }

    public bool EstaCompleta { get; private set; }

    public string? UltimoErrorConsulta { get; private set; }
}
