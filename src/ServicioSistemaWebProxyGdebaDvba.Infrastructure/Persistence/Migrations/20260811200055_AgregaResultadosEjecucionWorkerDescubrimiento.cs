using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaResultadosEjecucionWorkerDescubrimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkerDescubrimiento_EjecucionesPorTrataEstado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EjecucionWorkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrataHabilitadaVialidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoExpedienteGdebaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaResolucion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecibidosGdeba = table.Column<int>(type: "int", nullable: false),
                    Habilitados = table.Column<int>(type: "int", nullable: false),
                    Descartados = table.Column<int>(type: "int", nullable: false),
                    Creados = table.Column<int>(type: "int", nullable: false),
                    Actualizados = table.Column<int>(type: "int", nullable: false),
                    SinCambios = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerDescubrimiento_EjecucionesPorTrataEstado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerDescubrimiento_EjecucionesPorTrataEstado_EstadosExpedienteGdeba_EstadoExpedienteGdebaId",
                        column: x => x.EstadoExpedienteGdebaId,
                        principalTable: "EstadosExpedienteGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerDescubrimiento_EjecucionesPorTrataEstado_TratasHabilitadasVialidad_TrataHabilitadaVialidadId",
                        column: x => x.TrataHabilitadaVialidadId,
                        principalTable: "TratasHabilitadasVialidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerDescubrimiento_EjecucionesPorTrataEstado_Worker_Ejecuciones_EjecucionWorkerId",
                        column: x => x.EjecucionWorkerId,
                        principalTable: "Worker_Ejecuciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EjecucionWorkerDescubrimientoTrataEstadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado_WorkerDescubrimiento_EjecucionesPorTrataEstado_Ejecucion~",
                        column: x => x.EjecucionWorkerDescubrimientoTrataEstadoId,
                        principalTable: "WorkerDescubrimiento_EjecucionesPorTrataEstado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_EjecucionesPorTrataEstado_EjecucionWorkerId",
                table: "WorkerDescubrimiento_EjecucionesPorTrataEstado",
                column: "EjecucionWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_EjecucionesPorTrataEstado_EjecucionWorkerId_TrataHabilitadaVialidadId_EstadoExpedienteGdebaId",
                table: "WorkerDescubrimiento_EjecucionesPorTrataEstado",
                columns: new[] { "EjecucionWorkerId", "TrataHabilitadaVialidadId", "EstadoExpedienteGdebaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_EjecucionesPorTrataEstado_EstadoExpedienteGdebaId",
                table: "WorkerDescubrimiento_EjecucionesPorTrataEstado",
                column: "EstadoExpedienteGdebaId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_EjecucionesPorTrataEstado_TrataHabilitadaVialidadId",
                table: "WorkerDescubrimiento_EjecucionesPorTrataEstado",
                column: "TrataHabilitadaVialidadId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado_EjecucionWorkerDescubrimientoTrataEstadoId_ExpedienteId",
                table: "WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado",
                columns: new[] { "EjecucionWorkerDescubrimientoTrataEstadoId", "ExpedienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado_ExpedienteId",
                table: "WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado",
                column: "ExpedienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado");

            migrationBuilder.DropTable(
                name: "WorkerDescubrimiento_EjecucionesPorTrataEstado");
        }
    }
}
