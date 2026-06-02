using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("RegistrosAuditoria");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Operacion)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Recurso)
            .HasMaxLength(200);

        builder.Property(x => x.Ambiente)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Fuente)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Fecha);

        builder.HasIndex(x => new { x.AplicacionConsumidoraId, x.Operacion, x.Fecha });

        builder.HasOne(x => x.AplicacionConsumidora)
            .WithMany(x => x.RegistrosAuditoria)
            .HasForeignKey(x => x.AplicacionConsumidoraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
