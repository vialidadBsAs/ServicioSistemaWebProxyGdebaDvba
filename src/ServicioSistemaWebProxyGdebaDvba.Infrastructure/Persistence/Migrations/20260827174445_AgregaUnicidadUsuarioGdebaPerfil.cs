using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaUnicidadUsuarioGdebaPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Perfil_Usuarios_UsuarioGdeba",
                table: "Perfil_Usuarios",
                column: "UsuarioGdeba",
                unique: true,
                filter: "[UsuarioGdeba] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfil_Usuarios_UsuarioGdeba",
                table: "Perfil_Usuarios");
        }
    }
}
