using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class TemaExpediente : DomainEntity
{
    private readonly List<TemaExpedienteTrata> _tratas = new();

    private TemaExpediente()
    {
    }

    public TemaExpediente(string codigo, string nombre, string usuarioPropietario, string? descripcion = null)
    {
        Codigo = NormalizarRequerido(codigo, nameof(codigo)).ToUpperInvariant();
        Nombre = NormalizarRequerido(nombre, nameof(nombre));
        UsuarioPropietario = NormalizarRequerido(usuarioPropietario, nameof(usuarioPropietario));
        Descripcion = Normalizar(descripcion);
    }

    public string Codigo { get; private set; } = string.Empty;

    // El tema es una facilidad de consulta personal: cada usuario ve y administra solo los suyos.
    public string UsuarioPropietario { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public IReadOnlyCollection<TemaExpedienteTrata> Tratas => _tratas;

    private static string NormalizarRequerido(string value, string paramName)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("El valor es requerido.", paramName) : value.Trim();
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
