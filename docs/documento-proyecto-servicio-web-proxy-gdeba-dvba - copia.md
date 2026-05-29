# Proyecto Servicio Sistema Web Proxy GDEBA-DVBA

## Documento de Fundamentacion, Alcance y Arquitectura Propuesta

Version: 0.1  
Fecha: 28/05/2026  
Estado: Borrador de trabajo

## 1. Introduccion

La Direccion Provincial de Vialidad de la Provincia de Buenos Aires requiere integrar distintos sistemas internos con la plataforma GDEBA, a fin de consultar y eventualmente operar sobre expedientes, documentos electronicos, tratas, tipos documentales y demas informacion administrativa disponible mediante servicios web autorizados.

En la situacion actual, este tipo de integracion puede quedar incorporado dentro de aplicaciones especificas. Si bien esto permite resolver necesidades puntuales, a medida que aumenta la cantidad de sistemas consumidores tambien aumenta la duplicacion de logica tecnica, la dispersion de credenciales, la dificultad para auditar consumos y el riesgo de realizar llamadas repetidas o innecesarias hacia servicios externos.

Con el objetivo de ordenar este escenario, se propone la implementacion del **Servicio Sistema Web Proxy GDEBA-DVBA**, una capa institucional de interoperabilidad entre los sistemas internos de DVBA y los servicios autorizados de GDEBA.

Este servicio actuara como punto unico de comunicacion tecnica con GDEBA. Las aplicaciones internas no deberan conocer los detalles de autenticacion, contratos SOAP, encabezados HTTP, serializacion XML, codificacion de caracteres, endpoints externos ni politicas de reintento. En su lugar, consumiran una API institucional propia, diseñada para las necesidades internas de la DVBA y gobernada bajo criterios comunes de seguridad, trazabilidad, cache y disponibilidad.

## 2. Situacion Actual y Problema a Resolver

Los sistemas internos de la DVBA requieren acceder a informacion contenida en GDEBA para acompañar procesos vinculados con proyectos, licitaciones, seguimiento, ejecucion y certificacion de obras.

Entre las necesidades identificadas se encuentran:

- Consultar expedientes electronicos.
- Consultar el detalle de expedientes.
- Consultar historial de pases y movimientos.
- Consultar documentos vinculados.
- Obtener informacion de documentos GEDO.
- Descargar documentos PDF oficiales.
- Consultar tratas y tipos documentales.
- Validar expedientes y estados de pase.

Los contratos aprobados por IOP permiten consumir un conjunto definido de servicios SOAP. Adicionalmente, la autorizacion tecnica requiere obtener un token JWT mediante un servicio REST con Basic Auth.

La integracion directa desde cada sistema consumidor presenta varios inconvenientes:

- Cada aplicacion deberia administrar credenciales o mecanismos de autenticacion.
- Cada aplicacion deberia implementar clientes SOAP y manejo de XML.
- Cada aplicacion deberia conocer particularidades tecnicas como `Content-Type` con charset UTF-8.
- Se duplicarian criterios de cache, errores, reintentos y auditoria.
- Aumentaria el numero de llamadas directas a GDEBA.
- Resultaria mas dificil identificar institucionalmente que sistema consulto que informacion.
- Cualquier cambio futuro en GDEBA podria impactar en multiples aplicaciones.

El problema no es solamente tecnico. Tambien es de gobierno de integracion: se necesita una forma institucional, controlada y reutilizable de consumir servicios externos que son transversales a varios sistemas.

## 3. Objetivo General

Implementar el **Servicio Sistema Web Proxy GDEBA-DVBA** como capa institucional de integracion entre las aplicaciones internas de la Direccion Provincial de Vialidad y los servicios web autorizados de GDEBA.

El servicio debera centralizar la comunicacion con GDEBA, proteger las credenciales, reducir consumos innecesarios, registrar auditoria de solicitudes, normalizar respuestas y proveer una API interna estable para los sistemas institucionales.

## 4. Objetivos Especificos

Los objetivos especificos del proyecto son:

