using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class InvocacionGdebaConfiguration : IEntityTypeConfiguration<InvocacionGdeba>
{
    public void Configure(EntityTypeBuilder<InvocacionGdeba> builder)
    {
        builder.ToTable("InvocacionesGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Ambiente).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(x => x.Fecha);
        builder.HasIndex(x => new { x.OperacionId, x.Ambiente, x.Fecha });

        builder.HasOne(x => x.Operacion)
            .WithMany(x => x.Invocaciones)
            .HasForeignKey(x => x.OperacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
