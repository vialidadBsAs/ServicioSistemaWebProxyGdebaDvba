using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class Expediente : DomainEntity
{
    private readonly List<MovimientoExpediente> _movimientos = new();
    private readonly List<ExpedienteDocumento> _documentos = new();
    private readonly List<ExpedienteRelacion> _relaciones = new();
    private readonly List<ArchivoAdjuntoExpediente> _archivosAdjuntos = new();

    private Expediente()
    {
    }

    public Expediente(string numeroGdebaCompleto)
    {
        ActualizarNumeroCompleto(
            NumeroGdebaCompleto.Create(numeroGdebaCompleto));
    }

    public string GdebaNumeroCompleto { get; private set; } = string.Empty;

    public string GdebaTipo { get; private set; } = string.Empty;

    public int GdebaAnio { get; private set; }

    public long GdebaNumero { get; private set; }

    public string GdebaSistema { get; private set; } = string.Empty;

    public string GdebaReparticion { get; private set; } = string.Empty;

    public Guid? TrataId { get; private set; }

    public TrataGdeba? Trata { get; private set; }

    public string? EstadoActual { get; private set; }

    public string? SistemaOrigen { get; private set; }

    public string? DescripcionTramite { get; private set; }

    public DateTimeOffset? FechaCaratulacion { get; private set; }

    public string? UsuarioCaratulador { get; private set; }

    public string? UsuarioDestino { get; private set; }

    public string? SectorDestino { get; private set; }

    public string? ReparticionActual { get; private set; }

    public ExpedienteCacheControl? CacheControl { get; private set; }

    public HistorialExpedienteCacheControl? HistorialCacheControl { get; private set; }

    public IReadOnlyCollection<MovimientoExpediente> Movimientos => _movimientos;

    public IReadOnlyCollection<ExpedienteDocumento> Documentos => _documentos;

    public IReadOnlyCollection<ExpedienteRelacion> Relaciones => _relaciones;

    public IReadOnlyCollection<ArchivoAdjuntoExpediente> ArchivosAdjuntos => _archivosAdjuntos;

    public void ActualizarNumeroCompleto(NumeroGdebaCompleto numeroGdebaCompleto)
    {
        GdebaNumeroCompleto = numeroGdebaCompleto.Valor;
        GdebaTipo = numeroGdebaCompleto.Tipo;
        GdebaAnio = numeroGdebaCompleto.Anio;
        GdebaNumero = numeroGdebaCompleto.Numero;
        GdebaSistema = numeroGdebaCompleto.Sistema;
        GdebaReparticion = numeroGdebaCompleto.Reparticion;
    }

    public void ActualizarCabecera(
        Guid? trataId,
        string? estadoActual,
        string? sistemaOrigen,
        string? descripcionTramite,
        DateTimeOffset? fechaCaratulacion,
        string? usuarioCaratulador,
        string? usuarioDestino,
        string? sectorDestino,
        string? reparticionActual)
    {
        TrataId = trataId;
        EstadoActual = Normalizar(estadoActual);
        SistemaOrigen = Normalizar(sistemaOrigen);
        DescripcionTramite = Normalizar(descripcionTramite);
        FechaCaratulacion = fechaCaratulacion;
        UsuarioCaratulador = Normalizar(usuarioCaratulador);
        UsuarioDestino = Normalizar(usuarioDestino);
        SectorDestino = Normalizar(sectorDestino);
        ReparticionActual = Normalizar(reparticionActual);
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
