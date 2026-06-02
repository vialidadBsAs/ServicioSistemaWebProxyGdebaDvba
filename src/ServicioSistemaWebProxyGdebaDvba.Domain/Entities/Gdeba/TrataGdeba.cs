using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class TrataGdeba : DomainEntity
{
    private readonly List<Expediente> _expedientes = new();

    private TrataGdeba()
    {
    }

    public TrataGdeba(string codigo)
    {
        Codigo = string.IsNullOrWhiteSpace(codigo)
            ? throw new ArgumentException("El codigo de trata es requerido.", nameof(codigo))
            : codigo.Trim();
    }

    public string Codigo { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public string? AcronimoGedo { get; private set; }

    public bool? EsAutomatica { get; private set; }

    public bool? EsTrataManual { get; private set; }

    public string? Estado { get; private set; }

    public string? IdTrataGdeba { get; private set; }

    public string? TipoReservaDescripcion { get; private set; }

    public string? TipoReservaId { get; private set; }

    public string? TipoReservaDescripcionTipoReserva { get; private set; }

    public TrataCacheControl? CacheControl { get; private set; }

    public IReadOnlyCollection<Expediente> Expedientes => _expedientes;

    public void ActualizarDatos(
        string? descripcion,
        string? acronimoGedo,
        bool? esAutomatica,
        bool? esTrataManual,
        string? estado,
        string? idTrataGdeba,
        string? tipoReservaDescripcion,
        string? tipoReservaId,
        string? tipoReservaDescripcionTipoReserva)
    {
        Descripcion = Normalizar(descripcion);
        AcronimoGedo = Normalizar(acronimoGedo);
        EsAutomatica = esAutomatica;
        EsTrataManual = esTrataManual;
        Estado = Normalizar(estado);
        IdTrataGdeba = Normalizar(idTrataGdeba);
        TipoReservaDescripcion = Normalizar(tipoReservaDescripcion);
        TipoReservaId = Normalizar(tipoReservaId);
        TipoReservaDescripcionTipoReserva = Normalizar(tipoReservaDescripcionTipoReserva);
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