- Centralizar el consumo de servicios GDEBA autorizados.
- Evitar que las aplicaciones internas consuman directamente los servicios SOAP externos.
- Proteger credenciales, tokens y datos sensibles.
- Identificar la aplicacion interna que realiza cada solicitud.
- Registrar auditoria tecnica y funcional minima de los consumos.
- Implementar cache local sobre SQL Server para reducir llamadas repetidas.
- Mantener informacion local con politicas de frescura configurables.
- Permitir continuidad operativa parcial ante indisponibilidad temporal de GDEBA.
- Normalizar errores y respuestas parciales para las aplicaciones consumidoras.
- Desacoplar los sistemas internos de cambios futuros en GDEBA.
- Preparar la arquitectura para incorporar nuevos servicios autorizados.
- Permitir una evolucion futura desde SOAP hacia REST si GDEBA modifica sus mecanismos de integracion.

## 5. Alcance del Proyecto

El proxy sera una capa de soporte institucional. Su responsabilidad sera resolver la comunicacion con GDEBA y administrar politicas comunes de integracion.

El proxy debera hacerse cargo de:

- Autenticacion tecnica contra GDEBA mediante JWT.
- Consumo de servicios SOAP autorizados.
- Gestion de ambientes HML y PROD.
- Cache local de informacion consultada.
- Registro de auditoria de solicitudes.
- Control de aplicacion consumidora.
- Manejo de errores externos.
- Normalizacion de respuestas.
- Politicas de refresco y sincronizacion.
- Encapsulamiento de particularidades tecnicas de GDEBA.

El proxy no debera hacerse cargo de:

- Reglas de negocio propias de Obras.
- Reglas de negocio propias de Licitaciones.
- Reglas de negocio propias de Certificaciones.
- Decidir si corresponde iniciar un expediente para un proceso interno.
- Determinar el avance funcional de un tramite administrativo propio de otro sistema.
- Reemplazar los backends de las aplicaciones consumidoras.

Por ejemplo, si en el futuro se autoriza un servicio para iniciar expedientes, el sistema de Certificacion de Obras debera decidir si corresponde iniciar un expediente para un certificado determinado. El proxy solo debera proveer la operacion tecnica autorizada para comunicarse con GDEBA, registrar la solicitud y devolver el resultado normalizado.

Esta separacion es importante para evitar que el proxy se transforme en un backend acumulador de logica de distintos dominios. Su valor esta en ser una infraestructura transversal, no en reemplazar a los sistemas de negocio.

## 6. Sistemas Consumidores Previstos

Inicialmente se identifican como consumidores potenciales:

- Sistema de Proyectos de Obras.
- Sistema de Licitaciones de Obras.
- Sistema de Seguimiento y Ejecucion de Obras.
- Sistemas asociados a certificaciones, documentacion administrativa u otros procesos internos que requieran informacion de GDEBA.

Cada sistema mantendra su propia seguridad funcional y sus reglas de negocio. El proxy debera poder identificar que aplicacion realiza cada solicitud, principalmente con fines de auditoria, trazabilidad, control de consumo y diagnostico.

En principio, GDEBA visualizara al **Servicio Sistema Web Proxy GDEBA-DVBA** como aplicacion consumidora. La distincion entre aplicaciones internas se resolvera dentro de la auditoria propia del proxy.

## 7. Servicios GDEBA Autorizados

Los contratos aprobados disponibles corresponden principalmente a servicios SOAP vinculados con expedientes, documentos, tratas, tipos documentales y validaciones.

Los metodos identificados son:

| Grupo | Metodos autorizados |
|---|---|
| Expedientes | `buscarExpediente`, `consultarExpedienteDetallado`, `validarExpediente`, `buscarHistorialPasesExpediente`, `buscarDatosExpedientePorCodigosTrata`, `buscarCodigoCaratulaPorNumeroExpediente` |
| Documentos | `buscarPorNumero`, `buscarDetallePorNumero`, `buscarPDFPorNumero`, `buscarDocumentoEnExpedientes` |
| Tratas | `buscarTratasPorCodigo` |
| Tipos documentales | `consultarTipoDocumento` |
| Estados de pase | `esEstadoPaseExpedienteValido` |

Estos servicios representan la base inicial de capacidades del proxy. La API interna no necesariamente debe copiar los nombres SOAP. Es preferible que exponga operaciones comprensibles para las aplicaciones consumidoras, manteniendo oculto el detalle del metodo GDEBA utilizado.

## 8. Modelo de Integracion con GDEBA

La integracion con GDEBA tiene dos componentes principales:

