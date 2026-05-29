# Servicios Autorizados

Este documento lista los contratos aprobados disponibles para el Servicio Sistema Web Proxy GDEBA-DVBA.

## Autorizacion JWT

La autorizacion tecnica hacia GDEBA se realiza mediante un servicio REST JWT:

- Endpoint PROD: `https://iop.gba.gob.ar/servicios/JWT/1/REST/jwt`
- Esquema de autenticacion: header `Authorization` con Basic Auth.
- Credenciales: `username` y `password` provistos por GDEBA/IOP.
- Manejo requerido: las credenciales no deben almacenarse en codigo fuente ni registrarse en logs.

Pendiente de confirmar:

- Endpoint HML del servicio JWT.
- Vigencia del token.
- Header exacto requerido para enviar el token al consumir SOAP.
- Comportamiento ante expiracion o token invalido.

## Consideraciones Transversales SOAP

Todos los contratos revisados corresponden a SOAP 1.0.

Para peticiones XML con caracteres especiales o acentuacion, se debe enviar explicitamente charset UTF-8:

```http
Content-Type: application/xml; charset=UTF-8
```

Segun pruebas y respuesta tecnica recibida, enviar solo `application/xml` puede no producir error, pero devolver datos vacios si la peticion contiene parametros con caracteres especiales o acentos.

## Resumen de Servicios

| Metodo | Servicio GDEBA | WSDL PROD | WSDL HML |
|---|---|---|---|
| `buscarCodigoCaratulaPorNumeroExpediente` | `ws_gdeba_consultaExpediente` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` |
| `buscarDatosExpedientePorCodigosTrata` | `ws_gdeba_consultaExpediente` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` |
| `buscarExpediente` | `ws_gdeba_consultaExpediente` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` |
| `buscarHistorialPasesExpediente` | `ws_gdeba_consultaExpediente` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` |
| `consultarExpedienteDetallado` | `ws_gdeba_consultaExpediente` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` |
| `validarExpediente` | `ws_gdeba_consultaExpediente` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl` |
| `buscarDetallePorNumero` | `ws_gdeba_consultaDocumento` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` |
| `buscarDocumentoEnExpedientes` | `ws_gdeba_consultaDocumento` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` |
| `buscarPDFPorNumero` | `ws_gdeba_consultaDocumento` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` |
| `buscarPorNumero` | `ws_gdeba_consultaDocumento` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaDocumento?wsdl` |
| `buscarTratasPorCodigo` | `ws_gdeba_tratas` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/tratas?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/tratas?wsdl` |
| `consultarTipoDocumento` | `ws_gdeba_consultaTipoDocumento` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaTipoDocumento?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaTipoDocumentoService?wsdl` |
| `esEstadoPaseExpedienteValido` | `ws_gdeba_consultaEstadoPaseExpediente` | `https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaEstadoPaseExpediente?wsdl` | `https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaEstadoPaseExpediente?wsdl` |

## Detalle por Metodo

### buscarCodigoCaratulaPorNumeroExpediente

Permite obtener el numero de la Providencia cuya referencia es "Modificacion Caratula", vinculada automaticamente al expediente cuando se modifica la caratula original.

Entrada:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente a consultar. |

Salida:

| Parametro | Descripcion |
|---|---|
| `numeroCaratula` | Numero GDEBA del documento PV. |

### buscarDatosExpedientePorCodigosTrata

Recibe codigo de tramite, estado y usuario. Devuelve expedientes asociados a esos datos, junto con informacion adicional.

Entrada:

| Parametro | Descripcion |
|---|---|
| `codigosTrata` | Codigo de tramite a consultar. |
| `estadoDestino` | Estado de los expedientes que debe traer la consulta. |
| `usuario` | Usuario GDEBA cuyos expedientes tramitados se consultan. |

Salida:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente que coincide con los datos de entrada. |
| `codigoTrata` | Codigo de tramite del expediente. |
| `descripcionTrata` | Descripcion asociada al codigo de tramite. |
| `estado` | Estado del expediente en GDEBA. |
| `fechaModificacion` | Fecha de modificacion del expediente listado. |
| `motivo` | Motivo del expediente listado. |
| `usuarioAnterior` | Usuario GDEBA anterior apoderado del expediente listado. |

Observaciones:

- Si el codigo de expediente es inexistente o tiene formato incorrecto, el servicio no funciona correctamente.
- Si `estadoDestino` no respeta la denominacion correcta de estados GDEBA, el servicio no arroja datos.
- El contrato indica que puede suceder que no se informen `fechaModificacion`, `motivo` o `usuarioAnterior`.
- La ausencia de esos campos no debe interpretarse como inexistencia de pases o movimientos asociados.

### buscarExpediente

Obtiene informacion de un expediente existente, incluyendo documentos presentes. Retorna error si el expediente no existe.

Entrada:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente. |

Salida principal:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente consultado. |
| `codigoTrata` / `codigotrata` | Codigo de tramite. |
| `descripcionTrata` | Descripcion del tramite. |
| `estado` | Estado del expediente. |
| `archivosAdjuntos` | Lista de documentos de trabajo adjuntos. |
| `documentosOficiales` | Lista de documentos oficiales vinculados. |
| `expedientesAsociados` | Lista de expedientes asociados. |
| `listaDatosTarea` | Datos de tarea asociados al expediente. |
| `sistemaOrigen` | Sistema generador del expediente. |

