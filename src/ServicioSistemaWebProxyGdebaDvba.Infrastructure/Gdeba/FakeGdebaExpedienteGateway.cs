using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class FakeGdebaExpedienteGateway : IGdebaExpedienteGateway
{
    public Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var expediente = new ExpedienteGdebaDto(
            numeroGdebaCompleto.Valor,
            CodigoTrata: "FIN0057",
            DescripcionTrata: "Elevacion de Consultas",
            Estado: "Tramitacion");

        return Task.FromResult<ExpedienteGdebaDto?>(expediente);
    }

    public Task<GdebaExpedienteDetalladoDto?> ConsultarExpedienteDetalladoAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var expediente = new GdebaExpedienteDetalladoDto(
            numeroGdebaCompleto.Valor,
            CodigoTrata: "COMP0003",
            DescripcionTrata: "Licitacion de Obra Fake",
            Estado: "Ejecucion",
            SistemaOrigen: "EE",
            DescripcionTramite: "Rehabilitacion y Conservacion de Rutas Provinciales.",
            FechaCaratulacion: new DateTimeOffset(2020, 7, 3, 18, 50, 43, TimeSpan.FromHours(-3)),
            UsuarioCaratulador: "RCOLABIANCHI",
            UsuarioDestino: "VVERA",
            Documentos: new[]
            {
                new GdebaDocumentoExpedienteDto(
                    "RS-2023-33144875-GDEBA-DVMIYSPGP",
                    TipoDocumentoCodigo: null,
                    Referencia: "EX-2021-31948536-GDEBA-DPTLMIYSPGP Redeterminacion definitiva de precios.",
                    FechaCreacion: new DateTimeOffset(2023, 8, 7, 15, 26, 25, TimeSpan.FromHours(-3)),
                    FechaVinculacion: new DateTimeOffset(2025, 5, 20, 13, 30, 33, TimeSpan.FromHours(-3)),
                    UsuarioAsociacion: null,
                    UsuarioGenerador: null),
                new GdebaDocumentoExpedienteDto(
                    "PV-2025-01492173-GDEBA-DVMIYSPGP",
                    TipoDocumentoCodigo: "PV",
                    Referencia: "Pase",
                    FechaCreacion: new DateTimeOffset(2025, 1, 14, 10, 4, 54, TimeSpan.FromHours(-3)),
                    FechaVinculacion: new DateTimeOffset(2025, 1, 14, 10, 4, 54, TimeSpan.FromHours(-3)),
                    UsuarioAsociacion: null,
                    UsuarioGenerador: null)
            },
            ArchivosAdjuntos: new[]
            {
                "EX-2020-14232989-GDEBA-DVMIYSPGP dba Aprueba pliego y autoriza llamado a lic. publica.docx"
            },
            Relaciones: new[]
            {
                new GdebaRelacionExpedienteDto(
                    "EX-2021-4231314-   -GDEBA-DVMIYSPGP",
                    "TramitacionConjunta",
                    EsCabecera: false),
                new GdebaRelacionExpedienteDto(
                    "EX-2020-22936237-   -GDEBA-DVMIYSPGP",
                    "Asociado",
                    EsCabecera: false)
            });

        return Task.FromResult<GdebaExpedienteDetalladoDto?>(expediente);
    }

    public Task<GdebaHistorialExpedienteDto?> BuscarHistorialPasesExpedienteAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GdebaMovimientoExpedienteDto> movimientos = new[]
        {
            new GdebaMovimientoExpedienteDto(
                Orden: 1,
                FechaOperacion: new DateTimeOffset(2020, 7, 3, 18, 50, 43, TimeSpan.FromHours(-3)),
                EstadoOrigen: null,
                EstadoDestino: "Iniciacion",
                UsuarioOrigen: null,
                UsuarioDestino: "RCOLABIANCHI",
                Motivo: "Caratulacion del expediente.",
                ReparticionOrigen: null,
                ReparticionDestino: "DVMIYSPGP",
                SectorDestino: "MGEDV"),
            new GdebaMovimientoExpedienteDto(
                Orden: 2,
                FechaOperacion: new DateTimeOffset(2024, 11, 14, 10, 18, 38, TimeSpan.FromHours(-3)),
                EstadoOrigen: "Tramitacion",
                EstadoDestino: "Tramitacion",
                UsuarioOrigen: "GGAVALDA",
                UsuarioDestino: "VVERA",
                Motivo: "Pase a conocimiento.",
                ReparticionOrigen: "DVMIYSPGP",
                ReparticionDestino: "DVMIYSPGP",
                SectorDestino: "PVD"),
            new GdebaMovimientoExpedienteDto(
                Orden: 3,
                FechaOperacion: new DateTimeOffset(2025, 12, 15, 8, 54, 16, TimeSpan.FromHours(-3)),
                EstadoOrigen: "Tramitacion",
                EstadoDestino: "Tramitacion",
                UsuarioOrigen: "VVERA",
                UsuarioDestino: "DVMIYSPGP",
                Motivo: "Vinculacion de providencia de pase.",
                ReparticionOrigen: "DVMIYSPGP",
                ReparticionDestino: "DVMIYSPGP",
                SectorDestino: "PVD")
        };

        IReadOnlyCollection<GdebaDocumentoExpedienteDto> documentosVinculados = new[]
        {
            new GdebaDocumentoExpedienteDto(
                "PV-2022-39560475-GDEBA-DVMIYSPGP",
                TipoDocumentoCodigo: "PV",
                Referencia: "Caratula",
                FechaCreacion: new DateTimeOffset(2022, 11, 17, 10, 24, 5, TimeSpan.FromHours(-3)),
                FechaVinculacion: new DateTimeOffset(2022, 11, 17, 10, 24, 7, TimeSpan.FromHours(-3)),
                UsuarioAsociacion: "RCOLABIANCHI",
                UsuarioGenerador: "RCOLABIANCHI",
                OrdenRespuesta: 1),
            new GdebaDocumentoExpedienteDto(
                "PV-2022-40832917-GDEBA-GEDV",
                TipoDocumentoCodigo: "PV",
                Referencia: "Pase",
                FechaCreacion: new DateTimeOffset(2022, 11, 28, 14, 24, 29, TimeSpan.FromHours(-3)),
                FechaVinculacion: new DateTimeOffset(2022, 11, 28, 14, 24, 29, TimeSpan.FromHours(-3)),
                UsuarioAsociacion: "PPETTIROSSI",
                UsuarioGenerador: "PPETTIROSSI",
                OrdenRespuesta: 2)
        };

        var historial = new GdebaHistorialExpedienteDto(
            documentosVinculados,
            movimientos,
            Array.Empty<GdebaRelacionExpedienteDto>());

        return Task.FromResult<GdebaHistorialExpedienteDto?>(historial);
    }

    public Task<IReadOnlyCollection<GdebaExpedientePorTrataDto>> BuscarDatosExpedientePorCodigosTrataAsync(
        string codigoTrata,
        string estadoDestino,
        string? usuario,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GdebaExpedientePorTrataDto> expedientes = new[]
        {
            new GdebaExpedientePorTrataDto(
                "EX-2020-14232989-GDEBA-DVMIYSPGP",
                codigoTrata,
                "Licitacion de Obra Fake",
                estadoDestino,
                new DateTimeOffset(2025, 6, 20, 10, 30, 0, TimeSpan.FromHours(-3)),
                "Pase a seguimiento",
                usuario)
        };

        return Task.FromResult(expedientes);
    }
}
