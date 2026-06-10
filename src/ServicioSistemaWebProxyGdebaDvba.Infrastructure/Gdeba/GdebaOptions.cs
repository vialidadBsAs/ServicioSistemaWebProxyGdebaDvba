namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class GdebaOptions
{
    public const string SectionName = "Gdeba";

    public string CurrentEnvironment { get; set; } = GdebaEnvironmentNames.Hml;

    public string GatewayMode { get; set; } = GdebaGatewayModes.Fake;

    public GdebaSoapContractsOptions SoapContracts { get; set; } = new();

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
    public Dictionary<string, SoapServiceOptions> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SoapServiceOptions
{
    public string Wsdl { get; set; } = string.Empty;
}

public sealed class GdebaSoapContractsOptions
{
    public string EnvelopeNamespace { get; set; } = "http://schemas.xmlsoap.org/soap/envelope/";

    public Dictionary<string, GdebaSoapServiceContractOptions> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class GdebaSoapServiceContractOptions
{
    public string Namespace { get; set; } = string.Empty;

    public Dictionary<string, GdebaSoapOperationContractOptions> Operations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class GdebaSoapOperationContractOptions
{
    public string SoapAction { get; set; } = string.Empty;
}

public static class GdebaSoapServiceNames
{
    public const string ConsultaExpediente = "ConsultaExpediente";

    public const string ConsultaDocumento = "ConsultaDocumento";

    public const string Tratas = "Tratas";

    public const string ConsultaTipoDocumento = "ConsultaTipoDocumento";

    public const string ConsultaEstadoPaseExpediente = "ConsultaEstadoPaseExpediente";
}
