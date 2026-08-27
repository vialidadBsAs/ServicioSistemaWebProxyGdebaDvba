using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed partial class PerfilUsuario : DomainEntity
{
    private readonly List<SeguimientoExpediente> _seguimientos = new();

    private PerfilUsuario()
    {
    }

    public PerfilUsuario(string usuarioInstitucional)
    {
        UsuarioInstitucional = NormalizarRequerido(usuarioInstitucional, nameof(usuarioInstitucional));
    }

    // Identidad del token institucional DVBA-Auth; el proxy no administra credenciales.
    public string UsuarioInstitucional { get; private set; } = string.Empty;

    // Usuario GDEBA personal que viaja como parametro en las consultas SOAP interactivas.
    public string? UsuarioGdeba { get; private set; }

    public IReadOnlyCollection<SeguimientoExpediente> Seguimientos => _seguimientos;

    private static string NormalizarRequerido(string value, string paramName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", paramName)
            : value.Trim();
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
