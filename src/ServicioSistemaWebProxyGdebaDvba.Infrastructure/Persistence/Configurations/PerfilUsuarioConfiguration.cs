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
        // La exclusividad del usuario GDEBA la garantiza PerfilUsuarioService (absoluta salvo en Development, donde se
        // permite compartirlo entre perfiles de prueba): un indice unico en el esquema no puede expresar esa distincion.
        builder.HasIndex(x => x.UsuarioGdeba);

        builder.HasMany(x => x.Seguimientos)
            .WithOne(x => x.PerfilUsuario)
            .HasForeignKey(x => x.PerfilUsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