1. Autorizacion tecnica mediante servicio JWT.
2. Consumo de servicios SOAP autorizados.

La autorizacion se realiza mediante el endpoint:

`https://iop.gba.gob.ar/servicios/JWT/1/REST/jwt`

Este servicio utiliza cabecera `Authorization` con Basic Auth, compuesta por usuario y password provistos para la integracion.

Las credenciales no deben quedar en codigo fuente ni en archivos versionados. Deben administrarse mediante configuracion segura, como variables de entorno, secretos de desarrollo, configuracion protegida o un mecanismo institucional de gestion de secretos.

Para los servicios SOAP, se identifico una condicion tecnica importante: cuando las peticiones XML incluyen caracteres especiales o acentuacion, debe declararse explicitamente la codificacion UTF-8 en el header HTTP:

```http
Content-Type: application/xml; charset=UTF-8
```

La ausencia de esta declaracion puede no producir un error explicito, pero si respuestas vacias cuando se envian parametros con acentos o caracteres especiales. Por este motivo, el proxy debe imponer esta configuracion en todos los consumos XML correspondientes, evitando que cada aplicacion deba conocer esta particularidad.

## 9. Arquitectura Propuesta

Se propone implementar el proxy utilizando arquitectura limpia y orientacion al dominio.

Aunque el sistema se presenta como una capa de integracion, no se trata solamente de un conjunto de llamadas HTTP. El proxy posee reglas propias: cache, auditoria, autorizacion de aplicaciones consumidoras, politicas de refresco, manejo de ambientes, proteccion de credenciales, normalizacion de errores y control de consumo.

Por ese motivo, se recomienda organizar la solucion en capas:

| Capa | Responsabilidad |
|---|---|
| Domain | Conceptos y reglas propias del proxy. |
| Application | Casos de uso y orquestacion. |
| Infrastructure | SQL Server, URF, EF Core, clientes SOAP, JWT, logging tecnico. |
| API | Endpoints internos consumidos por aplicaciones institucionales. |

La capa de dominio no debe depender de Entity Framework, SOAP, HTTP ni SQL Server. La capa de aplicacion debe depender de interfaces. La infraestructura implementara esas interfaces.

Este enfoque permite que, si GDEBA cambia un servicio SOAP, incorpora REST o modifica el mecanismo de autenticacion, el impacto quede contenido en la infraestructura y no se propague a los sistemas consumidores ni a los casos de uso internos.

## 10. Repositorios, Unit of Work y Gateways

La persistencia local se implementara sobre SQL Server. Para mantener coherencia con las practicas habituales del equipo, se utilizara el patron Repository / Unit of Work mediante URF.

Ese enfoque es adecuado para acceder a la base local, donde se almacenaran cache, auditoria, sincronizaciones y metadatos.

Sin embargo, para consumir GDEBA se recomienda utilizar una abstraccion diferente: **Gateways** o **Adapters**. GDEBA no es la base de datos del sistema, sino un proveedor externo. Por eso, no conviene modelarlo como repositorio.

La distincion propuesta es:

| Concepto | Uso |
|---|---|
| Repository / Unit of Work | Acceso a SQL Server y cache local. |
| Gateway / Adapter | Acceso a servicios externos GDEBA. |
| Application Service / Handler | Orquesta cache, gateway, auditoria y reglas del proxy. |

De este modo, una implementacion inicial SOAP podria reemplazarse en el futuro por una implementacion REST sin modificar la logica de aplicacion ni la API interna.

## 11. Politica de Cache y Sincronizacion

Uno de los objetivos principales del proxy es reducir llamadas innecesarias a GDEBA y mejorar la disponibilidad de informacion para los sistemas internos.

La cache local permitira:

- Responder consultas frecuentes sin llamar siempre a GDEBA.
- Disminuir tiempos de respuesta.
- Evitar consultas repetidas de distintos sistemas sobre el mismo recurso.
- Mantener cierta continuidad operativa ante indisponibilidad externa.
- Registrar la fecha de ultima actualizacion de cada dato.

No todos los datos deben tener la misma politica de cache. Algunos catalogos cambian poco, mientras que los expedientes en tramitacion pueden modificarse con mayor frecuencia.

Como criterio inicial:

