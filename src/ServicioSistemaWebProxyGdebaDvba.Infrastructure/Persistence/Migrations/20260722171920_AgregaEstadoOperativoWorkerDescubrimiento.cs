using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaEstadoOperativoWorkerDescubrimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkerDescubrimiento_ExpedientesSegunTratasEstados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoTrata = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstadoExpedienteGdebaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaUltimaConsulta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaUltimoResultadoHabilitado = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConsultasSinResultadosConsecutivas = table.Column<int>(type: "int", nullable: false),
                    OmitirHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerDescubrimiento_ExpedientesSegunTratasEstados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_ExpedientesSegunTratasEstados_CodigoTrata_EstadoExpedienteGdebaId",
                table: "WorkerDescubrimiento_ExpedientesSegunTratasEstados",
                columns: new[] { "CodigoTrata", "EstadoExpedienteGdebaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDescubrimiento_ExpedientesSegunTratasEstados_OmitirHasta",
                table: "WorkerDescubrimiento_ExpedientesSegunTratasEstados",
                column: "OmitirHasta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerDescubrimiento_ExpedientesSegunTratasEstados");
        }
    }
}
