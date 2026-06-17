using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class AplicacionConsumidora : DomainEntity
{
    private readonly List<RegistroAuditoria> _registrosAuditoria = new();

    private AplicacionConsumidora()
    {
    }

    public AplicacionConsumidora(string codigo, string nombre)
    {
        Codigo = string.IsNullOrWhiteSpace(codigo) ? throw new ArgumentException("El codigo es requerido.", nameof(codigo)) : codigo.Trim();
        Nombre = string.IsNullOrWhiteSpace(nombre) ? throw new ArgumentException("El nombre es requerido.", nameof(nombre)) : nombre.Trim();
        Activa = true;
    }

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public bool Activa { get; private set; }

    public IReadOnlyCollection<RegistroAuditoria> RegistrosAuditoria => _registrosAuditoria;
}
