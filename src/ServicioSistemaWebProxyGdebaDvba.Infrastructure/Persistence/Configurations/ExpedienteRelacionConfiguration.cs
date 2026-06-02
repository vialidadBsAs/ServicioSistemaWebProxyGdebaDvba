using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ExpedienteRelacionConfiguration : IEntityTypeConfiguration<ExpedienteRelacion>
{
    public void Configure(EntityTypeBuilder<ExpedienteRelacion> builder)
    {
        builder.ToTable("ExpedienteRelaciones");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NumeroExpedienteRelacionado)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TipoRelacion)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(x => x.CodigoTrataRelacionado).HasMaxLength(50);
        builder.Property(x => x.DescripcionTrataRelacionado).HasMaxLength(500);
        builder.Property(x => x.UsuarioRelacion).HasMaxLength(150);

        builder.Property(x => x.FuenteDeteccion)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ExpedienteOrigenId, x.TipoRelacion, x.NumeroExpedienteRelacionado })
            .IsUnique();

        builder.HasIndex(x => x.ExpedienteRelacionadoId);

        builder.HasOne(x => x.ExpedienteOrigen)
            .WithMany(x => x.Relaciones)
            .HasForeignKey(x => x.ExpedienteOrigenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ExpedienteRelacionado)
            .WithMany()
            .HasForeignKey(x => x.ExpedienteRelacionadoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
