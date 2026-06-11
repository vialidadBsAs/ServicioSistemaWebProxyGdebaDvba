using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class DocumentoGdeba : DomainEntity
{
    private readonly List<ExpedienteDocumento> _expedientes = new();

    private DocumentoGdeba()
    {
    }

    public DocumentoGdeba(string numeroActuacionCompleto)
    {
        ActualizarNumeroActuacion(NumeroGdebaCompleto.Create(numeroActuacionCompleto));
    }

    public string NumeroActuacionCompleto { get; private set; } = string.Empty;

    public string ActuacionTipoCodigo { get; private set; } = string.Empty;

    public int ActuacionAnio { get; private set; }

    public long ActuacionNumero { get; private set; }

    public string ActuacionSistema { get; private set; } = string.Empty;

    public string ActuacionReparticion { get; private set; } = string.Empty;

    public string? NumeroEspecialCompleto { get; private set; }

    public string? EspecialTipoCodigo { get; private set; }

    public int? EspecialAnio { get; private set; }

    public long? EspecialNumero { get; private set; }

    public string? EspecialSistema { get; private set; }

    public string? EspecialReparticion { get; private set; }

    public string? TipoDocumentoCodigo { get; private set; }

    public string? TipoDocumentoNombre { get; private set; }

    public string? TipoDocumentoDescripcion { get; private set; }

    public string? Referencia { get; private set; }

    public DateTimeOffset? FechaCreacion { get; private set; }

    public bool MetadataCompleta { get; private set; }

    public DateTimeOffset? FechaUltimoEnriquecimiento { get; private set; }

    public string? UrlArchivo { get; private set; }

    public bool? PuedeVerDocumento { get; private set; }

    public DocumentoCacheControl? CacheControl { get; private set; }

    public DocumentoArchivoLocal? ArchivoLocal { get; private set; }

    public IReadOnlyCollection<ExpedienteDocumento> Expedientes => _expedientes;

    public void ActualizarNumeroActuacion(NumeroGdebaCompleto numeroActuacion)
    {
        MarcarComoModificada();
        NumeroActuacionCompleto = numeroActuacion.Valor;
        ActuacionTipoCodigo = numeroActuacion.Tipo;
        ActuacionAnio = numeroActuacion.Anio;
        ActuacionNumero = numeroActuacion.Numero;
        ActuacionSistema = numeroActuacion.Sistema;
        ActuacionReparticion = numeroActuacion.Reparticion;
    }

    public void ActualizarMetadata(
        string? numeroEspecial,
        string? tipoDocumento,
        string? referencia,
        DateTimeOffset? fechaCreacion,
        string? urlArchivo,
        bool? puedeVerDocumento)
    {
        ActualizarMetadata(
            numeroEspecial,
            tipoDocumentoCodigo: tipoDocumento,
            tipoDocumentoNombre: null,
            tipoDocumentoDescripcion: null,
            referencia,
            fechaCreacion,
            urlArchivo,
            puedeVerDocumento,
            fechaEnriquecimiento: null,
            metadataCompleta: false);
    }

    public void ActualizarMetadata(
        string? numeroEspecial,
        string? tipoDocumentoCodigo,
        string? tipoDocumentoNombre,
        string? tipoDocumentoDescripcion,
        string? referencia,
        DateTimeOffset? fechaCreacion,
        string? urlArchivo,
        bool? puedeVerDocumento,
        DateTimeOffset? fechaEnriquecimiento,
        bool metadataCompleta)
    {
        MarcarComoModificada();
        ActualizarNumeroEspecial(numeroEspecial);
        TipoDocumentoCodigo = Normalizar(tipoDocumentoCodigo);
        TipoDocumentoNombre = Normalizar(tipoDocumentoNombre);
        TipoDocumentoDescripcion = Normalizar(tipoDocumentoDescripcion);
        Referencia = Normalizar(referencia);
        FechaCreacion = fechaCreacion;
        UrlArchivo = Normalizar(urlArchivo);
        PuedeVerDocumento = puedeVerDocumento;
        MetadataCompleta = metadataCompleta;
        FechaUltimoEnriquecimiento = fechaEnriquecimiento;
    }

    private void ActualizarNumeroEspecial(string? numeroEspecial)
    {
        NumeroEspecialCompleto = Normalizar(numeroEspecial);

        if (NumeroEspecialCompleto is null)
        {
            EspecialTipoCodigo = null;
            EspecialAnio = null;
            EspecialNumero = null;
            EspecialSistema = null;
            EspecialReparticion = null;
            return;
        }

        var numeroEspecialParseado = NumeroGdebaCompleto.Create(NumeroEspecialCompleto);
        EspecialTipoCodigo = numeroEspecialParseado.Tipo;
        EspecialAnio = numeroEspecialParseado.Anio;
        EspecialNumero = numeroEspecialParseado.Numero;
        EspecialSistema = numeroEspecialParseado.Sistema;
        EspecialReparticion = numeroEspecialParseado.Reparticion;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