| Tipo de dato | Criterio sugerido |
|---|---|
| Tratas y tipos documentales | Cache de mayor duracion. |
| Documentos oficiales firmados | Altamente cacheables. |
| PDF de documentos | Cache recomendado por contrato, ya que documentos firmados no pueden eliminarse. |
| Detalle de expediente | Cache de duracion media o corta. |
| Historial de pases | Cache de duracion media o corta. |
| Consultas masivas por trata | Refresco programado y controlado. |

Las respuestas internas deberian indicar si el dato proviene de GDEBA o de cache, y cuando fue actualizado por ultima vez. Esto permite que las aplicaciones consumidoras conozcan la frescura de la informacion.

## 12. Limitaciones Actuales para Refresco Incremental

Actualmente, segun los contratos disponibles, no se cuenta con filtros suficientes para realizar una sincronizacion incremental completa.

El metodo `buscarDatosExpedientePorCodigosTrata` permite consultar por codigo de trata, estado y usuario, pero no por reparticion ni por fecha. Esto obliga a recuperar conjuntos amplios de expedientes y luego filtrar localmente aquellos que correspondan a la reparticion de interes, como `DVMIYSPGP`.

Ademas, el contrato indica que los campos `fechaModificacion`, `motivo` y `usuarioAnterior` pueden no venir informados. Durante las pruebas se verifico que existen expedientes con pases en GDEBA que no devuelven esos campos en ese metodo. Por lo tanto, la ausencia de dichos datos no debe interpretarse como inexistencia de movimientos.

El metodo `buscarHistorialPasesExpediente` devuelve el historial completo de un expediente, pero no permite consultar solamente los movimientos posteriores a una fecha determinada. Esto dificulta la actualizacion incremental y puede aumentar el volumen de procesamiento local.

Por estos motivos, se solicito evaluar mejoras a GDEBA, principalmente:

- Filtro por reparticion/dependencia en `buscarDatosExpedientePorCodigosTrata`.
- Metodo especifico para `DVMIYSPGP`, si fuera mas viable.
- Filtro por fecha en `buscarHistorialPasesExpediente`.
- Aclaracion sobre el significado y condiciones de los campos `fechaModificacion`, `motivo` y `usuarioAnterior`.

## 13. Estrategia Inicial y Estrategia Futura

La arquitectura debera contemplar dos escenarios.

### Escenario inicial

El escenario inicial debe ser compatible con los servicios actualmente autorizados:

- Consultas generales por trata, estado y usuario.
- Filtrado local por reparticion.
- Persistencia de expedientes unicos en SQL Server.
- Consulta puntual de detalle, historial o documentos para expedientes relevantes.
- Cache con politicas diferenciadas.
- Auditoria de cada solicitud.
- Control de frecuencia para evitar sobrecarga.

### Escenario futuro

Si GDEBA habilita filtros adicionales, el proxy debera poder evolucionar hacia:

- Sincronizacion por reparticion.
- Sincronizacion desde fecha de corte.
- Actualizaciones incrementales.
- Menor cantidad de llamadas externas.
- Mayor frescura de datos con menor costo operativo.

La recomendacion es modelar estas alternativas como estrategias de sincronizacion reemplazables. No es necesario implementar desde el inicio todos los escenarios futuros, pero si conviene que la arquitectura no los impida.

## 14. Auditoria, Trazabilidad y Seguridad

El proxy debe registrar la actividad de integracion de manera uniforme.

Cada solicitud deberia registrar, como minimo:

- Fecha y hora.
- Aplicacion interna solicitante.
- Operacion interna invocada.
- Metodo GDEBA utilizado, si corresponde.
- Ambiente utilizado.
- Numero de expediente o documento, cuando aplique.
- Resultado de la operacion.
- Si la respuesta provino de cache o de GDEBA.
- Tiempo de respuesta.
- Identificador de correlacion.
- Error tecnico normalizado, si existiera.

No deben registrarse:

- Passwords.
- Tokens completos.
- Headers de autorizacion completos.
- XML sensible sin enmascaramiento.
- Contenido documental sensible innecesario.

En cuanto a autenticacion interna, dado que las aplicaciones institucionales ya poseen su propia seguridad, no resulta necesario resolver en esta etapa un esquema completo de roles funcionales dentro del proxy. Sin embargo, si es necesario identificar a la aplicacion consumidora.

