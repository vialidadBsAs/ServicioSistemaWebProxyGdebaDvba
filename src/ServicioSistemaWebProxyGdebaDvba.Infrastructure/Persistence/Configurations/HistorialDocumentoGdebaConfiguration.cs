using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class HistorialDocumentoGdebaConfiguration : IEntityTypeConfiguration<HistorialDocumentoGdeba>
{
    public void Configure(EntityTypeBuilder<HistorialDocumentoGdeba> builder)
    {
        builder.ToTable("HistorialDocumentosGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Actividad).HasMaxLength(200);
        builder.Property(x => x.Usuario).HasMaxLength(150);
        builder.Property(x => x.NombreUsuario).HasMaxLength(250);
        builder.Property(x => x.WorkflowOrigen).HasMaxLength(250);

        builder.HasIndex(x => new { x.DocumentoId, x.IdGdeba })
            .IsUnique();

        builder.HasIndex(x => new { x.DocumentoId, x.FechaInicio });
    }
}
