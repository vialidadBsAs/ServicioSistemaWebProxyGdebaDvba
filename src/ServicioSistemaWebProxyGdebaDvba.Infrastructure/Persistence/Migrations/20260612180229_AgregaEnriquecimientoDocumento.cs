using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaEnriquecimientoDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ListaFirmantes",
                table: "DocumentosGdeba",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TipoDocumentoId",
                table: "DocumentosGdeba",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistorialDocumentosGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdGdeba = table.Column<long>(type: "bigint", nullable: false),
                    Actividad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Usuario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    WorkflowOrigen = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialDocumentosGdeba", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialDocumentosGdeba_DocumentosGdeba_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "DocumentosGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGdeba_TipoDocumentoId",
                table: "DocumentosGdeba",
                column: "TipoDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialDocumentosGdeba_DocumentoId_FechaInicio",
                table: "HistorialDocumentosGdeba",
                columns: new[] { "DocumentoId", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialDocumentosGdeba_DocumentoId_IdGdeba",
                table: "HistorialDocumentosGdeba",
                columns: new[] { "DocumentoId", "IdGdeba" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosGdeba_TiposDocumentoGdeba_TipoDocumentoId",
                table: "DocumentosGdeba",
                column: "TipoDocumentoId",
                principalTable: "TiposDocumentoGdeba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosGdeba_TiposDocumentoGdeba_TipoDocumentoId",
                table: "DocumentosGdeba");

            migrationBuilder.DropTable(
                name: "HistorialDocumentosGdeba");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosGdeba_TipoDocumentoId",
                table: "DocumentosGdeba");

            migrationBuilder.DropColumn(
                name: "ListaFirmantes",
                table: "DocumentosGdeba");

            migrationBuilder.DropColumn(
                name: "TipoDocumentoId",
                table: "DocumentosGdeba");
        }
    }
}
