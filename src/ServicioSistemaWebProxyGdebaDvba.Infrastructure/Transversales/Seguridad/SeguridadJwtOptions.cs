namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Seguridad;

public sealed class SeguridadJwtOptions
{
    public const string SectionName = "Seguridad:Jwt";

    public string ApplicationName { get; set; } = string.Empty;
    public string AdministratorRole { get; set; } = string.Empty;
    public string ValidIssuer { get; set; } = string.Empty;
    public string ValidAudience { get; set; } = string.Empty;
    public int ClockSkewSeconds { get; set; } = 60;
}
