using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaConfiguracionEnriquecimientoMetadataDocumentoPorTema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configuracion_TemasEnriquecimientoMetadataDocumento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemaExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Habilitado = table.Column<bool>(type: "bit", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracion_TemasEnriquecimientoMetadataDocumento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configuracion_TemasEnriquecimientoMetadataDocumento_TemasExpediente_TemaExpedienteId",
                        column: x => x.TemaExpedienteId,
                        principalTable: "TemasExpediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex(
                name: "IX_Configuracion_TemasEnriquecimientoMetadataDocumento_Habilitado_Prioridad",
                table: "Configuracion_TemasEnriquecimientoMetadataDocumento",
                columns: new[] { "Habilitado", "Prioridad" });

            migrationBuilder.CreateIndex(
                name: "IX_Configuracion_TemasEnriquecimientoMetadataDocumento_TemaExpedienteId",
                table: "Configuracion_TemasEnriquecimientoMetadataDocumento",
                column: "TemaExpedienteId",
                unique: true);

            migrationBuilder.InsertData(
                table: "Configuracion_TemasEnriquecimientoMetadataDocumento",
                columns: new[] { "Id", "TemaExpedienteId", "Habilitado", "Prioridad" },
                values: new object[]
                {
                    new Guid("34000000-0000-0000-0000-000000000001"),
                    new Guid("30000000-0000-0000-0000-000000000001"),
                    true,
                    1
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracion_TemasEnriquecimientoMetadataDocumento");
        }
    }
}
