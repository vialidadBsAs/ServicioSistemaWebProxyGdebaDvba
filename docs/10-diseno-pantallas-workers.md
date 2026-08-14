# Diseno de Pantallas de Workers: Alcance y Ejecuciones

Version: 0.2
Fecha: 14/08/2026
Estado: Diseno conceptual acordado; backend implementado (ver seccion 7 y
`07-arquitectura-tecnica-implementacion.md`); pantallas Angular pendientes

## 1. Proposito

Este documento registra el rediseno conceptual de la administracion de workers,
acordado durante el desarrollo de la rama `feature/configuracion-programada-workers`.
Reemplaza la organizacion actual del front (pestanas "Workers" y "Configuracion de
Workers" del feature `administracion` de ConsultaExpedientes) y define los conceptos
que el backend debe soportar.

No es un plan tecnico. Las entidades, endpoints y migraciones se definen despues,
sobre la base de estos conceptos.

## 2. Problema detectado

La organizacion actual mezcla responsabilidades:

- La pantalla de configuracion combina la politica de ejecucion programada
  (ventana, reserva, intervalo) con el alcance de datos (temas, tratas). Son
  responsabilidades distintas y la segunda es la dificil de presentar con claridad.
- El monitoreo muestra la misma historia en dos grillas: una solicitud manual nace
  en "Solicitudes activas" y, cuando el worker la toma, desaparece de ahi y
  reaparece como fila de "Ultimas ejecuciones". El registro se muda de seccion a
  mitad de su vida.
- La corrida programada automatica no aparece en ningun lado hasta que corre. No se
  puede responder "que va a pasar hoy" mirando la pantalla.

## 3. Conceptos

El subsistema se organiza sobre cinco conceptos:

1. **Proceso (worker)**: la capacidad tecnica. Catalogo que va a crecer
   (descubrimiento de expedientes, enriquecimiento documental, futuros).
2. **Politica de ejecucion**: regla permanente de cuando y cuanto puede correr solo
   un proceso: habilitacion, ventana horaria, cadencia, reserva de cupo, pausas.
   Es normativa: describe condiciones, no eventos.
3. **Alcance de datos**: que universo de datos procesa cada worker (temas, tratas,
   estados). Cada worker tiene un alcance genuinamente propio; no existe un interes
   institucional unico compartido entre procesos.
4. **Orden de trabajo**: la intencion de que un proceso corra. Tiene dos origenes:
   la genera la politica (automatica) o la crea una persona (manual).
5. **Ejecucion**: el hecho historico, inmutable, con resultados, metricas y consumo.

## 4. Decision: dos pantallas

### 4.1 Pantalla de alcance de datos

Configura exclusivamente el concepto 3: que datos alcanza cada worker, con los
parametros propios de cada uno (temas y excepciones por trata para descubrimiento;
temas para enriquecimiento documental). No contiene nada de ejecucion, ni
programada ni manual.

Es la pantalla dificil de disenar porque el alcance de cada worker es distinto y
no admite una interfaz generica. Su diseno detallado queda pendiente.

### 4.2 Pantalla de ejecuciones

Reune los conceptos 2, 4 y 5 en un solo lugar, organizados por eje temporal:

- **Politica de ejecucion programada**: se consulta y edita en esta pantalla
  (ventana, reserva, intervalo, habilitacion), no junto al alcance de datos.
- **Por ejecutarse (arriba)**: widgets de las corridas proximas, manuales o
  automaticas. Cada widget distingue de forma notoria su origen con palabra e
  icono (Automatica / Manual) y el tipo de worker con icono identificatorio.
- **Ejecutadas hoy (debajo, atenuadas)**: las corridas del dia ya completadas se
  muestran atenuadas, como confirmacion, con acceso a sus resultados y la
  proyeccion de la proxima corrida. Primero lo que viene, despues lo ejecutado.
- **Historico (al final)**: grilla de ejecuciones que crece con cada corrida, con
  posibilidad de consultar los resultados de cada una.

## 5. Ordenes manuales

La orden manual es un registro con identidad propia. Su ciclo de vida:

| Estado | Significado | Como se llega |
|---|---|---|
| `PendienteDeInicio` | Preparada, espera inicio manual | Crear sin horario |
| `Programada` | Espera su horario de inicio | Crear con fecha/hora de inicio |
| `Pendiente` | En cola; el worker la toma en su proximo ciclo | "Iniciar ahora" o llego su horario |
| `EnEjecucion` | El worker la tomo; queda vinculada a la ejecucion | Toma del worker |
| `Finalizada` / `Fallida` | Cerrada segun el resultado de la ejecucion | Fin de la ejecucion |
| `Cancelada` | Anulada antes de ser tomada, con rastro de quien y cuando | Accion "Cancelar" |

Reglas:

- Una orden con horario (`Programada`) se encola sola al llegar la hora; "Iniciar
  ahora" la fuerza antes.
- Solo puede cancelarse una orden que el worker todavia no tomo
  (`PendienteDeInicio`, `Programada`, `Pendiente`).

## 6. Corridas automaticas: proyeccion, no registro

La corrida programada automatica **no se materializa como registro** antes de
correr. El widget "por ejecutarse" de una corrida automatica es una proyeccion
calculada a partir de la politica vigente y el historial de ejecuciones:

- Descubrimiento (diaria): si esta habilitado y no corrio en la fecha local, la
  proxima corrida es hoy al inicio de la ventana (o de inmediato si ya esta dentro).
- Enriquecimiento (por intervalo): proxima corrida = ultima corrida persistida mas
  el intervalo, dentro de la ventana.

Se descarto materializar las ordenes automaticas porque el worker por intervalo
generaria un flujo continuo de registros y cada cambio de politica dejaria ordenes
materializadas obsoletas. La proyeccion es consistente con la politica por
construccion.

En consecuencia, "cancelar" significa algo distinto segun la naturaleza:

- **Orden manual**: se cancela el registro (estado `Cancelada`).
- **Corrida automatica diaria**: "omitir hoy", una marca de supresion persistida
  por fecha; al dia siguiente la proyeccion reaparece sola.
- **Corrida automatica por intervalo**: solo pausar/reanudar (habilitacion de la
  politica). No se saltean ticks individuales.

## 7. Implicancias para el backend

Elementos que este diseno requiere, ya implementados:

- Fecha/hora de inicio opcional en la solicitud manual y estado `Programada`, con
  promocion automatica a `Pendiente` al llegar el horario (la realiza el Worker al
  tomar solicitudes).
- Estado `Cancelada` en la solicitud manual, con usuario y fecha de cancelacion.
  Endpoint: `POST /api/gdeba/workers/solicitudes/{id}/cancelar`.
- Marca persistida de omision de la corrida programada diaria por fecha
  (`Worker_OmisionesCorridaProgramada`). Endpoints:
  `PUT`/`DELETE /api/gdeba/workers/{proceso}/omisiones/hoy`. Solo aplica al
  descubrimiento; el Worker registra una ejecucion `Omitida` con el operador.
- Consulta por proceso que alimenta los widgets:
  `GET /api/gdeba/workers/{proceso}/panel`, con configuracion, proyeccion de la
  corrida automatica, ordenes vivas, ejecuciones del dia e historico.

Se conserva sin cambios el modelo de `EjecucionWorker` y sus resultados por
trata-estado, la politica persistida en `Worker_ConfiguracionesProgramadas` y la
resistencia a reinicios documentada en `07-arquitectura-tecnica-implementacion.md`.

## 8. Referencias

- Front Angular: repositorio `ConsultaExpedientes` (mismo directorio padre que este
  repositorio), feature `administracion`.
- Estado actual de workers y configuracion: `07-arquitectura-tecnica-implementacion.md`,
  secciones de workers y alcance de datos.
