using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionDescubrimientoTemaExpedienteConfiguration : IEntityTypeConfiguration<ConfiguracionDescubrimientoTemaExpediente>
{
    public void Configure(EntityTypeBuilder<ConfiguracionDescubrimientoTemaExpediente> builder)
    {
        builder.ToTable("Configuracion_TemasDescubrimientoExpediente");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TemaExpedienteId).IsUnique();
        builder.HasIndex(x => new { x.Habilitado, x.Prioridad });
        builder.HasOne(x => x.TemaExpediente).WithMany().HasForeignKey(x => x.TemaExpedienteId).OnDelete(DeleteBehavior.Cascade);
    }
}
