using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaFuncionEstadisticaExpedientesPorTrata : Migration
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
                        t.Codigo AS CodigoTrata,
                        t.Descripcion AS DescripcionTrata,
                        CONVERT(int, COUNT_BIG(1)) AS TotalExpedientes
                    FROM dbo.Expedientes AS e
                    INNER JOIN dbo.TratasGdeba AS t ON t.Id = e.TrataId
                    WHERE (@CodigoTrata IS NULL OR t.Codigo = @CodigoTrata)
                        AND (@FechaDesde IS NULL OR e.FechaCaratulacion >= @FechaDesde)
                        AND (@FechaHastaExclusiva IS NULL OR
                            e.FechaCaratulacion < @FechaHastaExclusiva)
                        AND (@Estado IS NULL OR e.EstadoActual = @Estado)
                    GROUP BY t.Codigo, t.Descripcion
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP FUNCTION IF EXISTS dbo.fn_EstadisticaExpedientesPorTrata;");
        }
    }
}
