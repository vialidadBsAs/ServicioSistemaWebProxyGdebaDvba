# TEMP - Retomar correccion de documentos vinculados

Fecha: 2026-06-09

Este documento es una nota temporal para retomar el trabajo. No es documentacion final del proyecto.

## Punto de partida

Durante las pruebas con `EX-2022-39560462- -GDEBA-DVMIYSPGP` se confirmo que la grilla de GDEBA "Documentos / Con Pase" no se corresponde con `historialDeOperacion`.

El response de `buscarHistorialPasesExpediente` contiene dos colecciones distintas:

- `documentosVinculados`: 10 documentos. Estos reproducen la grilla de documentos con pase.
- `historialDeOperacion`: 8 operaciones/pases administrativos.

La confusion inicial fue tratar el historial como si solo fueran movimientos. El contrato y el XML real muestran que el metodo devuelve mas informacion.

## XML relevante

Ejemplo de un documento vinculado:

```xml
<documentosVinculados>
    <fechaCreacion>2022-11-17T10:25:15-03:00</fechaCreacion>
    <fechavinculacionDefinitiva>2022-11-17T10:25:32-03:00</fechavinculacionDefinitiva>
    <numeroDocumentoGDEBA>PV-2022-39560917-GDEBA-DVMIYSPGP</numeroDocumentoGDEBA>
    <referencia>Nota de la Municipalidad de la Costa</referencia>
    <tipodeDocumento>PROIM</tipodeDocumento>
    <usuarioAsociacion>RCOLABIANCHI</usuarioAsociacion>
    <usuarioGenerador>RCOLABIANCHI</usuarioGenerador>
</documentosVinculados>
```

Estos datos deben poder persistirse sin mezclarse semanticamente con otras formas de relacion documental.

## Cambio hecho hoy

Se agrego soporte de aplicacion para que `buscarHistorialPasesExpediente` ya no devuelva solamente movimientos:

- `GdebaHistorialExpedienteDto`
- `DocumentosVinculados`
- `Movimientos`
- `Relaciones`

Tambien se modifico `SoapGdebaExpedienteGateway` para mapear:

- `documentosVinculados`
- `historialDeOperacion`
- `expedientesAsociados`
- `expedientesFusionAsociados`
- `expedientesVinculados`

Y se modifico `ExpedienteService` para consolidar los documentos del historial.

## Problema detectado

La consolidacion actual guarda `documentosOficiales` y `documentosVinculados` en la misma relacion `ExpedienteDocumentos` sin distinguir la naturaleza del vinculo.

Esto esta mal modelado.

`FuenteDeteccion` no alcanza para resolverlo porque solo indica por que metodo fue detectado el dato, no que significa el documento dentro del expediente.

Ejemplo:

- `FuenteDeteccion = ConsultarExpedienteDetallado` significa que vino por el metodo de detalle.
- `FuenteDeteccion = BuscarHistorialPasesExpediente` significa que vino por historial.

Pero falta distinguir:

- documento oficial del expediente;
- documento vinculado del historial;
- eventualmente otros tipos de relacion documental.

## Correccion pendiente

Agregar un concepto de dominio para clasificar el vinculo documento-expediente.

Nombre tentativo:

```csharp
TipoVinculoDocumentoExpediente
```

Valores iniciales:

```csharp
Oficial
VinculadoHistorial
```

Opcionales a evaluar:

```csharp
DocumentoTrabajo
Otro
```

## Decision de modelado a tomar

Hay dos alternativas:

### Alternativa A - Una fila por expediente-documento con flags

Tabla `ExpedienteDocumentos`:

- `EsOficial`
- `EsVinculadoHistorial`
- datos de vinculacion

Ventaja: evita duplicar filas para el mismo documento.

Desventaja: mezcla en una fila datos que pueden pertenecer a vinculos distintos. Por ejemplo, la fecha de vinculacion del historial no necesariamente describe el hecho de ser documento oficial.

### Alternativa B - Una fila por expediente-documento-tipo vinculo

Tabla `ExpedienteDocumentos`:

- `ExpedienteId`
- `DocumentoId`
- `TipoVinculo`
- `FechaVinculacion`
- `OrdenRespuesta`
- `UsuarioAsociacion`
- `UsuarioGenerador`
- `FuenteDeteccion`

Indice unico:

```text
ExpedienteId + DocumentoId + TipoVinculo
```

Ventaja: modela correctamente que un mismo documento puede tener mas de una relacion semantica con el expediente.

Desventaja: puede haber mas de una fila para el mismo documento dentro del mismo expediente, pero con distinto tipo de vinculo.

Recomendacion actual: Alternativa B.

## Regla esperada

Cuando el documento viene de:

```text
consultarExpedienteDetallado.documentosOficiales
```

debe guardarse como:

```text
TipoVinculo = Oficial
FuenteDeteccion = ConsultarExpedienteDetallado
```

Cuando el documento viene de:

```text
buscarHistorialPasesExpediente.documentosVinculados
```

debe guardarse como:

```text
TipoVinculo = VinculadoHistorial
FuenteDeteccion = BuscarHistorialPasesExpediente
```

## Orden de documentos

Para reproducir la grilla de GDEBA:

- calcular `OrdenRespuesta` sobre `documentosVinculados` por `fechavinculacionDefinitiva ASC`;
- el documento mas antiguo queda con orden 1;
- el mas reciente queda con orden N;
- para mostrar como GDEBA, ordenar por `OrdenRespuesta DESC`.

Si no hay fecha de vinculacion, usar:

1. `FechaVinculacion`
2. `FechaCreacion`
3. `NumeroActuacionCompleto`

## Correcciones tecnicas esperadas manana

1. Agregar enum `TipoVinculoDocumentoExpediente` en Domain.
2. Agregar propiedad `TipoVinculo` a `ExpedienteDocumento`.
3. Cambiar constructor/logica de `ExpedienteDocumento` para registrar el tipo de vinculo.
4. Cambiar `Expediente.RegistrarDocumentoDetectado` para recibir `TipoVinculoDocumentoExpediente`.
5. Cambiar busqueda interna de relacion:

```csharp
DocumentoId + TipoVinculo
```

en lugar de solo:

```csharp
DocumentoId
```

6. Cambiar configuracion EF:

```text
Indice unico: ExpedienteId + DocumentoId + TipoVinculo
```

7. Crear migracion.
8. Ajustar `ExpedienteService`:

- detalle: `TipoVinculo = Oficial`;
- historial/documentosVinculados: `TipoVinculo = VinculadoHistorial`.

9. Revisar DTO de salida para decidir si conviene exponer `TipoVinculo`.
10. Probar con `EX-2022-39560462- -GDEBA-DVMIYSPGP`.

## Cuidado

No ejecutar `Update-Database` sin confirmacion expresa.

No mezclar `documentosVinculados` con `historialDeOperacion`.

No usar `buscarDocumentoEnExpedientes` para listar documentos de un expediente. Ese metodo es una consulta inversa: documento a expedientes.
