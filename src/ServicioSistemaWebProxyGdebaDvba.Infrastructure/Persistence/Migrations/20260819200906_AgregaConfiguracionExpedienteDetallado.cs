using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaConfiguracionExpedienteDetallado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Worker_ConfiguracionesProgramadas",
                columns: new[]
                {
                    "Id", "Proceso", "Habilitado", "HoraInicioLocal", "HoraFinLocal", "CupoReservaDiaria",
                    "IntervaloMinutos", "EjecutarAlIniciar", "TamanoLote", "ConsultasVaciasParaPausa",
                    "DiasPausaSinResultados", "OmitirConsultasRealizadasEnElDia"
                },
                values: new object[]
                {
                    new Guid("40000000-0000-0000-0000-000000000003"), 3, false, new TimeOnly(3, 0), new TimeOnly(6, 0), 0,
                    30, false, 20, null, null, false
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Worker_ConfiguracionesProgramadas",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"));
        }
    }
}
