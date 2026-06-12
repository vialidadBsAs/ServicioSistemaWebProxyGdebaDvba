using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaControlCuotasGdeba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperacionesGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Servicio = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LimiteDiario = table.Column<int>(type: "int", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacionesGdeba", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvocacionesGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ambiente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Exitosa = table.Column<bool>(type: "bit", nullable: false),
                    EstadoHttp = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvocacionesGdeba", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvocacionesGdeba_OperacionesGdeba_OperacionId",
                        column: x => x.OperacionId,
                        principalTable: "OperacionesGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvocacionesGdeba_Fecha",
                table: "InvocacionesGdeba",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_InvocacionesGdeba_OperacionId_Ambiente_Fecha",
                table: "InvocacionesGdeba",
                columns: new[] { "OperacionId", "Ambiente", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesGdeba_Servicio_Metodo",
                table: "OperacionesGdeba",
                columns: new[] { "Servicio", "Metodo" },
                unique: true);

            migrationBuilder.InsertData(
                table: "OperacionesGdeba",
                columns: new[] { "Id", "Servicio", "Metodo", "LimiteDiario", "Activa" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "ws_gdeba_consultaDocumento", "buscarDetallePorNumero", 200, true },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "ws_gdeba_consultaDocumento", "buscarPDFPorNumero", 400, true },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "ws_gdeba_consultaDocumento", "buscarDocumentoEnExpedientes", 200, true },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "ws_gdeba_consultaDocumento", "buscarPorNumero", 100, true },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "ws_gdeba_consultaTipoDocumento", "consultarTipoDocumento", 50, true },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "ws_gdeba_consultaExpediente", "buscarExpediente", 400, true },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "ws_gdeba_consultaExpediente", "buscarHistorialPasesExpediente", 500, true },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "ws_gdeba_consultaExpediente", "consultarExpedienteDetallado", 400, true },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "ws_gdeba_consultaExpediente", "buscarCodigoCaratulaPorNumeroExpediente", 100, true },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "ws_gdeba_consultaExpediente", "validarExpediente", 200, true },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "ws_gdeba_consultaExpediente", "buscarDatosExpedientePorCodigosTrata", 200, true },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "ws_gdeba_consultaEstadoPaseExpediente", "esEstadoPaseExpedienteValido", 100, true },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "ws_gdeba_consultaEstadoPaseExpediente", "consultaEstadoActualExpediente", 100, true },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "ws_gdeba_tratas", "buscarTratas", 100, true },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "ws_gdeba_tratas", "buscarTratasPorCodigo", 100, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvocacionesGdeba");

            migrationBuilder.DropTable(
                name: "OperacionesGdeba");
        }
    }
}
