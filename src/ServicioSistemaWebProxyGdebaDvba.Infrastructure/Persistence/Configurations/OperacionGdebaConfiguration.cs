using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class OperacionGdebaConfiguration : IEntityTypeConfiguration<OperacionGdeba>
{
    public void Configure(EntityTypeBuilder<OperacionGdeba> builder)
    {
        builder.ToTable("OperacionesGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Servicio).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Metodo).HasMaxLength(150).IsRequired();

        builder.HasIndex(x => new { x.Servicio, x.Metodo }).IsUnique();
    }
}