Como solucion inicial podria utilizarse un mecanismo simple, por ejemplo:

- Identificador de aplicacion.
- API key interna.
- Header firmado.
- Certificado interno.

En una etapa posterior, este mecanismo podria integrarse con un servicio troncal de seguridad y roles institucionales, si dicho servicio se consolida.

## 15. API Interna del Proxy

La API interna deberia expresar operaciones de negocio tecnico comprensibles para las aplicaciones consumidoras, sin exponer detalles SOAP.

Ejemplos posibles:

- Consultar expediente por numero.
- Consultar detalle de expediente.
- Consultar historial de pases.
- Validar expediente.
- Consultar documento por numero.
- Obtener PDF de documento.
- Consultar tratas.
- Consultar tipos documentales.

La API interna deberia mantenerse estable incluso si GDEBA cambia internamente el mecanismo de integracion. Esta es una de las razones principales para construir el proxy como servicio institucional y no como simple libreria compartida.

## 16. Worker y Procesos en Segundo Plano

El proyecto puede requerir dos tipos de ejecucion:

1. Una Web API para responder solicitudes bajo demanda.
2. Un Worker o servicio en segundo plano para procesos programados.

La Web API es necesaria para que las aplicaciones internas consuman el proxy.

El Worker seria util para:

- Refrescar cache de manera programada.
- Ejecutar sincronizaciones por trata.
- Actualizar expedientes relevantes.
- Reintentar operaciones pendientes.
- Limpiar registros tecnicos antiguos.
- Calcular hashes o detectar cambios.

No es obligatorio que el Worker ejecute una logica compleja desde la primera version. Puede incorporarse como componente preparado o implementarse en una etapa posterior. Lo importante es que la arquitectura contemple la posibilidad de tareas programadas, porque la cache y la sincronizacion no dependen solamente de consultas en tiempo real.

## 17. Ambientes

El proxy debe soportar al menos:

- HML: ambiente de homologacion.
- PROD: ambiente productivo.

Tambien debe contemplarse un modo de ejecucion local para desarrollo. Esto no implica tener una instancia local de GDEBA, sino poder ejecutar el proxy en una maquina de desarrollo con configuracion controlada.

El modo local podria trabajar:

- Apuntando a HML.
- Con mocks de servicios externos.
- Con respuestas grabadas para pruebas automatizadas.

La definicion exacta del modo local queda pendiente para una etapa posterior.

## 18. Riesgos y Pendientes

Se identifican los siguientes puntos pendientes:

- Confirmar endpoint HML del servicio JWT.
- Confirmar vigencia del token JWT.
- Confirmar header exacto de autorizacion requerido en consumos SOAP.
- Confirmar limites, cuotas o throttling de GDEBA.
- Confirmar si GDEBA puede incorporar filtros por reparticion.
- Confirmar si GDEBA puede incorporar filtros por fecha.
- Confirmar significado exacto de `fechaModificacion`, `motivo` y `usuarioAnterior`.
- Definir mecanismo inicial de identificacion de aplicaciones internas.
- Definir politicas definitivas de TTL por tipo de dato.
- Definir si se implementara Worker desde la primera version.
- Definir estrategia de pruebas locales con HML, mocks o respuestas grabadas.

## 19. Conclusion

El **Servicio Sistema Web Proxy GDEBA-DVBA** se propone como una pieza de infraestructura institucional para ordenar, asegurar y optimizar la integracion entre los sistemas internos de la Direccion Provincial de Vialidad y la plataforma GDEBA.

Su implementacion permitira centralizar credenciales, tokenizacion, consumo SOAP, cache, auditoria, trazabilidad y normalizacion de errores. Al mismo tiempo, reducira la duplicacion de codigo en aplicaciones internas y permitira que futuras modificaciones de GDEBA impacten en un unico punto controlado.

La solucion no debe entenderse como reemplazo de los sistemas de negocio existentes, sino como una capa transversal de soporte. Cada sistema consumidor conservara sus reglas funcionales, mientras que el proxy resolvera de manera comun y gobernada la comunicacion con GDEBA.

La arquitectura propuesta permite comenzar con los servicios actualmente disponibles y, al mismo tiempo, preparar el sistema para mejoras futuras como filtros por reparticion, sincronizacion incremental por fecha, nuevos metodos autorizados o migracion de SOAP a REST.

