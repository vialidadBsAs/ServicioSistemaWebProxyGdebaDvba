using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class SeguimientoExpedienteConfiguration : IEntityTypeConfiguration<SeguimientoExpediente>
{
    public void Configure(EntityTypeBuilder<SeguimientoExpediente> builder)
    {
        builder.ToTable("Perfil_SeguimientosExpediente");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.PerfilUsuarioId, x.ExpedienteId }).IsUnique();

        builder.HasIndex(x => x.ExpedienteId);

        builder.HasOne(x => x.Expediente)
            .WithMany()
            .HasForeignKey(x => x.ExpedienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
