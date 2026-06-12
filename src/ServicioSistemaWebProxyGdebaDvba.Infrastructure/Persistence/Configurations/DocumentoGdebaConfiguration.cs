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

        builder.Property(x => x.NumeroActuacionCompleto)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ActuacionTipoCodigo)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ActuacionSistema)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ActuacionReparticion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NumeroEspecialCompleto).HasMaxLength(100);
        builder.Property(x => x.EspecialTipoCodigo).HasMaxLength(20);
        builder.Property(x => x.EspecialSistema).HasMaxLength(50);
        builder.Property(x => x.EspecialReparticion).HasMaxLength(100);
        builder.Property(x => x.TipoDocumentoCodigo).HasMaxLength(50);
        builder.Property(x => x.TipoDocumentoNombre).HasMaxLength(200);
        builder.Property(x => x.TipoDocumentoDescripcion).HasMaxLength(500);
        builder.Property(x => x.Referencia).HasMaxLength(1000);
        builder.Property(x => x.ListaFirmantes).HasMaxLength(4000);
        builder.Property(x => x.UrlArchivo).HasMaxLength(1000);

        builder.HasIndex(x => x.NumeroActuacionCompleto)
            .IsUnique();

        builder.HasIndex(x => new
            {
                x.ActuacionTipoCodigo,
                x.ActuacionAnio,
                x.ActuacionNumero,
                x.ActuacionSistema,
                x.ActuacionReparticion
            })
            .IsUnique();

        builder.HasIndex(x => x.NumeroEspecialCompleto)
            .IsUnique()
            .HasFilter("[NumeroEspecialCompleto] IS NOT NULL");

        builder.HasIndex(x => new { x.TipoDocumentoCodigo, x.ActuacionReparticion });

        builder.HasIndex(x => x.TipoDocumentoId);

        builder.HasOne(x => x.TipoDocumento)
            .WithMany()
            .HasForeignKey(x => x.TipoDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Expedientes)
            .WithOne(x => x.Documento)
            .HasForeignKey(x => x.DocumentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Historial)
            .WithOne(x => x.Documento)
            .HasForeignKey(x => x.DocumentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
