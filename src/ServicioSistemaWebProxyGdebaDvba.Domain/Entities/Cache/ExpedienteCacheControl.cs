using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class ExpedienteCacheControl : DomainEntity
{
    private ExpedienteCacheControl()
    {
    }

    public ExpedienteCacheControl(Guid expedienteId, DateTimeOffset fechaPrimeraDeteccion)
    {
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        FechaPrimeraDeteccion = fechaPrimeraDeteccion;
    }

    public Guid ExpedienteId { get; private set; }

    public Expediente Expediente { get; private set; } = null!;

    public DateTimeOffset FechaPrimeraDeteccion { get; private set; }

    public DateTimeOffset? FechaUltimaConsultaGdeba { get; private set; }

    public DateTimeOffset? FechaUltimaActualizacionLocal { get; private set; }

    public DateTimeOffset? FechaVencimiento { get; private set; }

    public FuenteRespuesta? FuenteUltimaRespuesta { get; private set; }

    public bool EstaCompleto { get; private set; }

    public string? UltimoErrorConsulta { get; private set; }

    // Se sellan solo cuando una consulta detecta informacion nueva, no en cada refresco; son la base institucional de los badges de seguimiento (la global para las listas, las especificas para cada coleccion del detalle).
    public DateTimeOffset? FechaUltimaNovedad { get; private set; }

    public DateTimeOffset? FechaUltimaNovedadCabecera { get; private set; }

    public DateTimeOffset? FechaUltimaNovedadMovimientos { get; private set; }

    public DateTimeOffset? FechaUltimaNovedadDocumentos { get; private set; }

    public DateTimeOffset? FechaUltimaNovedadAdjuntos { get; private set; }

    public void RegistrarNovedades(DateTimeOffset fechaNovedad, bool cabecera, bool movimientos, bool documentos, bool adjuntos)
    {
        if (!cabecera && !movimientos && !documentos && !adjuntos)
        {
            return;
        }

        MarcarComoModificada();
        FechaUltimaNovedad = fechaNovedad;
        if (cabecera) FechaUltimaNovedadCabecera = fechaNovedad;
        if (movimientos) FechaUltimaNovedadMovimientos = fechaNovedad;
        if (documentos) FechaUltimaNovedadDocumentos = fechaNovedad;
        if (adjuntos) FechaUltimaNovedadAdjuntos = fechaNovedad;
    }

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
        bool estaCompleto,
        string? ultimoErrorConsulta)
    {
        MarcarComoModificada();
        FechaUltimaConsultaGdeba = fechaConsulta;
        FechaUltimaActualizacionLocal = fechaActualizacionLocal;
        FechaVencimiento = fechaVencimiento;
        FuenteUltimaRespuesta = fuente;
        EstaCompleto = estaCompleto;
        UltimoErrorConsulta = Normalizar(ultimoErrorConsulta);
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
