using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaMonitoreoYEjecucionManualWorkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Worker_Ejecuciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proceso = table.Column<int>(type: "int", nullable: false),
                    Origen = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    SolicitudEjecucionWorkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaFinalizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Resumen = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Procesados = table.Column<int>(type: "int", nullable: true),
                    Enriquecidos = table.Column<int>(type: "int", nullable: true),
                    SinDatos = table.Column<int>(type: "int", nullable: true),
                    Errores = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worker_Ejecuciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Worker_SolicitudesEjecucion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proceso = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    SolicitadaPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaSolicitud = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaFinalizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EjecucionWorkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worker_SolicitudesEjecucion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Worker_Ejecuciones_Proceso_FechaInicio",
                table: "Worker_Ejecuciones",
                columns: new[] { "Proceso", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Worker_Ejecuciones_SolicitudEjecucionWorkerId",
                table: "Worker_Ejecuciones",
                column: "SolicitudEjecucionWorkerId",
                unique: true,
                filter: "[SolicitudEjecucionWorkerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Worker_SolicitudesEjecucion_EjecucionWorkerId",
                table: "Worker_SolicitudesEjecucion",
                column: "EjecucionWorkerId",
                unique: true,
                filter: "[EjecucionWorkerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Worker_SolicitudesEjecucion_Proceso_Estado_FechaSolicitud",
                table: "Worker_SolicitudesEjecucion",
                columns: new[] { "Proceso", "Estado", "FechaSolicitud" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Worker_Ejecuciones");

            migrationBuilder.DropTable(
                name: "Worker_SolicitudesEjecucion");
        }
    }
}
