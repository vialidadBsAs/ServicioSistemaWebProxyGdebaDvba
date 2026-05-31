using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class DocumentoGdebaConfiguration : IEntityTypeConfiguration<DocumentoGdeba>
{
    public void Configure(EntityTypeBuilder<DocumentoGdeba> builder)
    {
        builder.ToTable("DocumentosGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GdebaNumeroCompleto)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.GdebaTipo)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.GdebaSistema)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.GdebaReparticion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NumeroEspecial).HasMaxLength(100);
        builder.Property(x => x.TipoDocumento).HasMaxLength(50);
        builder.Property(x => x.Referencia).HasMaxLength(1000);
        builder.Property(x => x.UrlArchivo).HasMaxLength(1000);

        builder.HasIndex(x => x.GdebaNumeroCompleto)
            .IsUnique();

        builder.HasIndex(x => new { x.GdebaTipo, x.GdebaAnio, x.GdebaNumero, x.GdebaSistema, x.GdebaReparticion })
            .IsUnique();

        builder.HasMany(x => x.Expedientes)
            .WithOne(x => x.Documento)
            .HasForeignKey(x => x.DocumentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
