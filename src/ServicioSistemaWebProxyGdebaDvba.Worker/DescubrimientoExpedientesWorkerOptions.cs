namespace ServicioSistemaWebProxyGdebaDvba.Worker;

public sealed class DescubrimientoExpedientesWorkerOptions
{
    public const string SectionName = "Workers:DescubrimientoExpedientes";
    public bool Enabled { get; set; }
    public int HoraInicioLocal { get; set; } = 0;
    public int HoraFinLocal { get; set; } = 6;
    public int IntervalMinutes { get; set; } = 30;
    public int CupoReservaDiaria { get; set; } = 20;
    public int ConsultasVaciasParaPausa { get; set; } = 3;
    public int DiasPausaSinResultados { get; set; } = 7;
    public string ServicioCuota { get; set; } = "ws_gdeba_consultaExpediente";
    public string MetodoCuota { get; set; } = "buscarDatosExpedientePorCodigosTrata";
}
