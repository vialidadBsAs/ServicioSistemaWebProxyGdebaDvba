using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class AlertaDocumentalConfiguration : IEntityTypeConfiguration<AlertaDocumental>
{
    public void Configure(EntityTypeBuilder<AlertaDocumental> builder)
    {
        builder.ToTable("Perfil_AlertasDocumentales");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(200);

        builder.Property(x => x.TipoDocumentoCodigo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UsuarioGenerador).HasMaxLength(150);

        builder.HasIndex(x => x.PerfilUsuarioId);

        // El detector busca las alertas activas por el tipo documental de los vinculos recien detectados.
        builder.HasIndex(x => new { x.Activa, x.TipoDocumentoCodigo });

        builder.HasOne(x => x.Trata)
            .WithMany()
            .HasForeignKey(x => x.TrataId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Coincidencias)
            .WithOne(x => x.AlertaDocumental)
            .HasForeignKey(x => x.AlertaDocumentalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
