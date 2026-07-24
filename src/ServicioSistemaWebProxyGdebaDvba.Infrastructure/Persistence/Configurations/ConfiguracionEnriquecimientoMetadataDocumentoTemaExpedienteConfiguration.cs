using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionEnriquecimientoMetadataDocumentoTemaExpedienteConfiguration : IEntityTypeConfiguration<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente>
{
    public void Configure(EntityTypeBuilder<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> builder)
    {
        builder.ToTable("Configuracion_TemasEnriquecimientoMetadataDocumento");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TemaExpedienteId).IsUnique();
        builder.HasIndex(x => new { x.Habilitado, x.Prioridad });
        builder.HasOne(x => x.TemaExpediente).WithMany().HasForeignKey(x => x.TemaExpedienteId).OnDelete(DeleteBehavior.Cascade);
    }
}
