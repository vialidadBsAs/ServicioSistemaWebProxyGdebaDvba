using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DesglosaEstadisticaExpedientesPorTrataPorEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                        e.EstadoActual AS Estado,
                        CONVERT(int, COUNT_BIG(1)) AS TotalExpedientes
                    FROM dbo.Expedientes AS e
                    INNER JOIN dbo.TratasHabilitadasVialidad AS t ON t.Id = e.TrataId
                    WHERE (@CodigoTrata IS NULL OR t.CodigoTrata = @CodigoTrata)
                        AND (@FechaDesde IS NULL OR e.FechaCaratulacion >= @FechaDesde)
                        AND (@FechaHastaExclusiva IS NULL OR e.FechaCaratulacion < @FechaHastaExclusiva)
                        AND (@Estado IS NULL OR e.EstadoActual = @Estado)
                    GROUP BY t.CodigoTrata, t.DescripcionTrata, e.EstadoActual
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
