using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class MovimientoExpedienteConfiguration : IEntityTypeConfiguration<MovimientoExpediente>
{
    public void Configure(EntityTypeBuilder<MovimientoExpediente> builder)
    {
        builder.ToTable("MovimientosExpediente");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EstadoOrigen).HasMaxLength(100);
        builder.Property(x => x.EstadoDestino).HasMaxLength(100);
        builder.Property(x => x.UsuarioOrigen).HasMaxLength(150);
        builder.Property(x => x.UsuarioDestino).HasMaxLength(150);
        builder.Property(x => x.Motivo).HasMaxLength(1000);
        builder.Property(x => x.ReparticionOrigen).HasMaxLength(100);
        builder.Property(x => x.ReparticionDestino).HasMaxLength(100);

        builder.HasIndex(x => new { x.ExpedienteId, x.Orden })
            .IsUnique();

        builder.HasIndex(x => new { x.ExpedienteId, x.EsUltimoConocido });
    }
}
