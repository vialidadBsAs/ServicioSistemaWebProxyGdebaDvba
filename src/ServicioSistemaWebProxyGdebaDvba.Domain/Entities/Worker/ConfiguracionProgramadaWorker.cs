using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed class ConfiguracionProgramadaWorker : DomainEntity
{
    private ConfiguracionProgramadaWorker()
    {
    }

    public ConfiguracionProgramadaWorker(ProcesoWorker proceso, bool habilitado, TimeOnly horaInicioLocal, TimeOnly horaFinLocal, int cupoReservaDiaria, int? intervaloMinutos, bool ejecutarAlIniciar, int? tamanoLote, int? consultasVaciasParaPausa, int? diasPausaSinResultados, bool omitirConsultasRealizadasEnElDia)
    {
        Proceso = proceso;
        this.Actualizar(habilitado, horaInicioLocal, horaFinLocal, cupoReservaDiaria, intervaloMinutos, ejecutarAlIniciar, tamanoLote, consultasVaciasParaPausa, diasPausaSinResultados, omitirConsultasRealizadasEnElDia);
    }

    public ProcesoWorker Proceso { get; private set; }
    public bool Habilitado { get; private set; }
    public TimeOnly HoraInicioLocal { get; private set; }
    public TimeOnly HoraFinLocal { get; private set; }
    public int CupoReservaDiaria { get; private set; }
    public int? IntervaloMinutos { get; private set; }
    public bool EjecutarAlIniciar { get; private set; }
    public int? TamanoLote { get; private set; }
    public int? ConsultasVaciasParaPausa { get; private set; }
    public int? DiasPausaSinResultados { get; private set; }
    public bool OmitirConsultasRealizadasEnElDia { get; private set; }

    public void Actualizar(bool habilitado, TimeOnly horaInicioLocal, TimeOnly horaFinLocal, int cupoReservaDiaria, int? intervaloMinutos, bool ejecutarAlIniciar, int? tamanoLote, int? consultasVaciasParaPausa, int? diasPausaSinResultados, bool omitirConsultasRealizadasEnElDia)
    {
        if (cupoReservaDiaria < 0) throw new ArgumentOutOfRangeException(nameof(cupoReservaDiaria), "La reserva diaria no puede ser negativa.");

        if (Proceso is ProcesoWorker.EnriquecimientoDetalleDocumental or ProcesoWorker.ExpedienteDetallado)
        {
            if (intervaloMinutos is null or < 1) throw new ArgumentOutOfRangeException(nameof(intervaloMinutos), "El intervalo debe ser mayor a cero.");
            if (tamanoLote is null or < 1) throw new ArgumentOutOfRangeException(nameof(tamanoLote), "El tamaño de lote debe ser mayor a cero.");
            consultasVaciasParaPausa = null;
            diasPausaSinResultados = null;
            omitirConsultasRealizadasEnElDia = false;
        }
        else if (Proceso == ProcesoWorker.DescubrimientoExpedientes)
        {
            if (consultasVaciasParaPausa is null or < 1) throw new ArgumentOutOfRangeException(nameof(consultasVaciasParaPausa), "Las consultas vacías para pausa deben ser mayores a cero.");
            if (diasPausaSinResultados is null or < 1) throw new ArgumentOutOfRangeException(nameof(diasPausaSinResultados), "Los días de pausa deben ser mayores a cero.");
            intervaloMinutos = null;
            ejecutarAlIniciar = false;
            tamanoLote = null;
        }
        else
        {
            throw new InvalidOperationException("El proceso de Worker no está soportado.");
        }

        Habilitado = habilitado;
        HoraInicioLocal = horaInicioLocal;
        HoraFinLocal = horaFinLocal;
        CupoReservaDiaria = cupoReservaDiaria;
        IntervaloMinutos = intervaloMinutos;
        EjecutarAlIniciar = ejecutarAlIniciar;
        TamanoLote = tamanoLote;
        ConsultasVaciasParaPausa = consultasVaciasParaPausa;
        DiasPausaSinResultados = diasPausaSinResultados;
        OmitirConsultasRealizadasEnElDia = omitirConsultasRealizadasEnElDia;
        this.MarcarComoModificada();
    }
}
