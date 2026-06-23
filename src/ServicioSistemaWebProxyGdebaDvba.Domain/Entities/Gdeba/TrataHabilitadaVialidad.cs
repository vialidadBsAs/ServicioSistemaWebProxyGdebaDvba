using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class TrataHabilitadaVialidad : DomainEntity
{
    private readonly List<Expediente> _expedientes = new();

    private TrataHabilitadaVialidad()
    {
    }

    public TrataHabilitadaVialidad(string codigoTrata, string codigoOrganismo, string codigoReparticion)
    {
        CodigoTrata = NormalizarRequerido(codigoTrata, nameof(codigoTrata));
        CodigoOrganismo = NormalizarRequerido(codigoOrganismo, nameof(codigoOrganismo));
        CodigoReparticion = NormalizarRequerido(codigoReparticion, nameof(codigoReparticion));
    }

    public string CodigoTrata { get; private set; } = string.Empty;

    public string? DescripcionTrata { get; private set; }

    public string? EstadoTrata { get; private set; }

    public bool? ReservaTotal { get; private set; }

    public bool? CaratulaVariable { get; private set; }

    public string? AcronimoGedo { get; private set; }

    public bool? EsAutomatica { get; private set; }

    public bool? EsTrataManual { get; private set; }

    public string? IdTrataGdeba { get; private set; }

    public string? TipoReservaDescripcion { get; private set; }

    public string? TipoReservaId { get; private set; }

    public string? TipoReservaDescripcionTipoReserva { get; private set; }

    public string CodigoReparticion { get; private set; } = string.Empty;

    public string? NombreReparticion { get; private set; }

    public string CodigoOrganismo { get; private set; } = string.Empty;

    public string? NombreOrganismo { get; private set; }

    public bool? PermisoCaratulacion { get; private set; }

    public bool? PermisoReserva { get; private set; }

    public TrataCacheControl? CacheControl { get; private set; }

    public IReadOnlyCollection<Expediente> Expedientes => _expedientes;

    public void ActualizarDatos(
        string? descripcionTrata,
        string? estadoTrata,
        bool? reservaTotal,
        bool? caratulaVariable,
        string? nombreReparticion,
        string? nombreOrganismo,
        bool? permisoCaratulacion,
        bool? permisoReserva)
    {
        MarcarComoModificada();
        DescripcionTrata = Normalizar(descripcionTrata);
        EstadoTrata = Normalizar(estadoTrata);
        ReservaTotal = reservaTotal;
        CaratulaVariable = caratulaVariable;
        NombreReparticion = Normalizar(nombreReparticion);
        NombreOrganismo = Normalizar(nombreOrganismo);
        PermisoCaratulacion = permisoCaratulacion;
        PermisoReserva = permisoReserva;
    }

    public void ActualizarDatosGdeba(
        string? descripcionTrata,
        string? acronimoGedo,
        bool? esAutomatica,
        bool? esTrataManual,
        string? estadoTrata,
        string? idTrataGdeba,
        string? tipoReservaDescripcion,
        string? tipoReservaId,
        string? tipoReservaDescripcionTipoReserva)
    {
        MarcarComoModificada();
        DescripcionTrata = Normalizar(descripcionTrata) ?? DescripcionTrata;
        AcronimoGedo = Normalizar(acronimoGedo);
        EsAutomatica = esAutomatica;
        EsTrataManual = esTrataManual;
        EstadoTrata = Normalizar(estadoTrata) ?? EstadoTrata;
        IdTrataGdeba = Normalizar(idTrataGdeba);
        TipoReservaDescripcion = Normalizar(tipoReservaDescripcion);
        TipoReservaId = Normalizar(tipoReservaId);
        TipoReservaDescripcionTipoReserva = Normalizar(tipoReservaDescripcionTipoReserva);
    }

    private static string NormalizarRequerido(string value, string paramName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", paramName)
            : value.Trim();
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