### buscarHistorialPasesExpediente

Retorna el historial de pases realizados sobre un expediente, con documentacion vinculada y expedientes asociados.

Entrada:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente. |

Salida destacada:

| Grupo | Campos relevantes |
|---|---|
| `documentosVinculados` | `fechaCreacion`, `fechavinculacionDefinitiva`, `numeroEspecialDocumento`, `numeroGDEBADocumento`, `referencia`, `tipodeDocumento`, `usuarioAsociacion`, `usuarioGenerador` |
| `expedientesAsociados` | `codigoExpediente`, `descTrataExAsociado`, `fechaAsociacion`, `trataExpedienteASociado`, `usuarioAsociador` |
| `expedientesFusionAsociados` | `codigoExpediente`, `codigoTrata`, `descripcionTrata` |
| `expedientesVinculados` | `codigoExpediente`, `descTrataExVinculado`, `fechaVinculacion`, `trataExpedienteVinculado`, `usuarioVinculador` |
| `historialDeOperacion` | `Destinatario`, `destinoPaseCodigoReparticion`, `destinoPaseCodigoSector`, `destinoPaseDescripcionReparticion`, `destinoPaseDescripcionSector`, `estado`, `Expediente`, `fechaOperacion`, `idExpediente`, `motivo`, `origenPaseCodigoReparticion`, `origenPaseCodigoSector`, `origenPaseDescripcionReparticion`, `origenPaseDescripcionSector`, `tipoOperacion`, `usuario` |

Necesidad identificada:

- Seria conveniente contar con un filtro por fecha para trabajar incrementalmente y evitar recuperar siempre el historial completo.

### consultarExpedienteDetallado

Retorna informacion detallada del expediente, incluyendo descripcion del tramite, fecha de caratulacion, usuario caratulador, usuario destino y documentos presentes.

Entrada:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente. |

Salida destacada:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente. |
| `codigoTrata` | Codigo de tramite. |
| `descripcionTrata` | Descripcion del tramite. |
| `estado` | Estado al momento de la consulta. |
| `archivosAdjuntos` | Documentos de trabajo adjuntos. |
| `documentosOficiales` | Documentos oficiales vinculados. |
| `expedientesAsociados` | Expedientes asociados. |
| `sistemaOrigen` | Sistema generador. |
| `descripcionTramite` | Descripcion del tramite consultado. |
| `fechaCaratulacion` | Fecha de caratulacion. |
| `usuarioCaratulador` | Usuario caratulador. |
| `usuarioDestino` | Usuario GDEBA que posee el expediente actualmente. |
| `sectorDestino` | Sector GDEBA que posee el expediente actualmente. |
| `listaExpedientesAsociados` | Expedientes asociados con indicador `esCabecera` y `numeroGDEBA`. |
| `listaExpedientesAsociadosFusion` | Expedientes fusionados con indicador `esCabecera` y `numeroGDEBA`. |
| `listaExpedientesAsociadosTC` | Expedientes en tramitacion conjunta con indicador `esCabecera` y `numeroGDEBA`. |

### validarExpediente

Valida si un numero GDEBA de expediente es valido.

Entrada:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente. |

Salida:

| Parametro | Descripcion |
|---|---|
| `valido` | `TRUE` si el expediente es valido, `FALSE` en caso contrario. |

### buscarDetallePorNumero

Consulta el detalle de un documento GEDO por numero GDEBA o numero especial, incluyendo historial de actividades.

Entrada:

| Parametro | Tipo | Descripcion | Requerido |
|---|---|---|---|
| `Assignee` | Boolean | No aplica; se debe asignar `true` o `false`. | SI |
| `numeroDocumento` | String | Numero GDEBA del documento. Excluyente con `numeroEspecial`. | SI/NO |
| `numeroEspecial` | String | Numero GDEBA especial del documento. Excluyente con `numeroDocumento`. | SI/NO |
| `usuarioConsulta` | String | Usuario GDEBA que realiza la consulta. | SI |

Salida destacada:

| Parametro | Descripcion |
|---|---|
| `fechaCreacion` | Fecha de creacion del documento. |
| `listaFirmantes` | Lista de usuarios firmantes. |
| `listaHistorial` | Historial del documento. |
| `numeroDocumento` | Numero GDEBA. |
| `puedeVerDocumento` | Indica si puede ver el documento. |
| `referencia` | Referencia asociada. |
| `tipoDocumento` | Acronimo y descripcion del tipo de documento. |
| `urlArchivo` | Direccion del GEDO en formato PDF. |

### buscarDocumentoEnExpedientes

Devuelve los expedientes en los que se encuentra vinculado un documento.

Entrada:

| Parametro | Descripcion |
|---|---|
| `numeroDocumento` | Numero GEDO a consultar. |
| `usuarioConsulta` | Usuario GDEBA que realiza la consulta. |

Salida:

