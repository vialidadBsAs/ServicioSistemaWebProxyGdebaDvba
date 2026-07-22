using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionDescubrimientoEstadoExpedienteConfiguration : IEntityTypeConfiguration<ConfiguracionDescubrimientoEstadoExpediente>
{
    public void Configure(EntityTypeBuilder<ConfiguracionDescubrimientoEstadoExpediente> builder)
    {
        builder.ToTable("Configuracion_EstadosDescubrimientoExpediente");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.EstadoExpedienteGdebaId)
            .IsUnique();

        builder.HasIndex(x => new { x.Habilitado, x.Prioridad });

        builder.HasOne(x => x.EstadoExpedienteGdeba)
            .WithMany()
            .HasForeignKey(x => x.EstadoExpedienteGdebaId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
