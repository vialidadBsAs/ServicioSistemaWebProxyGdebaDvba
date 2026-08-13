# Pendientes y Consultas a GDEBA

## Historial y estado actual de documentos GEDO

Pendiente de diagnóstico:

- Obtener la respuesta SOAP real de `buscarDetallePorNumero` para un documento conocido en `Iniciación` o `Confeccionar`.
- Comparar cada entrada de `listaHistorial` con las filas persistidas en `HistorialDocumentosGdeba`.
- Verificar que el parser soporte la estructura real de la colección y no descarte entradas anidadas.
- Confirmar si `FechaFin`, `FechaInicio` o un campo específico de estado representa el estado actual del documento.

Motivo:

- La grilla muestra la actividad de mayor fecha como “Última actividad”. Actualmente predominan `Firmar Documento` y la ausencia de filas se presenta como “Sin historial local”.
- No se puede concluir si todos los documentos enriquecidos están firmados, si GDEBA devuelve solo la actividad final o si el historial se está capturando de forma incompleta.

No registrar XML SOAP completo ni datos sensibles en logs. Para la comparación, usar una muestra controlada y conservar solo los campos funcionales necesarios.

## Filtros para buscarDatosExpedientePorCodigosTrata

Solicitud planteada:

- Agregar filtro por reparticion/dependencia.
- Alternativamente, crear metodo especifico para `DVMIYSPGP`.
- Evaluar a futuro si `codigosTrata` puede ser opcional o condicional para recuperar expedientes de una reparticion sin depender de una trata especifica.

Motivo:

- Evitar recuperar la totalidad de expedientes para filtrar localmente.
- Reducir procesamiento local.
- Reducir consumo de servicios externos.
- Facilitar sincronizacion de cache.

## Campos fechaModificacion, motivo y usuarioAnterior

El contrato indica que puede suceder que no se informen:

- `fechaModificacion`
- `motivo`
- `usuarioAnterior`

Consulta pendiente:

- Bajo que condiciones se informan.
- Si corresponden al ultimo pase, a la ultima tarea asociada al usuario consultado, al usuario anterior apoderado o a otra fuente interna.
- Si la ausencia debe considerarse dato parcial.

Observacion:

- Se verificaron expedientes que poseen pases en GDEBA, pero no devuelven esos campos en `buscarDatosExpedientePorCodigosTrata`.
- Por lo tanto, la ausencia de esos campos no debe interpretarse como inexistencia de pases.

## Filtros para buscarHistorialPasesExpediente

Solicitud planteada:

- Permitir consultar pases o movimientos desde una fecha determinada.
- Evaluar uso de `fechavinculacionDefinitiva`, `fechaOperacion` u otro criterio confiable.

Motivo:

- Evitar recuperar el historial completo en cada invocacion.
- Permitir sincronizacion incremental.
- Reducir llamadas y procesamiento local.

## Cuotas y Limites

Pendiente de confirmar:

- Si existen limites diarios.
- Si existen limites por minuto.
- Si hay politicas de throttling.
- Si la cache local institucional es recomendada por GDEBA.

## JWT

Pendiente de confirmar:

- Endpoint HML.
- Vigencia del token.
- Header exacto para consumir SOAP con token.
- Comportamiento ante expiracion.
- Si existe margen de renovacion recomendado.

## Charset UTF-8

Pendiente de documentacion formal:

- Confirmar que peticiones XML deben enviarse con `Content-Type: application/xml; charset=UTF-8`.
- Documentar que sin charset, parametros con acentos o caracteres especiales pueden provocar respuestas vacias sin fault SOAP.

