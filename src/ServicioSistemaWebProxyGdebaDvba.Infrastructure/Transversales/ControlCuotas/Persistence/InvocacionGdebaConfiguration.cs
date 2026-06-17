using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.ControlCuotas.Persistence;

public sealed class InvocacionGdebaConfiguration : IEntityTypeConfiguration<InvocacionGdeba>
{
    public void Configure(EntityTypeBuilder<InvocacionGdeba> builder)
    {
        builder.ToTable("InvocacionesGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Ambiente).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Origen)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(OrigenInvocacionGdeba.NoDeterminado);
        builder.Property(x => x.SolicitudId).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.NumeroIntento).HasDefaultValue(1);
        builder.Property(x => x.ServidorRespondio).HasDefaultValue(false);

        builder.HasIndex(x => x.Fecha);
        builder.HasIndex(x => new { x.Ambiente, x.Fecha, x.OperacionId, x.Origen });
        builder.HasIndex(x => new { x.SolicitudId, x.NumeroIntento });

        builder.HasOne(x => x.Operacion)
            .WithMany(x => x.Invocaciones)
            .HasForeignKey(x => x.OperacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
