using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class AplicacionConsumidoraConfiguration : IEntityTypeConfiguration<AplicacionConsumidora>
{
    public void Configure(EntityTypeBuilder<AplicacionConsumidora> builder)
    {
        builder.ToTable("AplicacionesConsumidoras");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Codigo)
            .IsUnique();
    }
}
