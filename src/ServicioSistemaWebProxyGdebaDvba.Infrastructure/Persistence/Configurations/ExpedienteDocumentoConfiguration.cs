using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ExpedienteDocumentoConfiguration : IEntityTypeConfiguration<ExpedienteDocumento>
{
    public void Configure(EntityTypeBuilder<ExpedienteDocumento> builder)
    {
        builder.ToTable("ExpedienteDocumentos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UsuarioAsociacion).HasMaxLength(150);
        builder.Property(x => x.UsuarioGenerador).HasMaxLength(150);

        builder.Property(x => x.FuenteDeteccion)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ExpedienteId, x.DocumentoId })
            .IsUnique();
    }
}
