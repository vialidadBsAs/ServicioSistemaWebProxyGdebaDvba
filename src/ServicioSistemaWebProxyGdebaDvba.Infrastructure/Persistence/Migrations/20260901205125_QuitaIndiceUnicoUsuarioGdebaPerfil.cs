using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuitaIndiceUnicoUsuarioGdebaPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfil_Usuarios_UsuarioGdeba",
                table: "Perfil_Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_Usuarios_UsuarioGdeba",
                table: "Perfil_Usuarios",
                column: "UsuarioGdeba");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfil_Usuarios_UsuarioGdeba",
                table: "Perfil_Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_Usuarios_UsuarioGdeba",
                table: "Perfil_Usuarios",
                column: "UsuarioGdeba",
                unique: true,
                filter: "[UsuarioGdeba] IS NOT NULL");
        }
    }
}
