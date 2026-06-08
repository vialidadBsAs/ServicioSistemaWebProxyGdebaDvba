using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ExpedienteConfiguration : IEntityTypeConfiguration<Expediente>
{
    public void Configure(EntityTypeBuilder<Expediente> builder)
    {
        builder.ToTable("Expedientes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GdebaNumeroCompleto)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.GdebaTipo)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.GdebaSistema)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.GdebaReparticion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EstadoActual)
            .HasMaxLength(100);

        builder.Property(x => x.SistemaOrigen)
            .HasMaxLength(100);

        builder.Property(x => x.DescripcionTramite)
            .HasMaxLength(500);

        builder.Property(x => x.UsuarioCaratulador)
            .HasMaxLength(150);

        builder.Property(x => x.UsuarioDestino)
            .HasMaxLength(150);

        builder.Property(x => x.SectorDestino)
            .HasMaxLength(150);

        builder.Property(x => x.ReparticionActual)
            .HasMaxLength(100);

        builder.HasIndex(x => x.GdebaNumeroCompleto)
            .IsUnique();

        builder.HasIndex(x => new { x.GdebaTipo, x.GdebaAnio, x.GdebaNumero, x.GdebaSistema, x.GdebaReparticion })
            .IsUnique();

        builder.HasIndex(x => new { x.GdebaAnio, x.GdebaReparticion });

        builder.HasIndex(x => x.TrataId);

        builder.HasOne(x => x.Trata)
            .WithMany(x => x.Expedientes)
            .HasForeignKey(x => x.TrataId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Movimientos)
            .WithOne(x => x.Expediente)
            .HasForeignKey(x => x.ExpedienteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Documentos)
            .WithOne(x => x.Expediente)
            .HasForeignKey(x => x.ExpedienteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ArchivosAdjuntos)
            .WithOne(x => x.Expediente)
            .HasForeignKey(x => x.ExpedienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
