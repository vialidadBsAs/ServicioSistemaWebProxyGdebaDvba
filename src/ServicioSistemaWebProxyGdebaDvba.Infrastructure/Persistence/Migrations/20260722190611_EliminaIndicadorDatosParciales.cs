using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EliminaIndicadorDatosParciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TieneDatosParciales",
                table: "Cache_HistorialExpedienteControl");

            migrationBuilder.DropColumn(
                name: "TieneDatosParciales",
                table: "Cache_ExpedienteControl");

            migrationBuilder.DropColumn(
                name: "TieneDatosParciales",
                table: "Cache_DocumentoControl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TieneDatosParciales",
                table: "Cache_HistorialExpedienteControl",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneDatosParciales",
                table: "Cache_ExpedienteControl",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneDatosParciales",
                table: "Cache_DocumentoControl",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
