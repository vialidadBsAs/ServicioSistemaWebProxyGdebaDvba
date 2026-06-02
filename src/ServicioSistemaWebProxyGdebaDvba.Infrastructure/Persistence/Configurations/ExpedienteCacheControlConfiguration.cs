using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ExpedienteCacheControlConfiguration : IEntityTypeConfiguration<ExpedienteCacheControl>
{
    public void Configure(EntityTypeBuilder<ExpedienteCacheControl> builder)
    {
        builder.ToTable("ExpedienteCacheControls");

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
            .WithOne(x => x.CacheControl)
            .HasForeignKey<ExpedienteCacheControl>(x => x.ExpedienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
