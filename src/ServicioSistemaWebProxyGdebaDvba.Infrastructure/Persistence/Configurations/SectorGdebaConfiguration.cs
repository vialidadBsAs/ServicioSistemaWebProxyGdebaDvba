using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class SectorGdebaConfiguration : IEntityTypeConfiguration<SectorGdeba>
{
    public void Configure(EntityTypeBuilder<SectorGdeba> builder)
    {
        builder.ToTable("SectoresGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodigoReparticion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Codigo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasMaxLength(300);

        builder.HasIndex(x => new { x.CodigoReparticion, x.Codigo })
            .IsUnique();
    }
}
