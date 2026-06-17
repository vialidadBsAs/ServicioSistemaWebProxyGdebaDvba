# Politica de Cache y Sincronizacion

## Objetivo

Reducir llamadas innecesarias a GDEBA, mejorar tiempos de respuesta internos y permitir continuidad operativa parcial ante interrupciones de conectividad, manteniendo un equilibrio razonable entre frescura y costo de actualizacion.

## Situacion Actual

Los servicios autorizados actuales no proveen, segun la documentacion disponible, filtros incrementales suficientes para sincronizar solo cambios recientes.

En particular:

- `buscarDatosExpedientePorCodigosTrata` permite consultar por codigo de trata, estado y usuario, pero no por reparticion ni fecha.
- `buscarHistorialPasesExpediente` retorna historial completo, pero no permite consultar desde una fecha determinada.
- `fechaModificacion`, `motivo` y `usuarioAnterior` pueden no venir informados en `buscarDatosExpedientePorCodigosTrata`.
- La ausencia de esos campos no debe considerarse evidencia de que no existan pases o movimientos.

## Estrategia Inicial

Mientras no existan filtros remotos adicionales:

- Ejecutar cargas generales controladas por trata/estado/usuario.
- Filtrar localmente expedientes de interes, por ejemplo reparticion `DVMIYSPGP`.
- Persistir expedientes unicos en SQL Server.
- Consultar detalles, historial o documentos solo para expedientes relevantes.
- Evitar refrescos masivos demasiado frecuentes.
- Registrar fecha de consulta y fuente de cada dato.

## Estrategia Futura

Si GDEBA habilita filtros por reparticion o fecha:

- Sincronizar por reparticion.
- Sincronizar desde una fecha de corte.
- Reducir consultas masivas.
- Mejorar frescura sin aumentar trafico.
- Mantener la misma API interna y reemplazar solo la estrategia de sincronizacion.

## Estrategias de Sincronizacion

Se recomienda modelar la sincronizacion como estrategia reemplazable:

```csharp
public interface IExpedienteSyncStrategy
{
    Task<SynchronizationResult> SyncAsync(
        SyncContext context,
        CancellationToken cancellationToken);
}
```

Implementaciones posibles:

- `FullScanByTrataSyncStrategy`: modo actual basado en consulta general por trata.
- `FilteredByReparticionSyncStrategy`: modo futuro si GDEBA permite filtrar por reparticion.
- `IncrementalByFechaSyncStrategy`: modo futuro si GDEBA permite filtrar por fecha.

En fase inicial solo debe implementarse lo necesario. Las otras estrategias pueden quedar previstas por arquitectura y documentacion.

## Frescura de Datos

Las respuestas internas deberian informar metadatos de frescura:

```json
{
  "source": "cache",
  "cachedAt": "2026-05-28T10:15:00-03:00",
  "expiresAt": "2026-05-28T10:30:00-03:00",
  "data": {}
}
```

Campos recomendados en la base local:

- `FechaPrimeraDeteccion`
- `FechaUltimaConsultaGdeba`
- `FechaUltimaActualizacionLocal`
- `FuenteUltimaRespuesta`
- `EstaCompleto`
- `TieneDatosParciales`
- `HashContenido`

Estos campos pertenecen al control de cache, no a las entidades puras de datos GDEBA. En el modelo persistente inicial se separan en entidades como `ExpedienteCacheControl`, `HistorialExpedienteCacheControl`, `DocumentoCacheControl` y `TrataCacheControl`.

Los archivos documentales no se guardan como binarios en SQL Server en esta etapa. Se guardan local o externamente, y la base conserva referencias y metadatos mediante `DocumentoArchivoLocal`.

## Enriquecimiento Documental

`DocumentoGdeba` puede nacer con informacion parcial cuando aparece en un expediente. En ese momento se conoce el numero de actuacion, pero no necesariamente el numero especial, firmantes, referencia completa, URL del archivo o historial documental.

La metadata faltante se enriquece consultando `buscarDetallePorNumero` del servicio `ws_gdeba_consultaDocumento`. Esta operacion puede ejecutarse de dos formas:

- Manualmente, para un documento puntual identificado localmente.
- En segundo plano, mediante el worker, para un lote de documentos con `MetadataCompleta = false`.

La operacion unitaria de enriquecimiento pertenece a Application y reutiliza el aggregate `DocumentoGdeba` para aplicar cambios de metadata e historial. El procesamiento por lote no duplica esa logica: solo selecciona pendientes y reutiliza la operacion unitaria.

El worker decide cuando ejecutar, que operacion controlar y que lote autorizar. Antes de procesar consulta el consumo diario disponible mediante `IConsultaCuotasGdeba`, respeta la ventana no pico configurada y registra las invocaciones con origen `WorkerProgramado` cuando el gateway SOAP responde.

## TTL Sugeridos Iniciales

Estos valores son orientativos y deben ajustarse con uso real:

| Tipo de dato | TTL sugerido | Motivo |
|---|---:|---|
| Tratas | 24 horas o mas | Catalogo de baja variacion. |
| Tipos documentales | 24 horas o mas | Catalogo de baja variacion. |
| Documento GEDO metadata | 12 a 24 horas | Documento firmado suele ser estable. |
| PDF documento oficial | Cache prolongada | El contrato sugiere cache porque documentos firmados no pueden eliminarse. |
| Expediente detalle | 15 a 60 minutos | Puede cambiar durante tramitacion. |
| Historial de pases | 15 a 60 minutos | Puede cambiar durante tramitacion. |
| BuscarDatosExpedientePorCodigosTrata | Programado/controlado | Consulta masiva; evitar alta frecuencia. |

## Resiliencia

Ante indisponibilidad de GDEBA:

- Responder desde cache si existe informacion local.
- Indicar que la fuente es cache y la fecha de ultima actualizacion.
- Registrar el error externo.
- Evitar reintentos agresivos.
- Aplicar circuit breaker o politica equivalente si hay errores repetidos.

## Dedupe y Concurrencia

El proxy debe evitar llamadas simultaneas duplicadas al mismo recurso externo.

Ejemplo:

- Si varias aplicaciones solicitan el mismo expediente al mismo tiempo y la cache esta vencida, solo una deberia consultar GDEBA.
- Las otras pueden esperar el resultado o recibir cache vencida marcada como stale si la politica lo permite.

