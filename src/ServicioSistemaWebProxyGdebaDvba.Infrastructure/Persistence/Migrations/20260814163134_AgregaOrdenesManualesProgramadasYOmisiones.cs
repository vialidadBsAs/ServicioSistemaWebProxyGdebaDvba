using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaOrdenesManualesProgramadasYOmisiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanceladaPor",
                table: "Worker_SolicitudesEjecucion",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaCancelacion",
                table: "Worker_SolicitudesEjecucion",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaInicioProgramada",
                table: "Worker_SolicitudesEjecucion",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Worker_OmisionesCorridaProgramada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proceso = table.Column<int>(type: "int", nullable: false),
                    FechaLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    OmitidaPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worker_OmisionesCorridaProgramada", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Worker_OmisionesCorridaProgramada_Proceso_FechaLocal",
                table: "Worker_OmisionesCorridaProgramada",
                columns: new[] { "Proceso", "FechaLocal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Worker_OmisionesCorridaProgramada");

            migrationBuilder.DropColumn(
                name: "CanceladaPor",
                table: "Worker_SolicitudesEjecucion");

            migrationBuilder.DropColumn(
                name: "FechaCancelacion",
                table: "Worker_SolicitudesEjecucion");

            migrationBuilder.DropColumn(
                name: "FechaInicioProgramada",
                table: "Worker_SolicitudesEjecucion");
        }
    }
}
