using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class PerfilUsuarioConfiguration : IEntityTypeConfiguration<PerfilUsuario>
{
    public void Configure(EntityTypeBuilder<PerfilUsuario> builder)
    {
        builder.ToTable("Perfil_Usuarios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UsuarioInstitucional)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.UsuarioGdeba)
            .HasMaxLength(100);

        builder.HasIndex(x => x.UsuarioInstitucional)
            .IsUnique();

        // La identidad GDEBA es exclusiva de un perfil; el filtro permite multiples perfiles sin usuario cargado.
        builder.HasIndex(x => x.UsuarioGdeba)
            .IsUnique()
            .HasFilter("[UsuarioGdeba] IS NOT NULL");

        builder.HasMany(x => x.Seguimientos)
            .WithOne(x => x.PerfilUsuario)
            .HasForeignKey(x => x.PerfilUsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
