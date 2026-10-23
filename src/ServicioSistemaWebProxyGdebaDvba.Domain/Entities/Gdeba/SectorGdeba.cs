using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

/// <summary>
/// Catalogo local de sectores GDEBA. El codigo de sector es propio de cada reparticion (DPTTDV existe dentro de DVMIYSPGP), por eso la
/// identidad es el par reparticion + sector. Se completa con lo que GDEBA informa en cada historial de pases.
/// </summary>
public sealed class SectorGdeba : DomainEntity
{
    private SectorGdeba()
    {
    }

    public SectorGdeba(string codigoReparticion, string codigo, string? descripcion)
    {
        CodigoReparticion = ReparticionGdeba.NormalizarCodigo(codigoReparticion);
        Codigo = string.IsNullOrWhiteSpace(codigo) ? throw new ArgumentException("El codigo de sector es requerido.", nameof(codigo)) : codigo.Trim().ToUpperInvariant();
        Descripcion = ReparticionGdeba.NormalizarDescripcion(descripcion);
    }

    public string CodigoReparticion { get; private set; } = string.Empty;
    public string Codigo { get; private set; } = string.Empty;
    public string? Descripcion { get; private set; }

    /// <summary>Toma la descripcion informada por GDEBA si es la primera o cambio; devuelve si hubo cambio, asi la fila no se escribe en vano.</summary>
    public bool ActualizarDescripcion(string? descripcion)
    {
        string? descripcionInformada = ReparticionGdeba.NormalizarDescripcion(descripcion);
        if (descripcionInformada is null || string.Equals(descripcionInformada, Descripcion, StringComparison.Ordinal))
        {
            return false;
        }

        Descripcion = descripcionInformada;
        this.MarcarComoModificada();
        return true;
    }
}
