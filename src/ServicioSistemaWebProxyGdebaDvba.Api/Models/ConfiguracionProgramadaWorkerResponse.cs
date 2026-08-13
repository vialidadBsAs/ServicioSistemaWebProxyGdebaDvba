using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Models;

public sealed record ConfiguracionProgramadaWorkerResponse(
    string Proceso,
    bool Habilitado,
    TimeOnly HoraInicioLocal,
    TimeOnly HoraFinLocal,
    int CupoReservaDiaria,
    int? IntervaloMinutos,
    bool EjecutarAlIniciar,
    int? TamanoLote,
    int? ConsultasVaciasParaPausa,
    int? DiasPausaSinResultados,
    bool OmitirConsultasRealizadasEnElDia)
{
    public static ConfiguracionProgramadaWorkerResponse Create(ConfiguracionProgramadaWorkerDto configuracion)
    {
        return new ConfiguracionProgramadaWorkerResponse(
            configuracion.Proceso.ToString(),
            configuracion.Habilitado,
            configuracion.HoraInicioLocal,
            configuracion.HoraFinLocal,
            configuracion.CupoReservaDiaria,
            configuracion.IntervaloMinutos,
            configuracion.EjecutarAlIniciar,
            configuracion.TamanoLote,
            configuracion.ConsultasVaciasParaPausa,
            configuracion.DiasPausaSinResultados,
            configuracion.OmitirConsultasRealizadasEnElDia);
    }
}

public sealed record GuardarConfiguracionProgramadaWorkerApiRequest(
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
