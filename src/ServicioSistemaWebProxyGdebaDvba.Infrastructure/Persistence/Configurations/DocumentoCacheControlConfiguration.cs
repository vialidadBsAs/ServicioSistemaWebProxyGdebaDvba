using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class DocumentoCacheControlConfiguration : IEntityTypeConfiguration<DocumentoCacheControl>
{
    public void Configure(EntityTypeBuilder<DocumentoCacheControl> builder)
    {
        builder.ToTable("DocumentoCacheControls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FuenteUltimaRespuesta)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.UltimoErrorConsulta)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.DocumentoId)
            .IsUnique();

        builder.HasIndex(x => x.FechaVencimiento);

        builder.HasOne(x => x.Documento)
            .WithOne(x => x.CacheControl)
            .HasForeignKey<DocumentoCacheControl>(x => x.DocumentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
