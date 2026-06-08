using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class HistorialExpedienteCacheControl : DomainEntity
{
    private HistorialExpedienteCacheControl()
    {
    }

    public HistorialExpedienteCacheControl(Guid expedienteId, DateTimeOffset fechaPrimeraDeteccion)
    {
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        FechaPrimeraDeteccion = fechaPrimeraDeteccion;
    }

    public Guid ExpedienteId { get; private set; }

    public Expediente Expediente { get; private set; } = null!;

    public Guid? UltimoMovimientoDetectadoId { get; private set; }

    public MovimientoExpediente? UltimoMovimientoDetectado { get; private set; }

    public DateTimeOffset FechaPrimeraDeteccion { get; private set; }

    public DateTimeOffset? FechaUltimaConsultaGdeba { get; private set; }

    public DateTimeOffset? FechaUltimaActualizacionLocal { get; private set; }

    public DateTimeOffset? FechaVencimiento { get; private set; }

    public FuenteRespuesta? FuenteUltimaRespuesta { get; private set; }

    public bool EstaCompleto { get; private set; }

    public bool TieneDatosParciales { get; private set; }

    public string? UltimoErrorConsulta { get; private set; }

    public bool PuedeResponder(DateTimeOffset fechaActual)
    {
        return EstaCompleto &&
            FechaVencimiento is not null &&
            FechaVencimiento > fechaActual;
    }

    public void RegistrarConsulta(
        DateTimeOffset fechaConsulta,
        DateTimeOffset fechaActualizacionLocal,
        DateTimeOffset? fechaVencimiento,
        FuenteRespuesta fuente,
        Guid? ultimoMovimientoDetectadoId,
        bool estaCompleto,
        bool tieneDatosParciales,
        string? ultimoErrorConsulta)
    {
        FechaUltimaConsultaGdeba = fechaConsulta;
        FechaUltimaActualizacionLocal = fechaActualizacionLocal;
        FechaVencimiento = fechaVencimiento;
        FuenteUltimaRespuesta = fuente;
        UltimoMovimientoDetectadoId = ultimoMovimientoDetectadoId;
        EstaCompleto = estaCompleto;
        TieneDatosParciales = tieneDatosParciales;
        UltimoErrorConsulta = Normalizar(ultimoErrorConsulta);
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
