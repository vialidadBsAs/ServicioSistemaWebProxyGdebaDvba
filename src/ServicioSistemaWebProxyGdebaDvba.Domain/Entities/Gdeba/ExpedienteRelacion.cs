using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class ExpedienteRelacion : DomainEntity
{
    private ExpedienteRelacion()
    {
    }

    public ExpedienteRelacion(
        Guid expedienteOrigenId,
        string numeroExpedienteRelacionado,
        TipoRelacionExpediente tipoRelacion,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        MarcarComoAgregada();
        ExpedienteOrigenId = expedienteOrigenId == Guid.Empty
            ? throw new ArgumentException("El expediente origen es requerido.", nameof(expedienteOrigenId))
            : expedienteOrigenId;
        NumeroExpedienteRelacionado = string.IsNullOrWhiteSpace(numeroExpedienteRelacionado)
            ? throw new ArgumentException("El numero de expediente relacionado es requerido.", nameof(numeroExpedienteRelacionado))
            : numeroExpedienteRelacionado.Trim();
        TipoRelacion = tipoRelacion;
        FuenteDeteccion = fuenteDeteccion;
        FechaPrimeraDeteccion = fechaDeteccion;
        FechaUltimaDeteccion = fechaDeteccion;
    }

    public Guid ExpedienteOrigenId { get; private set; }

    public Expediente ExpedienteOrigen { get; private set; } = null!;

    public Guid? ExpedienteRelacionadoId { get; private set; }

    public Expediente? ExpedienteRelacionado { get; private set; }

    public string NumeroExpedienteRelacionado { get; private set; } = string.Empty;

    public TipoRelacionExpediente TipoRelacion { get; private set; }

    public string? CodigoTrataRelacionado { get; private set; }

    public string? DescripcionTrataRelacionado { get; private set; }

    public DateTimeOffset? FechaRelacion { get; private set; }

    public string? UsuarioRelacion { get; private set; }

    public bool? EsCabecera { get; private set; }

    public FuenteDeteccionGdeba FuenteDeteccion { get; private set; }

    public DateTimeOffset FechaPrimeraDeteccion { get; private set; }

    public DateTimeOffset FechaUltimaDeteccion { get; private set; }

    public void RegistrarDeteccion(
        Guid? expedienteRelacionadoId,
        string? codigoTrataRelacionado,
        string? descripcionTrataRelacionado,
        DateTimeOffset? fechaRelacion,
        string? usuarioRelacion,
        bool? esCabecera,
        FuenteDeteccionGdeba fuenteDeteccion,
        DateTimeOffset fechaDeteccion)
    {
        MarcarComoModificada();
        ExpedienteRelacionadoId = expedienteRelacionadoId ?? ExpedienteRelacionadoId;
        CodigoTrataRelacionado = Normalizar(codigoTrataRelacionado) ?? CodigoTrataRelacionado;
        DescripcionTrataRelacionado = Normalizar(descripcionTrataRelacionado) ?? DescripcionTrataRelacionado;
        FechaRelacion = fechaRelacion ?? FechaRelacion;
        UsuarioRelacion = Normalizar(usuarioRelacion) ?? UsuarioRelacion;
        EsCabecera = esCabecera ?? EsCabecera;
        FuenteDeteccion = fuenteDeteccion;
        FechaUltimaDeteccion = fechaDeteccion;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
