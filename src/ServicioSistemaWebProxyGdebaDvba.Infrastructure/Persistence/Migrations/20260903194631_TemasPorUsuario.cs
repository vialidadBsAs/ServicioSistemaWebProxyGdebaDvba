using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TemasPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemasExpediente_Codigo",
                table: "TemasExpediente");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioPropietario",
                table: "TemasExpediente",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Los temas existentes pasan a ser del usuario administrador actual (decision del 03/09/2026: temas personales, institucionales pospuestos).
            migrationBuilder.Sql("UPDATE TemasExpediente SET UsuarioPropietario = 'pdelucca' WHERE UsuarioPropietario = '';");

            migrationBuilder.CreateIndex(
                name: "IX_TemasExpediente_UsuarioPropietario_Codigo",
                table: "TemasExpediente",
                columns: new[] { "UsuarioPropietario", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemasExpediente_UsuarioPropietario_Codigo",
                table: "TemasExpediente");

            migrationBuilder.DropColumn(
                name: "UsuarioPropietario",
                table: "TemasExpediente");

            migrationBuilder.CreateIndex(
                name: "IX_TemasExpediente_Codigo",
                table: "TemasExpediente",
                column: "Codigo",
                unique: true);
        }
    }
}
