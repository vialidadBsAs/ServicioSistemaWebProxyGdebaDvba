using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizaConfiguracionDescubrimientoYTablasTransversales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentoCacheControls_DocumentosGdeba_DocumentoId",
                table: "DocumentoCacheControls");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedienteCacheControls_Expedientes_ExpedienteId",
                table: "ExpedienteCacheControls");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialExpedienteCacheControls_Expedientes_ExpedienteId",
                table: "HistorialExpedienteCacheControls");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialExpedienteCacheControls_MovimientosExpediente_UltimoMovimientoDetectadoId",
                table: "HistorialExpedienteCacheControls");

            migrationBuilder.DropForeignKey(
                name: "FK_InvocacionesGdeba_OperacionesGdeba_OperacionId",
                table: "InvocacionesGdeba");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosAuditoria_AplicacionesConsumidoras_AplicacionConsumidoraId",
                table: "RegistrosAuditoria");

            migrationBuilder.DropForeignKey(
                name: "FK_TrataCacheControls_TratasHabilitadasVialidad_TrataId",
                table: "TrataCacheControls");

            migrationBuilder.DropIndex(
                name: "IX_EstadosExpedienteGdeba_HabilitadoParaDescubrimiento_PrioridadDescubrimiento",
                table: "EstadosExpedienteGdeba");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrataCacheControls",
                table: "TrataCacheControls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RegistrosAuditoria",
                table: "RegistrosAuditoria");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OperacionesGdeba",
                table: "OperacionesGdeba");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InvocacionesGdeba",
                table: "InvocacionesGdeba");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistorialExpedienteCacheControls",
                table: "HistorialExpedienteCacheControls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpedienteCacheControls",
                table: "ExpedienteCacheControls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocumentoCacheControls",
                table: "DocumentoCacheControls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AplicacionesConsumidoras",
                table: "AplicacionesConsumidoras");

            migrationBuilder.RenameTable(
                name: "TrataCacheControls",
                newName: "Cache_TrataControl");

            migrationBuilder.RenameTable(
                name: "RegistrosAuditoria",
                newName: "Auditoria_Registros");

            migrationBuilder.RenameTable(
                name: "OperacionesGdeba",
                newName: "IntegracionGdeba_Operaciones");

            migrationBuilder.RenameTable(
                name: "InvocacionesGdeba",
                newName: "IntegracionGdeba_Invocaciones");

            migrationBuilder.RenameTable(
                name: "HistorialExpedienteCacheControls",
                newName: "Cache_HistorialExpedienteControl");

            migrationBuilder.RenameTable(
                name: "ExpedienteCacheControls",
                newName: "Cache_ExpedienteControl");

            migrationBuilder.RenameTable(
                name: "DocumentoCacheControls",
                newName: "Cache_DocumentoControl");

            migrationBuilder.RenameTable(
                name: "AplicacionesConsumidoras",
                newName: "Seguridad_AplicacionesConsumidoras");

            migrationBuilder.RenameIndex(
                name: "IX_TrataCacheControls_TrataId",
                table: "Cache_TrataControl",
                newName: "IX_Cache_TrataControl_TrataId");

            migrationBuilder.RenameIndex(
                name: "IX_TrataCacheControls_FechaVencimiento",
                table: "Cache_TrataControl",
                newName: "IX_Cache_TrataControl_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_RegistrosAuditoria_Fecha",
                table: "Auditoria_Registros",
                newName: "IX_Auditoria_Registros_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_RegistrosAuditoria_AplicacionConsumidoraId_OperacionSolicitada_Fecha",
                table: "Auditoria_Registros",
                newName: "IX_Auditoria_Registros_AplicacionConsumidoraId_OperacionSolicitada_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_OperacionesGdeba_Servicio_Metodo",
                table: "IntegracionGdeba_Operaciones",
                newName: "IX_IntegracionGdeba_Operaciones_Servicio_Metodo");

            migrationBuilder.RenameIndex(
                name: "IX_InvocacionesGdeba_SolicitudId_NumeroIntento",
                table: "IntegracionGdeba_Invocaciones",
                newName: "IX_IntegracionGdeba_Invocaciones_SolicitudId_NumeroIntento");

            migrationBuilder.RenameIndex(
                name: "IX_InvocacionesGdeba_OperacionId",
                table: "IntegracionGdeba_Invocaciones",
                newName: "IX_IntegracionGdeba_Invocaciones_OperacionId");

            migrationBuilder.RenameIndex(
                name: "IX_InvocacionesGdeba_Fecha",
                table: "IntegracionGdeba_Invocaciones",
                newName: "IX_IntegracionGdeba_Invocaciones_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_InvocacionesGdeba_Ambiente_Fecha_OperacionId_Origen",
                table: "IntegracionGdeba_Invocaciones",
                newName: "IX_IntegracionGdeba_Invocaciones_Ambiente_Fecha_OperacionId_Origen");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialExpedienteCacheControls_UltimoMovimientoDetectadoId",
                table: "Cache_HistorialExpedienteControl",
                newName: "IX_Cache_HistorialExpedienteControl_UltimoMovimientoDetectadoId");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialExpedienteCacheControls_FechaVencimiento",
                table: "Cache_HistorialExpedienteControl",
                newName: "IX_Cache_HistorialExpedienteControl_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialExpedienteCacheControls_ExpedienteId",
                table: "Cache_HistorialExpedienteControl",
                newName: "IX_Cache_HistorialExpedienteControl_ExpedienteId");

            migrationBuilder.RenameIndex(
                name: "IX_ExpedienteCacheControls_FechaVencimiento",
                table: "Cache_ExpedienteControl",
                newName: "IX_Cache_ExpedienteControl_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_ExpedienteCacheControls_ExpedienteId",
                table: "Cache_ExpedienteControl",
                newName: "IX_Cache_ExpedienteControl_ExpedienteId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentoCacheControls_FechaVencimiento",
                table: "Cache_DocumentoControl",
                newName: "IX_Cache_DocumentoControl_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentoCacheControls_DocumentoId",
                table: "Cache_DocumentoControl",
                newName: "IX_Cache_DocumentoControl_DocumentoId");

            migrationBuilder.RenameIndex(
                name: "IX_AplicacionesConsumidoras_Codigo",
                table: "Seguridad_AplicacionesConsumidoras",
                newName: "IX_Seguridad_AplicacionesConsumidoras_Codigo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cache_TrataControl",
                table: "Cache_TrataControl",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Auditoria_Registros",
                table: "Auditoria_Registros",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IntegracionGdeba_Operaciones",
                table: "IntegracionGdeba_Operaciones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IntegracionGdeba_Invocaciones",
                table: "IntegracionGdeba_Invocaciones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cache_HistorialExpedienteControl",
                table: "Cache_HistorialExpedienteControl",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cache_ExpedienteControl",
                table: "Cache_ExpedienteControl",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cache_DocumentoControl",
                table: "Cache_DocumentoControl",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Seguridad_AplicacionesConsumidoras",
                table: "Seguridad_AplicacionesConsumidoras",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Configuracion_EstadosDescubrimientoExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoExpedienteGdebaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Habilitado = table.Column<bool>(type: "bit", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracion_EstadosDescubrimientoExpediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configuracion_EstadosDescubrimientoExpediente_EstadosExpedienteGdeba_EstadoExpedienteGdebaId",
                        column: x => x.EstadoExpedienteGdebaId,
                        principalTable: "EstadosExpedienteGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Configuracion_TratasDescubrimientoExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoTrata = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Habilitada = table.Column<bool>(type: "bit", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracion_TratasDescubrimientoExpediente", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO dbo.Configuracion_EstadosDescubrimientoExpediente (Id, EstadoExpedienteGdebaId, Habilitado, Prioridad)
                SELECT NEWID(), Id, HabilitadoParaDescubrimiento, PrioridadDescubrimiento
                FROM dbo.EstadosExpedienteGdeba;
                """);

            migrationBuilder.DropColumn(
                name: "HabilitadoParaDescubrimiento",
                table: "EstadosExpedienteGdeba");

            migrationBuilder.DropColumn(
                name: "PrioridadDescubrimiento",
                table: "EstadosExpedienteGdeba");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracion_EstadosDescubrimientoExpediente_EstadoExpedienteGdebaId",
                table: "Configuracion_EstadosDescubrimientoExpediente",
                column: "EstadoExpedienteGdebaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configuracion_EstadosDescubrimientoExpediente_Habilitado_Prioridad",
                table: "Configuracion_EstadosDescubrimientoExpediente",
                columns: new[] { "Habilitado", "Prioridad" });

            migrationBuilder.CreateIndex(
                name: "IX_Configuracion_TratasDescubrimientoExpediente_CodigoTrata",
                table: "Configuracion_TratasDescubrimientoExpediente",
                column: "CodigoTrata",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configuracion_TratasDescubrimientoExpediente_Habilitada_Prioridad",
                table: "Configuracion_TratasDescubrimientoExpediente",
                columns: new[] { "Habilitada", "Prioridad" });

            migrationBuilder.AddForeignKey(
                name: "FK_Auditoria_Registros_Seguridad_AplicacionesConsumidoras_AplicacionConsumidoraId",
                table: "Auditoria_Registros",
                column: "AplicacionConsumidoraId",
                principalTable: "Seguridad_AplicacionesConsumidoras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cache_DocumentoControl_DocumentosGdeba_DocumentoId",
                table: "Cache_DocumentoControl",
                column: "DocumentoId",
                principalTable: "DocumentosGdeba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cache_ExpedienteControl_Expedientes_ExpedienteId",
                table: "Cache_ExpedienteControl",
                column: "ExpedienteId",
                principalTable: "Expedientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cache_HistorialExpedienteControl_Expedientes_ExpedienteId",
                table: "Cache_HistorialExpedienteControl",
                column: "ExpedienteId",
                principalTable: "Expedientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cache_HistorialExpedienteControl_MovimientosExpediente_UltimoMovimientoDetectadoId",
                table: "Cache_HistorialExpedienteControl",
                column: "UltimoMovimientoDetectadoId",
                principalTable: "MovimientosExpediente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cache_TrataControl_TratasHabilitadasVialidad_TrataId",
                table: "Cache_TrataControl",
                column: "TrataId",
                principalTable: "TratasHabilitadasVialidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegracionGdeba_Invocaciones_IntegracionGdeba_Operaciones_OperacionId",
                table: "IntegracionGdeba_Invocaciones",
                column: "OperacionId",
                principalTable: "IntegracionGdeba_Operaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auditoria_Registros_Seguridad_AplicacionesConsumidoras_AplicacionConsumidoraId",
                table: "Auditoria_Registros");

            migrationBuilder.DropForeignKey(
                name: "FK_Cache_DocumentoControl_DocumentosGdeba_DocumentoId",
                table: "Cache_DocumentoControl");

            migrationBuilder.DropForeignKey(
                name: "FK_Cache_ExpedienteControl_Expedientes_ExpedienteId",
                table: "Cache_ExpedienteControl");

            migrationBuilder.DropForeignKey(
                name: "FK_Cache_HistorialExpedienteControl_Expedientes_ExpedienteId",
                table: "Cache_HistorialExpedienteControl");

            migrationBuilder.DropForeignKey(
                name: "FK_Cache_HistorialExpedienteControl_MovimientosExpediente_UltimoMovimientoDetectadoId",
                table: "Cache_HistorialExpedienteControl");

            migrationBuilder.DropForeignKey(
                name: "FK_Cache_TrataControl_TratasHabilitadasVialidad_TrataId",
                table: "Cache_TrataControl");

            migrationBuilder.DropForeignKey(
                name: "FK_IntegracionGdeba_Invocaciones_IntegracionGdeba_Operaciones_OperacionId",
                table: "IntegracionGdeba_Invocaciones");

            migrationBuilder.AddColumn<bool>(
                name: "HabilitadoParaDescubrimiento",
                table: "EstadosExpedienteGdeba",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PrioridadDescubrimiento",
                table: "EstadosExpedienteGdeba",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE estado
                SET HabilitadoParaDescubrimiento = configuracion.Habilitado,
                    PrioridadDescubrimiento = configuracion.Prioridad
                FROM dbo.EstadosExpedienteGdeba AS estado
                INNER JOIN dbo.Configuracion_EstadosDescubrimientoExpediente AS configuracion
                    ON configuracion.EstadoExpedienteGdebaId = estado.Id;
                """);

            migrationBuilder.DropTable(
                name: "Configuracion_EstadosDescubrimientoExpediente");

            migrationBuilder.DropTable(
                name: "Configuracion_TratasDescubrimientoExpediente");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Seguridad_AplicacionesConsumidoras",
                table: "Seguridad_AplicacionesConsumidoras");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IntegracionGdeba_Operaciones",
                table: "IntegracionGdeba_Operaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IntegracionGdeba_Invocaciones",
                table: "IntegracionGdeba_Invocaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cache_TrataControl",
                table: "Cache_TrataControl");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cache_HistorialExpedienteControl",
                table: "Cache_HistorialExpedienteControl");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cache_ExpedienteControl",
                table: "Cache_ExpedienteControl");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cache_DocumentoControl",
                table: "Cache_DocumentoControl");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Auditoria_Registros",
                table: "Auditoria_Registros");

            migrationBuilder.RenameTable(
                name: "Seguridad_AplicacionesConsumidoras",
                newName: "AplicacionesConsumidoras");

            migrationBuilder.RenameTable(
                name: "IntegracionGdeba_Operaciones",
                newName: "OperacionesGdeba");

            migrationBuilder.RenameTable(
                name: "IntegracionGdeba_Invocaciones",
                newName: "InvocacionesGdeba");

            migrationBuilder.RenameTable(
                name: "Cache_TrataControl",
                newName: "TrataCacheControls");

            migrationBuilder.RenameTable(
                name: "Cache_HistorialExpedienteControl",
                newName: "HistorialExpedienteCacheControls");

            migrationBuilder.RenameTable(
                name: "Cache_ExpedienteControl",
                newName: "ExpedienteCacheControls");

            migrationBuilder.RenameTable(
                name: "Cache_DocumentoControl",
                newName: "DocumentoCacheControls");

            migrationBuilder.RenameTable(
                name: "Auditoria_Registros",
                newName: "RegistrosAuditoria");

            migrationBuilder.RenameIndex(
                name: "IX_Seguridad_AplicacionesConsumidoras_Codigo",
                table: "AplicacionesConsumidoras",
                newName: "IX_AplicacionesConsumidoras_Codigo");

            migrationBuilder.RenameIndex(
                name: "IX_IntegracionGdeba_Operaciones_Servicio_Metodo",
                table: "OperacionesGdeba",
                newName: "IX_OperacionesGdeba_Servicio_Metodo");

            migrationBuilder.RenameIndex(
                name: "IX_IntegracionGdeba_Invocaciones_SolicitudId_NumeroIntento",
                table: "InvocacionesGdeba",
                newName: "IX_InvocacionesGdeba_SolicitudId_NumeroIntento");

            migrationBuilder.RenameIndex(
                name: "IX_IntegracionGdeba_Invocaciones_OperacionId",
                table: "InvocacionesGdeba",
                newName: "IX_InvocacionesGdeba_OperacionId");

            migrationBuilder.RenameIndex(
                name: "IX_IntegracionGdeba_Invocaciones_Fecha",
                table: "InvocacionesGdeba",
                newName: "IX_InvocacionesGdeba_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_IntegracionGdeba_Invocaciones_Ambiente_Fecha_OperacionId_Origen",
                table: "InvocacionesGdeba",
                newName: "IX_InvocacionesGdeba_Ambiente_Fecha_OperacionId_Origen");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_TrataControl_TrataId",
                table: "TrataCacheControls",
                newName: "IX_TrataCacheControls_TrataId");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_TrataControl_FechaVencimiento",
                table: "TrataCacheControls",
                newName: "IX_TrataCacheControls_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_HistorialExpedienteControl_UltimoMovimientoDetectadoId",
                table: "HistorialExpedienteCacheControls",
                newName: "IX_HistorialExpedienteCacheControls_UltimoMovimientoDetectadoId");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_HistorialExpedienteControl_FechaVencimiento",
                table: "HistorialExpedienteCacheControls",
                newName: "IX_HistorialExpedienteCacheControls_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_HistorialExpedienteControl_ExpedienteId",
                table: "HistorialExpedienteCacheControls",
                newName: "IX_HistorialExpedienteCacheControls_ExpedienteId");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_ExpedienteControl_FechaVencimiento",
                table: "ExpedienteCacheControls",
                newName: "IX_ExpedienteCacheControls_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_ExpedienteControl_ExpedienteId",
                table: "ExpedienteCacheControls",
                newName: "IX_ExpedienteCacheControls_ExpedienteId");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_DocumentoControl_FechaVencimiento",
                table: "DocumentoCacheControls",
                newName: "IX_DocumentoCacheControls_FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_Cache_DocumentoControl_DocumentoId",
                table: "DocumentoCacheControls",
                newName: "IX_DocumentoCacheControls_DocumentoId");

            migrationBuilder.RenameIndex(
                name: "IX_Auditoria_Registros_Fecha",
                table: "RegistrosAuditoria",
                newName: "IX_RegistrosAuditoria_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_Auditoria_Registros_AplicacionConsumidoraId_OperacionSolicitada_Fecha",
                table: "RegistrosAuditoria",
                newName: "IX_RegistrosAuditoria_AplicacionConsumidoraId_OperacionSolicitada_Fecha");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AplicacionesConsumidoras",
                table: "AplicacionesConsumidoras",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OperacionesGdeba",
                table: "OperacionesGdeba",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvocacionesGdeba",
                table: "InvocacionesGdeba",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrataCacheControls",
                table: "TrataCacheControls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistorialExpedienteCacheControls",
                table: "HistorialExpedienteCacheControls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpedienteCacheControls",
                table: "ExpedienteCacheControls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocumentoCacheControls",
                table: "DocumentoCacheControls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RegistrosAuditoria",
                table: "RegistrosAuditoria",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_EstadosExpedienteGdeba_HabilitadoParaDescubrimiento_PrioridadDescubrimiento",
                table: "EstadosExpedienteGdeba",
                columns: new[] { "HabilitadoParaDescubrimiento", "PrioridadDescubrimiento" });

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentoCacheControls_DocumentosGdeba_DocumentoId",
                table: "DocumentoCacheControls",
                column: "DocumentoId",
                principalTable: "DocumentosGdeba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedienteCacheControls_Expedientes_ExpedienteId",
                table: "ExpedienteCacheControls",
                column: "ExpedienteId",
                principalTable: "Expedientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialExpedienteCacheControls_Expedientes_ExpedienteId",
                table: "HistorialExpedienteCacheControls",
                column: "ExpedienteId",
                principalTable: "Expedientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialExpedienteCacheControls_MovimientosExpediente_UltimoMovimientoDetectadoId",
                table: "HistorialExpedienteCacheControls",
                column: "UltimoMovimientoDetectadoId",
                principalTable: "MovimientosExpediente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvocacionesGdeba_OperacionesGdeba_OperacionId",
                table: "InvocacionesGdeba",
                column: "OperacionId",
                principalTable: "OperacionesGdeba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosAuditoria_AplicacionesConsumidoras_AplicacionConsumidoraId",
                table: "RegistrosAuditoria",
                column: "AplicacionConsumidoraId",
                principalTable: "AplicacionesConsumidoras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrataCacheControls_TratasHabilitadasVialidad_TrataId",
                table: "TrataCacheControls",
                column: "TrataId",
                principalTable: "TratasHabilitadasVialidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
