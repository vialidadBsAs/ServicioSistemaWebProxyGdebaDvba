using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

public sealed record ConfiguracionProgramadaWorkerDto(
    Guid Id,
    ProcesoWorker Proceso,
    bool Habilitado,
    TimeOnly HoraInicioLocal,
    TimeOnly HoraFinLocal,
    int CupoReservaDiaria,
    int? IntervaloMinutos,
    bool EjecutarAlIniciar,
    int? TamanoLote,
    int? ConsultasVaciasParaPausa,
    int? DiasPausaSinResultados,
    bool OmitirConsultasRealizadasEnElDia);

public sealed record GuardarConfiguracionProgramadaWorkerRequest(
    ProcesoWorker Proceso,
    bool Habilitado,
    TimeOnly HoraInicioLocal,
    TimeOnly HoraFinLocal,
    int CupoReservaDiaria,
    int? IntervaloMinutos,
    bool EjecutarAlIniciar,
    int? TamanoLote,
    int? ConsultasVaciasParaPausa,
    int? DiasPausaSinResultados,
    bool OmitirConsultasRealizadasEnElDia);
