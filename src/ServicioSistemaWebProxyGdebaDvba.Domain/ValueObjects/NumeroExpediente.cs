namespace ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

public sealed record NumeroExpediente
{
    private NumeroExpediente(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static NumeroExpediente Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El numero de expediente es requerido.", nameof(value));
        }

        return new NumeroExpediente(Normalize(value));
    }

    public override string ToString() => Value;

    private static string Normalize(string value) => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
