using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class CoincidenciaAlertaDocumentalConfiguration : IEntityTypeConfiguration<CoincidenciaAlertaDocumental>
{
    public void Configure(EntityTypeBuilder<CoincidenciaAlertaDocumental> builder)
    {
        builder.ToTable("Perfil_CoincidenciasAlertaDocumental");

        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.EstaPendiente);

        builder.HasIndex(x => new { x.AlertaDocumentalId, x.ExpedienteDocumentoId }).IsUnique();

        // El badge cuenta pendientes por alerta; la pantalla ordena por fecha de deteccion.
        builder.HasIndex(x => new { x.AlertaDocumentalId, x.FechaVista });

        // La coincidencia es un hecho derivado del vinculo: si el vinculo desaparece, la coincidencia no tiene sentido.
        builder.HasOne(x => x.ExpedienteDocumento)
            .WithMany()
            .HasForeignKey(x => x.ExpedienteDocumentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
