using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class TemaExpedienteConfiguration : IEntityTypeConfiguration<TemaExpediente>
{
    public void Configure(EntityTypeBuilder<TemaExpediente> builder)
    {
        builder.ToTable("TemasExpediente");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.HasMany(x => x.Tratas).WithOne(x => x.TemaExpediente).HasForeignKey(x => x.TemaExpedienteId).OnDelete(DeleteBehavior.Cascade);
    }
}
