using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaAlertasDocumentales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Perfil_AlertasDocumentales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerfilUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TipoDocumentoCodigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrataId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioGenerador = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil_AlertasDocumentales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfil_AlertasDocumentales_Perfil_Usuarios_PerfilUsuarioId",
                        column: x => x.PerfilUsuarioId,
                        principalTable: "Perfil_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perfil_AlertasDocumentales_TratasHabilitadasVialidad_TrataId",
                        column: x => x.TrataId,
                        principalTable: "TratasHabilitadasVialidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Perfil_CoincidenciasAlertaDocumental",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertaDocumentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteDocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaVista = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil_CoincidenciasAlertaDocumental", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfil_CoincidenciasAlertaDocumental_ExpedienteDocumentos_ExpedienteDocumentoId",
                        column: x => x.ExpedienteDocumentoId,
                        principalTable: "ExpedienteDocumentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perfil_CoincidenciasAlertaDocumental_Perfil_AlertasDocumentales_AlertaDocumentalId",
                        column: x => x.AlertaDocumentalId,
                        principalTable: "Perfil_AlertasDocumentales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_AlertasDocumentales_Activa_TipoDocumentoCodigo",
                table: "Perfil_AlertasDocumentales",
                columns: new[] { "Activa", "TipoDocumentoCodigo" });

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_AlertasDocumentales_PerfilUsuarioId",
                table: "Perfil_AlertasDocumentales",
                column: "PerfilUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_AlertasDocumentales_TrataId",
                table: "Perfil_AlertasDocumentales",
                column: "TrataId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_CoincidenciasAlertaDocumental_AlertaDocumentalId_ExpedienteDocumentoId",
                table: "Perfil_CoincidenciasAlertaDocumental",
                columns: new[] { "AlertaDocumentalId", "ExpedienteDocumentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_CoincidenciasAlertaDocumental_AlertaDocumentalId_FechaVista",
                table: "Perfil_CoincidenciasAlertaDocumental",
                columns: new[] { "AlertaDocumentalId", "FechaVista" });

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_CoincidenciasAlertaDocumental_ExpedienteDocumentoId",
                table: "Perfil_CoincidenciasAlertaDocumental",
                column: "ExpedienteDocumentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Perfil_CoincidenciasAlertaDocumental");

            migrationBuilder.DropTable(
                name: "Perfil_AlertasDocumentales");
        }
    }
}