| Parametro | Descripcion |
|---|---|
| `listadoExpedientes` | Numeros de expedientes donde se encuentra vinculado el documento. |

Observacion:

- Si el documento no fue generado en el ambiente consultado o el numero es inexacto, el servicio puede devolver fault indicando que no se genero en GEDO un documento con ese numero o que el numero no es valido.

### buscarPDFPorNumero

Devuelve el contenido PDF de un documento GEDO codificado en Base64.

Entrada:

| Parametro | Tipo | Descripcion | Requerido |
|---|---|---|---|
| `assignee` | Boolean | No aplica; se debe asignar `false`. | NO |
| `numeroDocumento` | String | Numero GDEBA del documento. Excluyente con `numeroEspecial`. | SI/NO |
| `numeroEspecial` | String | Numero GDEBA especial del documento. Excluyente con `numeroDocumento`. | SI/NO |
| `usuarioConsulta` | String | Usuario GDEBA que realiza la consulta. | SI |

Salida:

| Parametro | Descripcion |
|---|---|
| `return` | Contenido del documento codificado en Base64. |

Observacion del contrato:

- Se sugiere implementar cache interna, ya que los documentos firmados no pueden ser eliminados.

### buscarPorNumero

Devuelve informacion de un documento GEDO, sin incluir su contenido.

Entrada:

| Parametro | Descripcion |
|---|---|
| `Assignee` | No aplica; se debe asignar `true` o `false`. |
| `numeroDocumento` | Numero GDEBA del documento. Excluyente con `numeroEspecial`. |
| `numeroEspecial` | Numero GDEBA especial del documento. Excluyente con `numeroDocumento`. |
| `usuarioConsulta` | Usuario GDEBA que realiza la consulta. |

Salida:

| Parametro | Descripcion |
|---|---|
| `fechaCreacion` | Fecha de creacion del documento. |
| `motivo` | Motivo de la operacion listada. |
| `numeroDocumento` | Numero GDEBA del documento. |
| `sistemaOrigen` | Sistema generador. |
| `tipoDocumento` | Acronimo y descripcion del tipo documental. |
| `urlArchivo` | Direccion del GEDO en PDF. |
| `usuarioGenerador` | Usuario generador. |
| `usuarioIniciador` | Usuario iniciador. |

### buscarTratasPorCodigo

Obtiene informacion asociada al codigo GDEBA de un tramite.

Entrada:

| Parametro | Descripcion |
|---|---|
| `codigoTrata` | Codigo de tramite, por ejemplo `OTR0017`. |

Salida:

| Parametro | Descripcion |
|---|---|
| `acronimoGedo` | Acronimo del tipo de documento GEDO asociado como caratula variable. |
| `codigoTrata` | Codigo de tramite consultado. |
| `descripcionTrata` | Descripcion asociada. |
| `esAutomatica` | Atributo interno; `True`/`False`. |
| `esTrataManual` | Atributo interno; `True`/`False`. |
| `estado` | Estado de vigencia. |
| `idTrata` | ID interno del tramite. |
| `tipoReserva` | Informacion de reserva: `descripcion`, `idTipoReserva`, `descripcionTipoReserva`. |

### consultarTipoDocumento

Obtiene el detalle de un tipo de documento.

Entrada:

| Parametro | Descripcion | Requerido |
|---|---|---|
| `Acronimo` | Acronimo del tipo de documento. | Si |

Salida:

| Parametro | Descripcion |
|---|---|
| `Acronimo` | Acronimo del tipo de documento. |
| `codigoTipoDocumentoGDEBA` | Acronimo del documento. |
| `descripcion` | Descripcion del tipo documental. |
| `esAutomatica` | Atributo interno. |
| `esComunicable` | Indica si pertenece a CCOO. |
| `esConfidencial` | Indica atributo confidencial. |
| `esEmbebido` | Permite embebidos. |
| `esEspecial` | Indica numeracion especial. |
| `esFirmaConjunta` | Requiere firma conjunta. |
| `esFirmaExterna` | Permite PDF con firma externa. |
| `esManual` | Atributo interno. |
| `esNotificable` | Atributo interno. |
| `estado` | Estado de vigencia. |
| `familia` | Familia documental. |
| `nombre` | Nombre del documento. |
| `tieneTemplate` | Indica si tiene template. |
| `tieneToken` | Indica si posee firma con token. |
| `tipoProduccion` | Tipo de produccion. |

### esEstadoPaseExpedienteValido

Indica si un estado destino es valido para el pase de un expediente.

Entrada:

| Parametro | Descripcion |
|---|---|
| `numeroExpediente` | Numero GDEBA del expediente. |
| `estadoDestino` | Estado que se desea consultar. |

Salida:

| Parametro | Descripcion |
|---|---|
| `Return` | `True` si es posible utilizar el estado ingresado; si no, `False`. |

Observaciones:

- La nomenclatura del estado debe coincidir con GDEBA, incluyendo mayusculas y tildes.
- Estados de ejemplo: `Iniciacion`, `Tramitacion`, `Comunicacion`, `Guarda Temporal`.
- Si se ingresa un estado no permitido para el expediente, retorna `False`.

