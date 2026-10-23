using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

/// <summary>
/// Catalogo local de reparticiones GDEBA: codigo (el acronimo que viaja en numeros, pases y documentos) y su descripcion.
/// No se carga a mano ni por catalogo externo: el proxy lo completa con lo que GDEBA informa en cada historial de pases,
/// donde cada pase trae codigo y descripcion de la reparticion de origen y de destino.
/// </summary>
public sealed class ReparticionGdeba : DomainEntity
{
    private ReparticionGdeba()
    {
    }

    public ReparticionGdeba(string codigo, string? descripcion)
    {
        Codigo = ReparticionGdeba.NormalizarCodigo(codigo);
        Descripcion = ReparticionGdeba.NormalizarDescripcion(descripcion);
    }

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

    internal static string NormalizarCodigo(string codigo)
    {
        return string.IsNullOrWhiteSpace(codigo)
            ? throw new ArgumentException("El codigo de reparticion es requerido.", nameof(codigo))
            : codigo.Trim().ToUpperInvariant();
    }

    internal static string? NormalizarDescripcion(string? descripcion)
    {
        return string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
    }
}
