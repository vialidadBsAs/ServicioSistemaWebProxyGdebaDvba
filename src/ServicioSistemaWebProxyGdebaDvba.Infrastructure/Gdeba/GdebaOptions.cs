namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class GdebaOptions
{
    public const string SectionName = "Gdeba";

    public string CurrentEnvironment { get; set; } = GdebaEnvironmentNames.Hml;

    public string GatewayMode { get; set; } = GdebaGatewayModes.Fake;

    public Dictionary<string, GdebaEnvironmentOptions> Environments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class GdebaGatewayModes
{
    public const string Fake = "Fake";
    public const string Soap = "Soap";
    public const string Rest = "Rest";
}

public static class GdebaEnvironmentNames
{
    public const string Hml = "HML";
    public const string Prod = "PROD";
}

public sealed class GdebaEnvironmentOptions
{
    public JwtOptions Jwt { get; set; } = new();

    public SoapOptions Soap { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class SoapOptions
{
    public string ConsultaExpedienteWsdl { get; set; } = string.Empty;

    public string ConsultaDocumentoWsdl { get; set; } = string.Empty;

    public string TratasWsdl { get; set; } = string.Empty;

    public string ConsultaTipoDocumentoWsdl { get; set; } = string.Empty;

    public string ConsultaEstadoPaseExpedienteWsdl { get; set; } = string.Empty;
}
