using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class TipoDocumentoGdeba : DomainEntity
{
    private TipoDocumentoGdeba()
    {
    }

    public TipoDocumentoGdeba(string codigo, string nombre)
    {
        Codigo = string.IsNullOrWhiteSpace(codigo)
            ? throw new ArgumentException("El codigo del tipo documental es requerido.", nameof(codigo))
            : codigo.Trim().ToUpperInvariant();
        Nombre = string.IsNullOrWhiteSpace(nombre)
            ? throw new ArgumentException("El nombre del tipo documental es requerido.", nameof(nombre))
            : nombre.Trim();
        Activo = true;
    }

    public string Codigo { get; private set; } = string.Empty;

    public string? CodigoTipoDocumentoGdeba { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public string? Familia { get; private set; }

    public string? TipoProduccion { get; private set; }

    public string? Estado { get; private set; }

    public bool? EsAutomatica { get; private set; }

    public bool? EsComunicable { get; private set; }

    public bool? EsConfidencial { get; private set; }

    public bool? EsEmbebido { get; private set; }

    public bool? EsEspecial { get; private set; }

    public bool? EsFirmaConjunta { get; private set; }

    public bool? EsFirmaExterna { get; private set; }

    public bool? EsManual { get; private set; }

    public bool? EsNotificable { get; private set; }

    public bool? TieneTemplate { get; private set; }

    public bool? TieneToken { get; private set; }

    public bool EsResolucion { get; private set; }

    public bool Activo { get; private set; }

    public void ActualizarMetadata(
        string nombre,
        string? codigoTipoDocumentoGdeba,
        string? descripcion,
        string? familia,
        string? tipoProduccion,
        string? estado,
        bool esResolucion,
        bool activo)
    {
        ActualizarMetadata(
            nombre,
            codigoTipoDocumentoGdeba,
            descripcion,
            familia,
            tipoProduccion,
            estado,
            esAutomatica: null,
            esComunicable: null,
            esConfidencial: null,
            esEmbebido: null,
            esEspecial: null,
            esFirmaConjunta: null,
            esFirmaExterna: null,
            esManual: null,
            esNotificable: null,
            tieneTemplate: null,
            tieneToken: null,
            esResolucion,
            activo);
    }

    public void ActualizarMetadata(
        string nombre,
        string? codigoTipoDocumentoGdeba,
        string? descripcion,
        string? familia,
        string? tipoProduccion,
        string? estado,
        bool? esAutomatica,
        bool? esComunicable,
        bool? esConfidencial,
        bool? esEmbebido,
        bool? esEspecial,
        bool? esFirmaConjunta,
        bool? esFirmaExterna,
        bool? esManual,
        bool? esNotificable,
        bool? tieneTemplate,
        bool? tieneToken,
        bool esResolucion,
        bool activo)
    {
        Nombre = string.IsNullOrWhiteSpace(nombre)
            ? throw new ArgumentException("El nombre del tipo documental es requerido.", nameof(nombre))
            : nombre.Trim();
        CodigoTipoDocumentoGdeba = Normalizar(codigoTipoDocumentoGdeba);
        Descripcion = Normalizar(descripcion);
        Familia = Normalizar(familia);
        TipoProduccion = Normalizar(tipoProduccion);
        Estado = Normalizar(estado);
        EsAutomatica = esAutomatica;
        EsComunicable = esComunicable;
        EsConfidencial = esConfidencial;
        EsEmbebido = esEmbebido;
        EsEspecial = esEspecial;
        EsFirmaConjunta = esFirmaConjunta;
        EsFirmaExterna = esFirmaExterna;
        EsManual = esManual;
        EsNotificable = esNotificable;
        TieneTemplate = tieneTemplate;
        TieneToken = tieneToken;
        EsResolucion = esResolucion;
        Activo = activo;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
