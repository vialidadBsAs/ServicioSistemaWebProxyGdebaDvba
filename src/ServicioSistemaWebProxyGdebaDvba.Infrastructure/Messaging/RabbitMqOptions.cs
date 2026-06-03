namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string Host { get; set; } = "localhost";

    public ushort Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string CachearDetalleExpedienteQueue { get; set; } = "gdeba.cachear-detalle-expediente";
}
