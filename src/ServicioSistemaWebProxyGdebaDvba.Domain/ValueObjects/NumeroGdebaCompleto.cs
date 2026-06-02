namespace ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

public sealed record NumeroGdebaCompleto
{
    private NumeroGdebaCompleto(
        string valor,
        string tipo,
        int anio,
        long numero,
        string sistema,
        string reparticion)
    {
        Valor = valor;
        Tipo = tipo;
        Anio = anio;
        Numero = numero;
        Sistema = sistema;
        Reparticion = reparticion;
    }

    public string Valor { get; }

    public string Tipo { get; }

    public int Anio { get; }

    public long Numero { get; }

    public string Sistema { get; }

    public string Reparticion { get; }

    public static NumeroGdebaCompleto Create(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("El numero GDEBA completo es requerido.", nameof(valor));
        }

        var normalized = Normalize(valor);
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 5)
        {
            throw new ArgumentException(
                "El numero completo de expediente debe tener formato tipo-anio-numero-sistema-reparticion.",
                nameof(valor));
        }

        if (!int.TryParse(parts[1], out var expAnio))
        {
            throw new ArgumentException("El anio del expediente no tiene un formato valido.", nameof(valor));
        }

        if (!long.TryParse(parts[2], out var expNumero))
        {
            throw new ArgumentException("El numero del expediente no tiene un formato valido.", nameof(valor));
        }

        return new NumeroGdebaCompleto(
            normalized,
            parts[0].ToUpperInvariant(),
            expAnio,
            expNumero,
            parts[3].ToUpperInvariant(),
            parts[4].ToUpperInvariant());
    }

    public override string ToString() => Valor;

    private static string Normalize(string valor)
    {
        return string.Join(
            '-',
            valor.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
