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

        // GDEBA no acota estos textos: se almacenan completos (nvarchar(max)).
        builder.Property(x => x.Referencia);
        builder.Property(x => x.ListaFirmantes);
        builder.Property(x => x.UrlArchivo);

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
        // Resumen por tipo documental: el conteo por tipo y metadata se resuelve integramente sobre este indice angosto, sin leer la tabla ancha (Referencia es texto largo).
        builder.HasIndex(x => x.ActuacionTipoCodigo)
            .IncludeProperties(x => x.MetadataCompleta);
        // Busqueda por referencia: el LIKE '%texto%' recorre este indice angosto (la referencia promedia 20 caracteres) en vez de la tabla ancha.
        // Referencia va completa como columna incluida (los textos largos admiten INCLUDE), asi la busqueda es exacta sin recortes; la clave
        // FechaCreacion entrega el orden por defecto ya resuelto y las demas incluidas cubren orden, filtros y pagina sin tocar la tabla.
        builder.HasIndex(x => x.FechaCreacion)
            .IncludeProperties(x => new { x.Referencia, x.NumeroActuacionCompleto, x.ActuacionTipoCodigo, x.TipoDocumentoCodigo });

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
