using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class DocumentoGdeba : DomainEntity
{
    private readonly List<ExpedienteDocumento> _expedientes = new();

    private DocumentoGdeba()
    {
    }

    public DocumentoGdeba(string numeroGdebaCompleto)
    {
        ActualizarNumeroCompleto(NumeroGdebaCompleto.Create(numeroGdebaCompleto));
    }

    public string GdebaNumeroCompleto { get; private set; } = string.Empty;

    public string GdebaTipo { get; private set; } = string.Empty;

    public int GdebaAnio { get; private set; }

    public long GdebaNumero { get; private set; }

    public string GdebaSistema { get; private set; } = string.Empty;

    public string GdebaReparticion { get; private set; } = string.Empty;

    public string? NumeroEspecial { get; private set; }

    public string? TipoDocumento { get; private set; }

    public string? Referencia { get; private set; }

    public DateTimeOffset? FechaCreacion { get; private set; }

    public string? UrlArchivo { get; private set; }

    public bool? PuedeVerDocumento { get; private set; }

    public DocumentoCacheControl? CacheControl { get; private set; }

    public DocumentoArchivoLocal? ArchivoLocal { get; private set; }

    public IReadOnlyCollection<ExpedienteDocumento> Expedientes => _expedientes;

    public void ActualizarNumeroCompleto(NumeroGdebaCompleto numeroGdebaCompleto)
    {
        GdebaNumeroCompleto = numeroGdebaCompleto.Valor;
        GdebaTipo = numeroGdebaCompleto.Tipo;
        GdebaAnio = numeroGdebaCompleto.Anio;
        GdebaNumero = numeroGdebaCompleto.Numero;
        GdebaSistema = numeroGdebaCompleto.Sistema;
        GdebaReparticion = numeroGdebaCompleto.Reparticion;
    }

    public void ActualizarMetadata(
        string? numeroEspecial,
        string? tipoDocumento,
        string? referencia,
        DateTimeOffset? fechaCreacion,
        string? urlArchivo,
        bool? puedeVerDocumento)
    {
        NumeroEspecial = Normalizar(numeroEspecial);
        TipoDocumento = Normalizar(tipoDocumento);
        Referencia = Normalizar(referencia);
        FechaCreacion = fechaCreacion;
        UrlArchivo = Normalizar(urlArchivo);
        PuedeVerDocumento = puedeVerDocumento;
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
