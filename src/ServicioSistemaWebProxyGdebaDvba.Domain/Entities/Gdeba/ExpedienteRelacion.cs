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
        // Solo escribe si algun dato de la relacion cambio; una deteccion repetida sin novedades no toca la fila (una relacion recien
        // creada nace sin datos, asi que lo informado siempre difiere). Fuente y fecha de ultima deteccion quedan como la ultima
        // deteccion que aporto datos.
        Guid? expedienteRelacionadoInformado = expedienteRelacionadoId ?? ExpedienteRelacionadoId;
        string? codigoTrataInformado = Normalizar(codigoTrataRelacionado) ?? CodigoTrataRelacionado;
        string? descripcionTrataInformada = Normalizar(descripcionTrataRelacionado) ?? DescripcionTrataRelacionado;
        DateTimeOffset? fechaRelacionInformada = fechaRelacion ?? FechaRelacion;
        string? usuarioRelacionInformado = Normalizar(usuarioRelacion) ?? UsuarioRelacion;
        bool? esCabeceraInformado = esCabecera ?? EsCabecera;
        bool hayCambios = expedienteRelacionadoInformado != ExpedienteRelacionadoId ||
            !string.Equals(codigoTrataInformado, CodigoTrataRelacionado, StringComparison.Ordinal) ||
            !string.Equals(descripcionTrataInformada, DescripcionTrataRelacionado, StringComparison.Ordinal) ||
            fechaRelacionInformada != FechaRelacion ||
            !string.Equals(usuarioRelacionInformado, UsuarioRelacion, StringComparison.Ordinal) ||
            esCabeceraInformado != EsCabecera;
        if (!hayCambios)
        {
            return;
        }

        MarcarComoModificada();
        ExpedienteRelacionadoId = expedienteRelacionadoInformado;
        CodigoTrataRelacionado = codigoTrataInformado;
        DescripcionTrataRelacionado = descripcionTrataInformada;
        FechaRelacion = fechaRelacionInformada;
        UsuarioRelacion = usuarioRelacionInformado;
        EsCabecera = esCabeceraInformado;
        FuenteDeteccion = fuenteDeteccion;
        FechaUltimaDeteccion = fechaDeteccion;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
