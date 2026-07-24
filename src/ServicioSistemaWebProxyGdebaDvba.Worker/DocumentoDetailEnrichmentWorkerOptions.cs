namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public sealed class DocumentoDetailEnrichmentWorkerOptions
{
    public const string SectionName = "Workers:DocumentoDetailEnrichment";

    public const string ServicioCuotaDefault = "ws_gdeba_consultaDocumento";

    public const string MetodoCuotaDefault = "buscarDetallePorNumero";

    public bool Enabled { get; set; }

    public int IntervalMinutes { get; set; } = 30;

    public int BatchSize { get; set; } = 20;

    public bool RunOnStartup { get; set; } = true;

    public int VentanaInicioHoraLocal { get; set; } = 20;

    public int VentanaFinHoraLocal { get; set; } = 6;

    public int CupoReservaDiaria { get; set; } = 10;

    public string ServicioCuota { get; set; } = DocumentoDetailEnrichmentWorkerOptions.ServicioCuotaDefault;

    public string MetodoCuota { get; set; } = DocumentoDetailEnrichmentWorkerOptions.MetodoCuotaDefault;
}
