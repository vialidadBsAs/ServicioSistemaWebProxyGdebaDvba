using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsaTratasHabilitadasComoReferenciaLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expedientes_TratasGdeba_TrataId",
                table: "Expedientes");

            migrationBuilder.DropForeignKey(
                name: "FK_TrataCacheControls_TratasGdeba_TrataId",
                table: "TrataCacheControls");

            migrationBuilder.DropForeignKey(
                name: "FK_TratasHabilitadasVialidad_TratasGdeba_TrataId",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropIndex(
                name: "IX_TratasHabilitadasVialidad_TrataId",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.AddColumn<string>(
                name: "AcronimoGedo",
                table: "TratasHabilitadasVialidad",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsAutomatica",
                table: "TratasHabilitadasVialidad",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsTrataManual",
                table: "TratasHabilitadasVialidad",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdTrataGdeba",
                table: "TratasHabilitadasVialidad",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoReservaDescripcion",
                table: "TratasHabilitadasVialidad",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoReservaDescripcionTipoReserva",
                table: "TratasHabilitadasVialidad",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoReservaId",
                table: "TratasHabilitadasVialidad",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE thv
                SET
                    thv.AcronimoGedo = tg.AcronimoGedo,
                    thv.EsAutomatica = tg.EsAutomatica,
                    thv.EsTrataManual = tg.EsTrataManual,
                    thv.EstadoTrata = COALESCE(tg.Estado, thv.EstadoTrata),
                    thv.IdTrataGdeba = tg.IdTrataGdeba,
                    thv.TipoReservaDescripcion = tg.TipoReservaDescripcion,
                    thv.TipoReservaId = tg.TipoReservaId,
                    thv.TipoReservaDescripcionTipoReserva = tg.TipoReservaDescripcionTipoReserva
                FROM dbo.TratasHabilitadasVialidad AS thv
                INNER JOIN dbo.TratasGdeba AS tg ON tg.Id = thv.TrataId;
                """);

            migrationBuilder.Sql(
                """
                DECLARE @trataIdDefaultConstraint sysname;
                SELECT @trataIdDefaultConstraint = [d].[name]
                FROM [sys].[default_constraints] [d]
                INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id]
                    AND [d].[parent_object_id] = [c].[object_id]
                WHERE [d].[parent_object_id] = OBJECT_ID(N'[TratasHabilitadasVialidad]')
                    AND [c].[name] = N'TrataId';

                IF @trataIdDefaultConstraint IS NOT NULL
                    EXEC(N'ALTER TABLE [TratasHabilitadasVialidad] DROP CONSTRAINT [' + @trataIdDefaultConstraint + '];');

                ALTER TABLE [TratasHabilitadasVialidad] DROP COLUMN [TrataId];
                """);

            migrationBuilder.Sql(
                """
                UPDATE e
                SET TrataId = nuevaTrata.Id
                FROM dbo.Expedientes AS e
                INNER JOIN dbo.TratasGdeba AS trataAnterior ON trataAnterior.Id = e.TrataId
                OUTER APPLY
                (
                    SELECT TOP (1) thv.Id
                    FROM dbo.TratasHabilitadasVialidad AS thv
                    WHERE thv.CodigoTrata = trataAnterior.Codigo
                    ORDER BY
                        CASE WHEN thv.CodigoReparticion = e.GdebaReparticion THEN 0 ELSE 1 END,
                        CASE WHEN thv.CodigoReparticion = N'DVMIYSPGP' THEN 0 ELSE 1 END,
                        thv.CodigoReparticion
                ) AS nuevaTrata;

                UPDATE e
                SET TrataId = NULL
                FROM dbo.Expedientes AS e
                WHERE e.TrataId IS NOT NULL
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM dbo.TratasHabilitadasVialidad AS thv
                        WHERE thv.Id = e.TrataId
                    );
                """);

            migrationBuilder.Sql(
                """
                UPDATE tc
                SET TrataId = nuevaTrata.Id
                FROM dbo.TrataCacheControls AS tc
                INNER JOIN dbo.TratasGdeba AS trataAnterior ON trataAnterior.Id = tc.TrataId
                OUTER APPLY
                (
                    SELECT TOP (1) thv.Id
                    FROM dbo.TratasHabilitadasVialidad AS thv
                    WHERE thv.CodigoTrata = trataAnterior.Codigo
                    ORDER BY
                        CASE WHEN thv.CodigoReparticion = N'DVMIYSPGP' THEN 0 ELSE 1 END,
                        thv.CodigoReparticion
                ) AS nuevaTrata
                WHERE nuevaTrata.Id IS NOT NULL;

                DELETE tc
                FROM dbo.TrataCacheControls AS tc
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.TratasHabilitadasVialidad AS thv
                    WHERE thv.Id = tc.TrataId
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TratasHabilitadasVialidad_IdTrataGdeba",
                table: "TratasHabilitadasVialidad",
                column: "IdTrataGdeba");

            migrationBuilder.AddForeignKey(
                name: "FK_Expedientes_TratasHabilitadasVialidad_TrataId",
                table: "Expedientes",
                column: "TrataId",
                principalTable: "TratasHabilitadasVialidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrataCacheControls_TratasHabilitadasVialidad_TrataId",
                table: "TrataCacheControls",
                column: "TrataId",
                principalTable: "TratasHabilitadasVialidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER FUNCTION dbo.fn_EstadisticaExpedientesPorTrata
                (
                    @CodigoTrata nvarchar(50) = NULL,
                    @FechaDesde datetimeoffset = NULL,
                    @FechaHastaExclusiva datetimeoffset = NULL,
                    @Estado nvarchar(100) = NULL
                )
                RETURNS TABLE
                AS
                RETURN
                (
                    SELECT
                        t.CodigoTrata AS CodigoTrata,
                        t.DescripcionTrata AS DescripcionTrata,
                        CONVERT(int, COUNT_BIG(1)) AS TotalExpedientes
                    FROM dbo.Expedientes AS e
                    INNER JOIN dbo.TratasHabilitadasVialidad AS t ON t.Id = e.TrataId
                    WHERE (@CodigoTrata IS NULL OR t.CodigoTrata = @CodigoTrata)
                        AND (@FechaDesde IS NULL OR e.FechaCaratulacion >= @FechaDesde)
                        AND (@FechaHastaExclusiva IS NULL OR e.FechaCaratulacion < @FechaHastaExclusiva)
                        AND (@Estado IS NULL OR e.EstadoActual = @Estado)
                    GROUP BY t.CodigoTrata, t.DescripcionTrata
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expedientes_TratasHabilitadasVialidad_TrataId",
                table: "Expedientes");

            migrationBuilder.DropForeignKey(
                name: "FK_TrataCacheControls_TratasHabilitadasVialidad_TrataId",
                table: "TrataCacheControls");

            migrationBuilder.DropIndex(
                name: "IX_TratasHabilitadasVialidad_IdTrataGdeba",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropColumn(
                name: "AcronimoGedo",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropColumn(
                name: "EsAutomatica",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropColumn(
                name: "EsTrataManual",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropColumn(
                name: "IdTrataGdeba",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropColumn(
                name: "TipoReservaDescripcion",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropColumn(
                name: "TipoReservaDescripcionTipoReserva",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.DropColumn(
                name: "TipoReservaId",
                table: "TratasHabilitadasVialidad");

            migrationBuilder.AddColumn<Guid>(
                name: "TrataId",
                table: "TratasHabilitadasVialidad",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE thv
                SET TrataId = tg.Id
                FROM dbo.TratasHabilitadasVialidad AS thv
                INNER JOIN dbo.TratasGdeba AS tg ON tg.Codigo = thv.CodigoTrata;

                UPDATE e
                SET TrataId = tg.Id
                FROM dbo.Expedientes AS e
                INNER JOIN dbo.TratasHabilitadasVialidad AS thv ON thv.Id = e.TrataId
                INNER JOIN dbo.TratasGdeba AS tg ON tg.Codigo = thv.CodigoTrata;

                UPDATE tc
                SET TrataId = tg.Id
                FROM dbo.TrataCacheControls AS tc
                INNER JOIN dbo.TratasHabilitadasVialidad AS thv ON thv.Id = tc.TrataId
                INNER JOIN dbo.TratasGdeba AS tg ON tg.Codigo = thv.CodigoTrata;

                DELETE tc
                FROM dbo.TrataCacheControls AS tc
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.TratasGdeba AS tg
                    WHERE tg.Id = tc.TrataId
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TratasHabilitadasVialidad_TrataId",
                table: "TratasHabilitadasVialidad",
                column: "TrataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expedientes_TratasGdeba_TrataId",
                table: "Expedientes",
                column: "TrataId",
                principalTable: "TratasGdeba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrataCacheControls_TratasGdeba_TrataId",
                table: "TrataCacheControls",
                column: "TrataId",
                principalTable: "TratasGdeba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TratasHabilitadasVialidad_TratasGdeba_TrataId",
                table: "TratasHabilitadasVialidad",
                column: "TrataId",
                principalTable: "TratasGdeba",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER FUNCTION dbo.fn_EstadisticaExpedientesPorTrata
                (
                    @CodigoTrata nvarchar(50) = NULL,
                    @FechaDesde datetimeoffset = NULL,
                    @FechaHastaExclusiva datetimeoffset = NULL,
                    @Estado nvarchar(100) = NULL
                )
                RETURNS TABLE
                AS
                RETURN
                (
                    SELECT
                        t.Codigo AS CodigoTrata,
                        t.Descripcion AS DescripcionTrata,
                        CONVERT(int, COUNT_BIG(1)) AS TotalExpedientes
                    FROM dbo.Expedientes AS e
                    INNER JOIN dbo.TratasGdeba AS t ON t.Id = e.TrataId
                    WHERE (@CodigoTrata IS NULL OR t.Codigo = @CodigoTrata)
                        AND (@FechaDesde IS NULL OR e.FechaCaratulacion >= @FechaDesde)
                        AND (@FechaHastaExclusiva IS NULL OR e.FechaCaratulacion < @FechaHastaExclusiva)
                        AND (@Estado IS NULL OR e.EstadoActual = @Estado)
                    GROUP BY t.Codigo, t.Descripcion
                );
                """);
        }
    }
}
