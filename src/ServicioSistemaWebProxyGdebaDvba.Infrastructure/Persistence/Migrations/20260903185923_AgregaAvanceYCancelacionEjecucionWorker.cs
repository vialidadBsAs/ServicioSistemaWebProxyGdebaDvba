using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaAvanceYCancelacionEjecucionWorker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaCancelacionSolicitada",
                table: "Worker_Ejecuciones",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TamanoLote",
                table: "Worker_Ejecuciones",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCancelacionSolicitada",
                table: "Worker_Ejecuciones");

            migrationBuilder.DropColumn(
                name: "TamanoLote",
                table: "Worker_Ejecuciones");
        }
    }
}
