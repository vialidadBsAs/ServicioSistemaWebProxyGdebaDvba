namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public sealed class GdebaConfigurationException : Exception
{
    public GdebaConfigurationException(string publicTitle, string publicMessage, string? technicalMessage = null)
        : base(technicalMessage ?? publicMessage)
    {
        PublicTitle = publicTitle;
        PublicMessage = publicMessage;
    }

    public string PublicTitle { get; }

    public string PublicMessage { get; }
}
