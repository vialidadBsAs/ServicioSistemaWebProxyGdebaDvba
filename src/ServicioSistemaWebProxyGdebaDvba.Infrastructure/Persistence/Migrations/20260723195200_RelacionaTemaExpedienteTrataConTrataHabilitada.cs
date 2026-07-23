using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RelacionaTemaExpedienteTrataConTrataHabilitada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemaExpedienteTratas_CodigoTrata",
                table: "TemaExpedienteTratas");

            migrationBuilder.DropIndex(
                name: "IX_TemaExpedienteTratas_TemaExpedienteId_CodigoTrata",
                table: "TemaExpedienteTratas");

            migrationBuilder.AddColumn<Guid>(
                name: "TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM TemaExpedienteTratas AS asignacion
                    LEFT JOIN TratasHabilitadasVialidad AS trata ON trata.CodigoTrata = asignacion.CodigoTrata
                    WHERE trata.Id IS NULL
                )
                    THROW 50001, 'Existen codigos de TemaExpedienteTratas sin una trata habilitada correspondiente.', 1;

                UPDATE asignacion
                SET asignacion.TrataHabilitadaVialidadId = primeraTrata.Id
                FROM TemaExpedienteTratas AS asignacion
                CROSS APPLY (
                    SELECT TOP (1) trata.Id
                    FROM TratasHabilitadasVialidad AS trata
                    WHERE trata.CodigoTrata = asignacion.CodigoTrata
                    ORDER BY trata.Id
                ) AS primeraTrata;

                INSERT INTO TemaExpedienteTratas (Id, TemaExpedienteId, CodigoTrata, TrataHabilitadaVialidadId)
                SELECT NEWID(), asignacion.TemaExpedienteId, asignacion.CodigoTrata, trata.Id
                FROM TemaExpedienteTratas AS asignacion
                INNER JOIN TratasHabilitadasVialidad AS trata ON trata.CodigoTrata = asignacion.CodigoTrata
                WHERE trata.Id <> asignacion.TrataHabilitadaVialidadId;
                """);

            migrationBuilder.DropColumn(
                name: "CodigoTrata",
                table: "TemaExpedienteTratas");

            migrationBuilder.AlterColumn<Guid>(
                name: "TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemaExpedienteTratas_TemaExpedienteId_TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas",
                columns: new[] { "TemaExpedienteId", "TrataHabilitadaVialidadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemaExpedienteTratas_TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas",
                column: "TrataHabilitadaVialidadId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemaExpedienteTratas_TratasHabilitadasVialidad_TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas",
                column: "TrataHabilitadaVialidadId",
                principalTable: "TratasHabilitadasVialidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemaExpedienteTratas_TratasHabilitadasVialidad_TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas");

            migrationBuilder.DropIndex(
                name: "IX_TemaExpedienteTratas_TemaExpedienteId_TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas");

            migrationBuilder.DropIndex(
                name: "IX_TemaExpedienteTratas_TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas");

            migrationBuilder.AddColumn<string>(
                name: "CodigoTrata",
                table: "TemaExpedienteTratas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE asignacion
                SET asignacion.CodigoTrata = trata.CodigoTrata
                FROM TemaExpedienteTratas AS asignacion
                INNER JOIN TratasHabilitadasVialidad AS trata ON trata.Id = asignacion.TrataHabilitadaVialidadId;

                WITH asignacionesDuplicadas AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY TemaExpedienteId, CodigoTrata ORDER BY Id) AS NumeroFila
                    FROM TemaExpedienteTratas
                )
                DELETE FROM asignacionesDuplicadas WHERE NumeroFila > 1;
                """);

            migrationBuilder.DropColumn(
                name: "TrataHabilitadaVialidadId",
                table: "TemaExpedienteTratas");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoTrata",
                table: "TemaExpedienteTratas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemaExpedienteTratas_CodigoTrata",
                table: "TemaExpedienteTratas",
                column: "CodigoTrata");

            migrationBuilder.CreateIndex(
                name: "IX_TemaExpedienteTratas_TemaExpedienteId_CodigoTrata",
                table: "TemaExpedienteTratas",
                columns: new[] { "TemaExpedienteId", "CodigoTrata" },
                unique: true);
        }
    }
}
