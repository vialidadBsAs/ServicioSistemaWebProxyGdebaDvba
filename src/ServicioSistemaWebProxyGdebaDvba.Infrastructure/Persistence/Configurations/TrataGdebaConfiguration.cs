using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class TrataGdebaConfiguration : IEntityTypeConfiguration<TrataGdeba>
{
    public void Configure(EntityTypeBuilder<TrataGdeba> builder)
    {
        builder.ToTable("TratasGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.AcronimoGedo).HasMaxLength(50);
        builder.Property(x => x.Estado).HasMaxLength(100);
        builder.Property(x => x.IdTrataGdeba).HasMaxLength(100);
        builder.Property(x => x.TipoReservaDescripcion).HasMaxLength(500);
        builder.Property(x => x.TipoReservaId).HasMaxLength(100);
        builder.Property(x => x.TipoReservaDescripcionTipoReserva).HasMaxLength(500);

        builder.HasIndex(x => x.Codigo)
            .IsUnique();
    }
}
