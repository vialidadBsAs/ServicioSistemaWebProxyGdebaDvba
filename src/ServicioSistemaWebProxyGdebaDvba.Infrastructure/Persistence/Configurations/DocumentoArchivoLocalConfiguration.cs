using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class DocumentoArchivoLocalConfiguration : IEntityTypeConfiguration<DocumentoArchivoLocal>
{
    public void Configure(EntityTypeBuilder<DocumentoArchivoLocal> builder)
    {
        builder.ToTable("DocumentoArchivosLocales");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.HashContenido)
            .HasMaxLength(128);

        builder.Property(x => x.StorageProvider)
            .HasMaxLength(100);

        builder.Property(x => x.StorageKey)
            .HasMaxLength(500);

        builder.Property(x => x.RutaRelativa)
            .HasMaxLength(1000);

        builder.Property(x => x.ContentType)
            .HasMaxLength(200);

        builder.Property(x => x.ExtensionArchivo)
            .HasMaxLength(20);

        builder.HasIndex(x => x.DocumentoId)
            .IsUnique();

        builder.HasIndex(x => x.StorageKey);

        builder.HasOne(x => x.Documento)
            .WithOne(x => x.ArchivoLocal)
            .HasForeignKey<DocumentoArchivoLocal>(x => x.DocumentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
