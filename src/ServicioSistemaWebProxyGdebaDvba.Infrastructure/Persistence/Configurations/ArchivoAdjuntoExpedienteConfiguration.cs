using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ArchivoAdjuntoExpedienteConfiguration : IEntityTypeConfiguration<ArchivoAdjuntoExpediente>
{
    public void Configure(EntityTypeBuilder<ArchivoAdjuntoExpediente> builder)
    {
        builder.ToTable("ArchivosAdjuntosExpediente");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NombreArchivo)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.FuenteDeteccion)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ExpedienteId, x.NombreArchivo })
            .IsUnique();
    }
}
