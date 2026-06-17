namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria;

public sealed class AuditoriaOptions
{
    public const string SectionName = "Auditoria";

    public string Mode { get; set; } = AuditoriaModes.Persisted;
}

public static class AuditoriaModes
{
    public const string InMemory = "InMemory";

    public const string Persisted = "Persisted";
}
