using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionDescubrimientoTrataExpedienteConfiguration : IEntityTypeConfiguration<ConfiguracionDescubrimientoTrataExpediente>
{
    public void Configure(EntityTypeBuilder<ConfiguracionDescubrimientoTrataExpediente> builder)
    {
        builder.ToTable("Configuracion_TratasDescubrimientoExpediente");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodigoTrata)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.CodigoTrata)
            .IsUnique();

        builder.HasIndex(x => new { x.Habilitada, x.Prioridad });
    }
}
