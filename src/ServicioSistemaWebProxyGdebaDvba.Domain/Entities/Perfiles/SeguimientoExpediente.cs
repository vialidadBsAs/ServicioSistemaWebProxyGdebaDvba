using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class SeguimientoExpediente : DomainEntity
{
    private SeguimientoExpediente()
    {
    }

    public SeguimientoExpediente(Guid perfilUsuarioId, Guid expedienteId, DateTimeOffset fechaAgregado)
    {
        PerfilUsuarioId = perfilUsuarioId == Guid.Empty
            ? throw new ArgumentException("El perfil es requerido.", nameof(perfilUsuarioId))
            : perfilUsuarioId;
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        FechaAgregado = fechaAgregado;
        FechaUltimaVista = fechaAgregado;
    }

    public Guid PerfilUsuarioId { get; private set; }

    public PerfilUsuario PerfilUsuario { get; private set; } = null!;

    public Guid ExpedienteId { get; private set; }

    public Expediente Expediente { get; private set; } = null!;

    public DateTimeOffset FechaAgregado { get; private set; }

    // El badge de novedades es personal: hay novedad si la ultima novedad detectada del expediente es posterior a esta vista.
    public DateTimeOffset FechaUltimaVista { get; private set; }

    public void RegistrarVista(DateTimeOffset fecha)
    {
        MarcarComoModificada();
        FechaUltimaVista = fecha;
    }
}
