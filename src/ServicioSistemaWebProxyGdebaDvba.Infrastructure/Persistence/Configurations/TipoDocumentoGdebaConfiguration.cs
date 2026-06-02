using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class TipoDocumentoGdebaConfiguration : IEntityTypeConfiguration<TipoDocumentoGdeba>
{
    public void Configure(EntityTypeBuilder<TipoDocumentoGdeba> builder)
    {
        builder.ToTable("TiposDocumentoGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CodigoTipoDocumentoGdeba)
            .HasMaxLength(50);

        builder.Property(x => x.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasMaxLength(500);

        builder.Property(x => x.Familia)
            .HasMaxLength(100);

        builder.Property(x => x.TipoProduccion)
            .HasMaxLength(100);

        builder.Property(x => x.Estado)
            .HasMaxLength(100);

        builder.HasIndex(x => x.Codigo)
            .IsUnique();

        builder.HasIndex(x => x.CodigoTipoDocumentoGdeba);

        builder.HasIndex(x => new { x.EsResolucion, x.Activo });

        builder.HasIndex(x => new { x.Familia, x.Activo });
    }
}
