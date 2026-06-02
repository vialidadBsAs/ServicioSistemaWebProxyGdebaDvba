using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class HistorialExpedienteCacheControlConfiguration : IEntityTypeConfiguration<HistorialExpedienteCacheControl>
{
    public void Configure(EntityTypeBuilder<HistorialExpedienteCacheControl> builder)
    {
        builder.ToTable("HistorialExpedienteCacheControls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FuenteUltimaRespuesta)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.UltimoErrorConsulta)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.ExpedienteId)
            .IsUnique();

        builder.HasIndex(x => x.FechaVencimiento);

        builder.HasOne(x => x.Expediente)
            .WithOne(x => x.HistorialCacheControl)
            .HasForeignKey<HistorialExpedienteCacheControl>(x => x.ExpedienteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UltimoMovimientoDetectado)
            .WithMany()
            .HasForeignKey(x => x.UltimoMovimientoDetectadoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
