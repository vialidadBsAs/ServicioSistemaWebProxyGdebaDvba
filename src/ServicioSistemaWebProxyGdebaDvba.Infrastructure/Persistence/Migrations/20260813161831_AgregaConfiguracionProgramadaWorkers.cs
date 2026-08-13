using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaConfiguracionProgramadaWorkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Worker_ConfiguracionesProgramadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proceso = table.Column<int>(type: "int", nullable: false),
                    Habilitado = table.Column<bool>(type: "bit", nullable: false),
                    HoraInicioLocal = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFinLocal = table.Column<TimeOnly>(type: "time", nullable: false),
                    CupoReservaDiaria = table.Column<int>(type: "int", nullable: false),
                    IntervaloMinutos = table.Column<int>(type: "int", nullable: true),
                    EjecutarAlIniciar = table.Column<bool>(type: "bit", nullable: false),
                    TamanoLote = table.Column<int>(type: "int", nullable: true),
                    ConsultasVaciasParaPausa = table.Column<int>(type: "int", nullable: true),
                    DiasPausaSinResultados = table.Column<int>(type: "int", nullable: true),
                    OmitirConsultasRealizadasEnElDia = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worker_ConfiguracionesProgramadas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Worker_ConfiguracionesProgramadas_Proceso",
                table: "Worker_ConfiguracionesProgramadas",
                column: "Proceso",
                unique: true);

            migrationBuilder.InsertData(
                table: "Worker_ConfiguracionesProgramadas",
                columns: new[]
                {
                    "Id", "Proceso", "Habilitado", "HoraInicioLocal", "HoraFinLocal", "CupoReservaDiaria",
                    "IntervaloMinutos", "EjecutarAlIniciar", "TamanoLote", "ConsultasVaciasParaPausa",
                    "DiasPausaSinResultados", "OmitirConsultasRealizadasEnElDia"
                },
                values: new object[,]
                {
                    {
                        new Guid("40000000-0000-0000-0000-000000000001"), 1, true, new TimeOnly(3, 0), new TimeOnly(6, 0), 20,
                        null, false, null, 3, 7, false
                    },
                    {
                        new Guid("40000000-0000-0000-0000-000000000002"), 2, true, new TimeOnly(4, 0), new TimeOnly(6, 0), 20,
                        30, true, 150, null, null, false
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Worker_ConfiguracionesProgramadas");
        }
    }
}
