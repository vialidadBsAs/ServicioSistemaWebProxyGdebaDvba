# Proyecto Servicio Sistema Web Proxy GDEBA-DVBA

## Documento de Fundamentacion, Alcance y Arquitectura Propuesta

Version: 0.2  
Fecha: 28/05/2026  
Estado: Borrador de trabajo

## Guia visual del documento

Para facilitar la lectura, el documento utiliza recuadros de referencia que identifican la naturaleza de algunos temas.

| Identificador | Uso |
|---|---|
| **▣ Decision estrategica** | Define una orientacion central del proyecto o una decision de alto impacto. |
| **◇ Alcance** | Delimita responsabilidades del proxy y de los sistemas consumidores. |
| **▣ Seguridad** | Indica temas relacionados con credenciales, tokens, autorizacion, logs sensibles o trazabilidad. |
| **▣ Tecnico** | Describe condiciones tecnicas necesarias para la integracion. |
| **▣ Operacion** | Explica criterios de cache, sincronizacion, disponibilidad o mantenimiento. |
| **⚠ Pendiente** | Marca definiciones que dependen de confirmacion futura o maduracion del proyecto. |

## 1. Introduccion

### Contexto institucional

La Direccion Provincial de Vialidad de la Provincia de Buenos Aires requiere integrar distintos sistemas internos con la plataforma GDEBA, a fin de consultar y eventualmente operar sobre informacion administrativa vinculada con expedientes electronicos, documentos oficiales, tratas, tipos documentales y movimientos de tramitacion.

Esta integracion resulta necesaria para acompañar procesos propios de la gestion vial, especialmente aquellos relacionados con proyectos, licitaciones, seguimiento, ejecucion y certificacion de obras. En dichos procesos, la informacion alojada en GDEBA puede constituir una fuente relevante para consultar expedientes, identificar documentacion vinculada, verificar estados administrativos o recuperar documentos oficiales.

### Necesidad de una solucion transversal

Si cada sistema interno implementa por separado la comunicacion con GDEBA, se genera una multiplicacion de componentes tecnicos similares: autenticacion, consumo SOAP, manejo de XML, tratamiento de errores, control de codificacion, cache y auditoria. Esta repeticion aumenta el costo de mantenimiento y dificulta aplicar criterios uniformes de seguridad y trazabilidad.

Por este motivo se propone implementar el **Servicio Sistema Web Proxy GDEBA-DVBA** como una capa institucional de interoperabilidad. Este servicio funcionara como punto de acceso comun entre los sistemas internos de la DVBA y los servicios web autorizados de GDEBA, permitiendo centralizar la complejidad tecnica y ofrecer a las aplicaciones consumidoras una API interna mas estable y controlada.

> **▣ Decision estrategica**  
> El proxy se plantea como una capacidad institucional compartida, no como una solucion puntual de una aplicacion. Esta decision busca ordenar el consumo de GDEBA desde una perspectiva transversal.

## 2. Situacion Actual y Problema a Resolver

### Consumo distribuido de servicios externos

En el escenario actual, algunos consumos de GDEBA pueden encontrarse implementados directamente dentro de aplicaciones especificas, o bien ser requeridos por nuevos sistemas que necesitan resolver la integracion de manera individual. Esta forma de trabajo puede ser suficiente para una necesidad puntual, pero no escala adecuadamente cuando varios sistemas institucionales requieren acceder a los mismos servicios.

La integracion directa implica que cada aplicacion deba conocer endpoints externos, contratos SOAP, mecanismos de autenticacion, manejo de tokens, encabezados HTTP y formatos de respuesta. Ademas, cada equipo deberia resolver de manera propia la gestion de errores, la auditoria, el cacheo y la proteccion de credenciales.

### Riesgos de duplicacion y dispersion

La duplicacion de logica tecnica genera riesgos concretos. Por un lado, las credenciales o mecanismos de autorizacion pueden quedar distribuidos en mas de una aplicacion. Por otro, pueden aparecer diferencias de implementacion entre sistemas que consumen el mismo servicio externo, provocando comportamientos inconsistentes ante errores, respuestas vacias o cambios de contrato.

Tambien se incrementa la cantidad de llamadas directas a GDEBA. Si varios sistemas consultan repetidamente los mismos expedientes o documentos, el consumo externo crece sin que necesariamente exista un beneficio funcional equivalente. Esto puede afectar el rendimiento percibido, aumentar el procesamiento local y generar preocupaciones respecto de limites o cuotas de acceso.

### Dificultad de auditoria institucional

Cuando el consumo se realiza desde multiples aplicaciones, resulta mas complejo reconstruir que sistema consulto que informacion, con que frecuencia, bajo que resultado y en que momento. La auditoria queda dispersa entre aplicaciones, con formatos y criterios posiblemente diferentes.

El proxy busca resolver este problema registrando en un punto comun las solicitudes internas, la aplicacion consumidora, la operacion requerida, la respuesta obtenida, la fuente de datos utilizada y los errores producidos. Esta trazabilidad es importante tanto para diagnostico tecnico como para control institucional del uso de los servicios.

> **▣ Seguridad**  
> La trazabilidad no es solo un aspecto tecnico. Permite conocer que aplicacion institucional origino cada consulta, aun cuando GDEBA vea al proxy como unico consumidor autorizado.

## 3. Objetivo General

### Definicion del objetivo

El objetivo general del proyecto es implementar el **Servicio Sistema Web Proxy GDEBA-DVBA** como capa institucional de integracion entre los sistemas internos de la Direccion Provincial de Vialidad y los servicios web autorizados de GDEBA.

El servicio debera actuar como intermediario tecnico y operativo, concentrando la comunicacion con GDEBA y exponiendo hacia los sistemas internos una API propia, orientada a las necesidades de consulta y soporte de la institucion.

### Resultado esperado

Como resultado, las aplicaciones internas no deberian necesitar conocer los detalles de los contratos SOAP ni administrar credenciales GDEBA. Deberian consumir operaciones internas provistas por el proxy, mientras este se encarga de autenticar, consultar, cachear, auditar y normalizar la informacion recibida.

Esta separacion permitira mejorar la seguridad, reducir duplicaciones, facilitar el mantenimiento y preparar a la institucion para incorporar nuevos servicios autorizados o adaptarse a cambios futuros en la plataforma GDEBA.

> **▣ Decision estrategica**  
> El resultado esperado no es solamente construir un cliente de servicios GDEBA, sino establecer un punto institucional de gobierno tecnico para las integraciones.

## 4. Objetivos Especificos

### Centralizacion de la integracion

El proxy centralizara el consumo de los servicios GDEBA autorizados, evitando que cada aplicacion interna implemente su propio cliente SOAP o gestione directamente la autorizacion tecnica. Esta centralizacion permite mantener un unico punto de actualizacion ante cambios de endpoints, contratos, codificacion, autenticacion o politicas de consumo.

Desde el punto de vista operativo, esto reduce la complejidad de los sistemas consumidores. Las aplicaciones internas podran concentrarse en su dominio funcional, delegando en el proxy la responsabilidad de interoperar con GDEBA.

### Seguridad y proteccion de credenciales

Uno de los objetivos centrales es evitar que las credenciales de GDEBA se encuentren distribuidas en multiples aplicaciones. El proxy debera almacenar y utilizar dichas credenciales mediante mecanismos de configuracion segura, sin incorporarlas al codigo fuente ni exponerlas en logs.

Asimismo, el manejo de tokens JWT quedara encapsulado dentro del servicio. Las aplicaciones consumidoras no deberian conocer ni manipular directamente el token utilizado para la comunicacion externa.

> **▣ Seguridad**  
> Las credenciales GDEBA y los tokens JWT son activos sensibles. Su centralizacion reduce superficie de exposicion y evita que cada sistema consumidor replique mecanismos de proteccion.

### Auditoria y trazabilidad

El servicio debera registrar las solicitudes realizadas por las aplicaciones internas, incluyendo informacion suficiente para reconstruir el uso de la integracion. La auditoria debera permitir identificar que aplicacion realizo una consulta, que operacion solicito, sobre que expediente o documento, en que momento y con que resultado.

Esta trazabilidad no busca reemplazar la auditoria funcional de cada sistema consumidor, sino complementar el control tecnico e institucional de la comunicacion con GDEBA.

### Cache y reduccion de llamadas externas

El proxy debera implementar una cache local sobre SQL Server para disminuir llamadas repetidas hacia GDEBA. Esta cache permitira mejorar tiempos de respuesta, reducir trafico externo y sostener cierto nivel de disponibilidad ante interrupciones temporales de conectividad.

La cache no debe entenderse como una simple copia indiscriminada de datos, sino como una politica administrada de frescura. Cada tipo de informacion debera tener criterios de actualizacion adecuados a su naturaleza y frecuencia de cambio.

> **▣ Operacion**  
> La cache es una decision operativa de disponibilidad y eficiencia. Debe administrarse con criterios de frescura, no como una copia local sin control.

### Desacoplamiento frente a cambios futuros

El proxy tambien debe proteger a los sistemas internos frente a cambios futuros en GDEBA. Si un servicio SOAP cambia, si se habilita un nuevo metodo o si en el futuro se migra hacia una API REST, el impacto deberia concentrarse en la capa de integracion del proxy.

El objetivo es que las aplicaciones internas mantengan contratos estables, incluso cuando cambien detalles tecnicos del proveedor externo.

## 5. Alcance del Proyecto

### Responsabilidades incluidas

El proxy sera responsable de resolver la comunicacion tecnica con GDEBA. Esto incluye obtener y administrar el token JWT, consumir servicios SOAP autorizados, configurar correctamente las peticiones XML, manejar ambientes HML y PROD, registrar auditoria, aplicar politicas de cache y normalizar errores.

Tambien debera identificar la aplicacion interna que realiza cada solicitud. Esta identificacion sera utilizada para control, auditoria, diagnostico y eventualmente aplicacion de politicas internas de autorizacion o consumo.

### Responsabilidades excluidas

El proxy no debera contener reglas de negocio propias de los sistemas consumidores. No corresponde que decida si una obra esta en condiciones de certificarse, si una licitacion debe avanzar de etapa, si corresponde crear un expediente por un tramite interno o que documentacion funcional exige un proceso administrativo determinado.

Estas decisiones pertenecen a los sistemas de dominio correspondientes. El proxy solo debe proveer la capacidad tecnica de comunicarse con GDEBA bajo reglas comunes de seguridad, trazabilidad y eficiencia.

> **◇ Alcance**  
> El proxy no es el backend funcional de Obras, Licitaciones o Certificaciones. Su dominio es la interoperabilidad institucional con GDEBA.

### Ejemplo de separacion de responsabilidades

Si en una etapa futura se autoriza un metodo para iniciar expedientes, el sistema de Certificacion de Obras debera decidir si corresponde iniciar un expediente para un certificado determinado. Esa decision depende de reglas funcionales del proceso de certificacion.

El proxy, en cambio, debera encargarse de ejecutar la operacion autorizada contra GDEBA, validar la peticion desde el punto de vista tecnico, registrar la auditoria, manejar errores y devolver una respuesta normalizada al sistema solicitante.

## 6. Sistemas Consumidores Previstos

### Sistemas iniciales

Los consumidores iniciales previstos son el Sistema de Proyectos de Obras, el Sistema de Licitaciones de Obras y el Sistema de Seguimiento y Ejecucion de Obras. Estos sistemas forman parte del ciclo de vida de las obras dentro de la reparticion y requieren consultar informacion administrativa disponible en GDEBA.

Tambien podran incorporarse otros sistemas internos que necesiten acceder a expedientes, documentos oficiales, tratas, tipos documentales o informacion de movimientos, siempre que su consumo se encuentre autorizado y alineado con el alcance institucional del proxy.

### Seguridad propia de los sistemas consumidores

Cada sistema consumidor mantiene su propio esquema de seguridad funcional. Esto significa que la validacion de usuarios, roles internos y permisos de negocio continuara perteneciendo a cada aplicacion.

El proxy no necesita, en una primera etapa, reemplazar esa seguridad funcional. Sin embargo, si debe identificar que aplicacion interna realiza cada solicitud. Esta identificacion permitira auditar consumos y aplicar controles minimos de uso institucional.

> **◇ Alcance**  
> La autenticacion funcional de usuarios permanece en cada sistema consumidor. El proxy solo necesita identificar la aplicacion que lo consume, salvo que en el futuro se integre con un servicio troncal de seguridad.

### Visibilidad frente a GDEBA

Desde la perspectiva de GDEBA, la aplicacion consumidora sera el Servicio Sistema Web Proxy GDEBA-DVBA. La distincion entre aplicaciones internas se administrara dentro del propio proxy.

Esto permite transparentar hacia GDEBA un unico punto de integracion institucional, mientras que internamente se conserva la trazabilidad sobre el sistema que origino cada solicitud.

## 7. Servicios GDEBA Autorizados

### Agrupacion funcional

Los contratos aprobados corresponden principalmente a servicios SOAP de consulta vinculados con expedientes, documentos, tratas, tipos documentales y estados de pase. Estos servicios constituyen la base inicial de capacidades que el proxy debera encapsular.

En lugar de exponer directamente los nombres tecnicos de los metodos SOAP, la API interna deberia presentar operaciones comprensibles para las aplicaciones consumidoras. Por ejemplo, una aplicacion deberia poder solicitar el detalle de un expediente sin conocer si internamente se invoca `buscarExpediente` o `consultarExpedienteDetallado`.

### Servicios de expedientes

Los servicios de expedientes permiten consultar informacion general, validar la existencia de expedientes, obtener detalle ampliado, consultar historiales de pases y recuperar datos asociados a codigos de trata.

Entre los metodos identificados se encuentran `buscarExpediente`, `consultarExpedienteDetallado`, `validarExpediente`, `buscarHistorialPasesExpediente`, `buscarDatosExpedientePorCodigosTrata` y `buscarCodigoCaratulaPorNumeroExpediente`.

### Servicios de documentos

Los servicios de documentos permiten consultar informacion de documentos GEDO, obtener detalle de actividades, identificar expedientes donde se encuentra vinculado un documento y recuperar el contenido PDF codificado en Base64.

Los metodos identificados son `buscarPorNumero`, `buscarDetallePorNumero`, `buscarPDFPorNumero` y `buscarDocumentoEnExpedientes`. En particular, para la obtencion de PDFs, el contrato sugiere implementar cache interna, dado que los documentos firmados no pueden ser eliminados.

### Servicios de catalogos y validaciones

Los servicios de tratas y tipos documentales permiten consultar informacion de referencia necesaria para interpretar expedientes y documentos. En este grupo se encuentran `buscarTratasPorCodigo` y `consultarTipoDocumento`.

Tambien se encuentra autorizado el metodo `esEstadoPaseExpedienteValido`, que permite validar si un estado de pase es aplicable a un expediente determinado. Este metodo requiere especial cuidado en la denominacion del estado, incluyendo tildes y escritura exacta.

> **▣ Tecnico**  
> Algunos servicios son sensibles a la escritura exacta de parametros, incluyendo acentos y denominaciones propias de GDEBA. El proxy debe encapsular validaciones y convenciones para reducir errores de consumo.

## 8. Modelo de Integracion con GDEBA

### Autorizacion mediante JWT

La integracion con GDEBA requiere obtener autorizacion tecnica mediante un servicio JWT. El endpoint productivo informado es `https://iop.gba.gob.ar/servicios/JWT/1/REST/jwt`, utilizando cabecera `Authorization` con Basic Auth basada en usuario y password.

Este mecanismo debe quedar completamente encapsulado dentro del proxy. Las credenciales deben administrarse mediante configuracion segura y nunca deben ser registradas en logs, incluidas en codigo fuente o expuestas a aplicaciones consumidoras.

> **▣ Seguridad**  
> El servicio JWT es un componente critico de seguridad. La gestion del token debe ser interna al proxy y debe evitarse cualquier exposicion hacia aplicaciones consumidoras.

### Consumo SOAP

Los servicios aprobados son SOAP 1.0. El proxy debera construir las peticiones XML, invocar los endpoints correspondientes, procesar respuestas y convertir los resultados en modelos internos o DTOs propios.

La capa de aplicacion no deberia conocer detalles de SOAP. La construccion de envelopes, namespaces, headers, faults y serializacion XML debe permanecer en la infraestructura, dentro de gateways o adapters especificos de GDEBA.

### Codificacion UTF-8

Durante el analisis se identifico una condicion tecnica relevante: cuando la peticion incluye parametros con caracteres especiales o acentuacion, debe declararse explicitamente la codificacion UTF-8 en el header `Content-Type`.

La configuracion recomendada es:

```http
Content-Type: application/xml; charset=UTF-8
```

La ausencia de esta declaracion puede no producir un error explicito, pero si respuestas vacias. Este comportamiento refuerza la necesidad de centralizar la integracion en el proxy, evitando que cada aplicacion deba descubrir y resolver individualmente estas particularidades.

> **▣ Tecnico**  
> La codificacion UTF-8 es un requisito tecnico de interoperabilidad. No tiene el mismo nivel estrategico que el alcance del proyecto, pero es una condicion necesaria para evitar errores silenciosos en peticiones con acentos o caracteres especiales.

## 9. Arquitectura Propuesta

### Enfoque general

Se propone implementar el proxy con arquitectura limpia y orientacion al dominio. Aunque el sistema consume servicios externos, no debe considerarse un simple cliente HTTP. El proxy tendra reglas propias de seguridad, cache, auditoria, control de consumo, manejo de ambientes y normalizacion de errores.

La arquitectura limpia permite separar estas reglas de los detalles tecnicos. De esta forma, el dominio y los casos de uso del proxy no quedan acoplados a SOAP, Entity Framework, SQL Server o mecanismos especificos de autenticacion.

> **▣ Decision estrategica**  
> La arquitectura limpia se elige para proteger las reglas propias del proxy frente a cambios de infraestructura, contratos externos o mecanismos de autenticacion.

### Capa de dominio

La capa de dominio contendra conceptos propios del proxy, como expediente GDEBA, documento GDEBA, trata, tipo documental, aplicacion consumidora, politica de cache, solicitud de integracion, registro de auditoria y ambiente GDEBA.

Estos conceptos representan el lenguaje interno del proxy. No deben depender directamente de clases generadas desde WSDL ni de entidades de persistencia, ya que esas estructuras pertenecen a la infraestructura.

### Capa de aplicacion

La capa de aplicacion orquestara los casos de uso. Por ejemplo, consultar un expediente puede requerir validar la aplicacion consumidora, revisar cache, decidir si corresponde consultar GDEBA, invocar un gateway, guardar el resultado y registrar auditoria.

Esta capa debe depender de interfaces. No deberia construir XML, abrir conexiones SQL directamente ni conocer detalles de autenticacion externa. Su funcion es coordinar reglas y flujos del proxy.

### Capa de infraestructura

La infraestructura implementara los detalles tecnicos: clientes SOAP, cliente JWT, repositorios URF, Unit of Work, SQL Server, serializacion XML, configuracion segura y logging tecnico.

Al concentrar estos detalles en una capa especifica, se facilita reemplazar componentes en el futuro. Por ejemplo, si GDEBA habilita REST, se podra implementar un nuevo adapter sin modificar la logica principal del proxy.

### Capa API

La API expondra endpoints internos para las aplicaciones institucionales. Estos endpoints deberian representar operaciones propias del proxy y no simples espejos de los metodos SOAP.

El objetivo es ofrecer contratos internos estables y comprensibles. Las aplicaciones consumidoras deberian depender de la API institucional, no de los contratos tecnicos externos de GDEBA.

## 10. Repositorios, Unit of Work y Gateways

### Uso de URF para persistencia local

La base de datos local se implementara sobre SQL Server. Para acceder a ella se utilizara el patron Repository / Unit of Work mediante URF, siguiendo las practicas habituales del equipo.

Este enfoque permite desacoplar la capa de dominio y aplicacion respecto de Entity Framework y mantener una forma consistente de trabajar con persistencia local, transacciones y repositorios.

### Diferencia entre repositorio y gateway

Es importante distinguir entre persistencia propia y servicios externos. Un repositorio representa acceso a datos que pertenecen al sistema, como cache, auditoria o sincronizaciones almacenadas en SQL Server. En cambio, GDEBA es un proveedor externo.

Por ese motivo, para GDEBA se recomienda utilizar interfaces tipo Gateway o Adapter. Estas interfaces expresan capacidades externas, como consultar un expediente o recuperar un PDF, sin exponer si la implementacion actual usa SOAP, REST u otro mecanismo.

### Beneficio ante cambios externos

Si en el futuro GDEBA modifica los servicios SOAP o migra a API REST, la implementacion del gateway podra reemplazarse sin cambiar la API interna ni los casos de uso principales.

Esta decision reduce el acoplamiento y evita que un cambio externo obligue a modificar multiples capas o aplicaciones consumidoras.

> **▣ Decision estrategica**  
> Separar repositorios locales de gateways externos evita confundir persistencia propia con integracion. Esta distincion es clave para mantener un diseño extensible.

## 11. Politica de Cache y Sincronizacion

### Finalidad de la cache

La cache local tiene como finalidad reducir llamadas innecesarias a GDEBA, mejorar los tiempos de respuesta y sostener cierta disponibilidad cuando el servicio externo no se encuentre accesible.

No se busca almacenar informacion sin criterio, sino administrar datos locales bajo una politica de frescura. Cada dato cacheado debe poder indicar cuando fue consultado, de donde provino y bajo que condiciones se considera vigente.

> **▣ Operacion**  
> La cache debe permitir responder mejor, pero tambien debe informar la frescura del dato. La disponibilidad no debe confundirse con certeza absoluta de actualidad.

### Diferencia entre tipos de informacion

No todos los datos cambian con la misma frecuencia. Las tratas y tipos documentales suelen comportarse como catalogos relativamente estables, por lo que pueden tener tiempos de cache mas prolongados.

En cambio, un expediente en tramitacion o su historial de pases pueden cambiar con mayor frecuencia. Para estos casos se requiere una politica mas cautelosa, con TTL menor, actualizacion bajo demanda o refrescos programados segun criticidad.

### Cache de documentos oficiales

Los documentos oficiales firmados tienen una naturaleza mas estable. En particular, el contrato de `buscarPDFPorNumero` sugiere implementar cache interna, ya que los documentos firmados no pueden ser eliminados.

Esto convierte a los PDFs oficiales en candidatos naturales para cache persistente, siempre considerando controles de acceso, almacenamiento seguro y auditoria de consulta.

### Frescura visible para consumidores

Las respuestas del proxy deberian indicar si la informacion proviene de cache o de una consulta reciente a GDEBA. Tambien deberian informar la fecha de ultima actualizacion.

Este criterio permite que las aplicaciones consumidoras tomen decisiones informadas. Por ejemplo, una pantalla de consulta puede aceptar datos cacheados, mientras que una operacion critica podria requerir forzar actualizacion si la politica lo permite.

## 12. Limitaciones Actuales para Refresco Incremental

### Consulta masiva por trata

El metodo `buscarDatosExpedientePorCodigosTrata` permite consultar expedientes a partir de codigo de trata, estado y usuario. Sin embargo, no cuenta actualmente con un filtro por reparticion ni por fecha.

Esto obliga a recuperar un conjunto amplio de expedientes y luego filtrar localmente aquellos correspondientes a la reparticion de interes, como `DVMIYSPGP`. Desde el punto de vista operativo, este esquema puede resultar costoso y poco eficiente para mantener una cache actualizada.

> **⚠ Pendiente**  
> La eficiencia de la sincronizacion depende en parte de que GDEBA pueda habilitar filtros por reparticion o fecha. Mientras eso no ocurra, el proxy debera administrar consultas masivas con cuidado operativo.

### Campos de salida no siempre informados

El contrato indica que `fechaModificacion`, `motivo` y `usuarioAnterior` pueden no venir informados en algunos casos. Durante las pruebas se verifico que existen expedientes con pases en GDEBA que, aun asi, no devuelven dichos campos en este metodo.

Por lo tanto, la ausencia de esos datos no debe interpretarse como ausencia de movimientos. Esta observacion es relevante porque impide utilizar esos campos como criterio confiable de sincronizacion incremental.

### Historial completo sin filtro por fecha

El metodo `buscarHistorialPasesExpediente` devuelve el historial completo de un expediente, incluyendo operaciones y documentacion vinculada. No obstante, segun los contratos disponibles, no permite consultar unicamente movimientos posteriores a una fecha determinada.

Esto significa que, para detectar cambios, podria ser necesario recuperar historiales completos repetidamente. El proxy debera administrar esta limitacion mediante cache, control de frecuencia y mecanismos de comparacion local.

## 13. Estrategia Inicial y Estrategia Futura

### Estrategia compatible con servicios actuales

La primera version debe ser compatible con los servicios actualmente autorizados. Esto implica realizar consultas generales controladas, filtrar localmente la informacion de interes, persistir expedientes unicos y consultar detalle o historial solo cuando sea necesario.

Esta estrategia no es ideal desde el punto de vista de eficiencia, pero permite avanzar sin depender de cambios externos. Para evitar sobrecarga, debera complementarse con politicas de cache, programacion de refrescos y limites de frecuencia.

> **▣ Operacion**  
> La primera version debe resolver el escenario real disponible, no el escenario ideal. La arquitectura, sin embargo, debe quedar preparada para sincronizacion incremental futura.

### Estrategia futura con filtros GDEBA

Si GDEBA habilita filtros por reparticion o por fecha, el proxy debera evolucionar hacia una sincronizacion mas incremental. Esto permitiria recuperar solo informacion relevante y reducir significativamente el volumen de datos procesados.

La arquitectura deberia prever esta posibilidad mediante estrategias de sincronizacion reemplazables. De esta manera, el cambio estaria contenido en la forma de obtener o refrescar datos, sin alterar la API interna consumida por los sistemas institucionales.

### Evitar sobreingenieria inicial

No resulta necesario implementar desde el primer momento todas las estrategias futuras. Lo importante es no cerrar la arquitectura de forma tal que luego resulte dificil incorporarlas.

La recomendacion es implementar el modo actual con servicios disponibles y documentar claramente los puntos de extension para un modo incremental futuro.

## 14. Auditoria, Trazabilidad y Seguridad

### Auditoria de solicitudes internas

Cada solicitud al proxy deberia registrar informacion suficiente para reconstruir el consumo. Entre los datos relevantes se encuentran la fecha y hora, aplicacion solicitante, operacion interna, metodo GDEBA utilizado, ambiente, expediente o documento consultado, resultado, tiempo de respuesta y fuente de datos.

Esta auditoria permitira detectar patrones de consumo, diagnosticar errores, identificar consultas repetidas y responder ante necesidades de control institucional.

### Proteccion de informacion sensible

El registro de auditoria no debe incluir informacion sensible innecesaria. En particular, no deben registrarse passwords, tokens completos, headers de autorizacion completos ni XML con datos sensibles sin enmascaramiento.

La politica de logs debe equilibrar capacidad de diagnostico y proteccion de informacion. Un log demasiado pobre dificulta resolver problemas; un log excesivo puede exponer datos que no deberian persistirse.

> **▣ Seguridad**  
> La auditoria debe servir para explicar lo ocurrido sin exponer credenciales, tokens ni datos documentales sensibles. El enmascaramiento debe ser parte de la politica de logging desde el inicio.

### Identificacion de aplicaciones consumidoras

Aunque cada sistema interno mantenga su propia seguridad, el proxy necesita conocer que aplicacion realiza la solicitud. Esta identificacion puede resolverse inicialmente mediante un mecanismo simple, como un identificador de aplicacion y una API key interna, un header firmado o un certificado.

En el futuro, si se consolida un servicio troncal de seguridad y roles, el proxy podria integrarse con dicho componente. No obstante, no es necesario bloquear el inicio del proyecto por esa definicion.

## 15. API Interna del Proxy

### Operaciones internas estables

La API interna debe diseñarse pensando en las necesidades de los sistemas consumidores, no en reproducir literalmente los contratos SOAP. Por ejemplo, una operacion interna puede llamarse "consultar detalle de expediente" aunque por debajo utilice uno o mas metodos GDEBA.

Este criterio permite conservar estabilidad frente a cambios externos y ofrece una interfaz mas comprensible para los equipos que integren sus aplicaciones con el proxy.

### Evitar exponer SOAP crudo

No se recomienda que el proxy sea un pasamanos generico de envelopes SOAP. Si la API interna permitiera ejecutar cualquier metodo externo con XML libre, se perderian beneficios de control, auditoria, validacion, cache y normalizacion.

El proxy debe exponer operaciones autorizadas y conocidas. Esto permite aplicar reglas especificas por tipo de consulta, registrar auditoria mas clara y proteger mejor el consumo externo.

> **▣ Decision estrategica**  
> La API interna debe expresar capacidades institucionales, no detalles de implementacion SOAP. Esto mantiene estable el contrato con las aplicaciones consumidoras.

## 16. Worker y Procesos en Segundo Plano

### Rol de la Web API

La Web API sera el componente encargado de responder solicitudes bajo demanda de las aplicaciones internas. Por ejemplo, una aplicacion podra pedir el detalle de un expediente o el PDF de un documento cuando un usuario lo requiera.

Este componente es indispensable para que el proxy funcione como servicio institucional consumible.

### Rol del Worker

Un Worker o servicio en segundo plano permite ejecutar tareas que no dependen de una solicitud inmediata de usuario. Entre ellas se encuentran refrescar cache, sincronizar expedientes por trata, actualizar datos relevantes, limpiar registros tecnicos antiguos o recalcular metadatos de control.

El Worker es especialmente util cuando se desea mantener informacion local actualizada de manera preventiva. Sin embargo, puede incorporarse gradualmente. La primera version podria comenzar con Web API y tareas minimas, dejando preparado el diseño para activar procesos programados cuando la politica de cache este mas definida.

### Criterio recomendado

La recomendacion es contemplar ambos modos dentro de la solucion: API para consumo bajo demanda y Worker para sincronizacion o mantenimiento. No obstante, la complejidad inicial del Worker debe mantenerse acotada para no anticipar escenarios que todavia dependen de definiciones externas de GDEBA.

> **▣ Operacion**  
> El Worker no es obligatorio para exponer la API, pero sera importante si se desea mantener cache actualizada de forma preventiva o ejecutar sincronizaciones programadas.

## 17. Ambientes

### Homologacion y produccion

El proxy debera soportar los ambientes HML y PROD de GDEBA. Cada ambiente tendra sus propios endpoints, credenciales, configuracion de token, timeouts y politicas de diagnostico.

La separacion de ambientes es fundamental para probar integraciones sin afectar datos productivos y para validar cambios antes de promoverlos.

### Ejecucion local

Cuando se menciona un ambiente local, no se propone tener una instancia local de GDEBA. Se refiere a ejecutar el proxy en una maquina de desarrollo con configuracion controlada.

Ese modo local podria apuntar a HML, utilizar mocks de servicios externos o trabajar con respuestas grabadas para pruebas automatizadas. La decision dependera de las necesidades de desarrollo, disponibilidad de HML y criterios de prueba del equipo.

## 18. Riesgos y Pendientes

### Dependencias con GDEBA

Existen definiciones pendientes que dependen de respuestas o decisiones de GDEBA. Entre ellas se encuentran confirmar endpoint HML del servicio JWT, vigencia del token, headers exactos para consumir SOAP, limites de consumo y posibilidad de incorporar filtros por reparticion o fecha.

Estas definiciones pueden modificar la implementacion, especialmente en lo relacionado con sincronizacion y cache. Por eso deben mantenerse documentadas y revisarse a medida que avance el proyecto.

> **⚠ Pendiente**  
> Las respuestas de GDEBA pueden ajustar decisiones de implementacion, pero no invalidan la necesidad del proxy. La arquitectura debe absorber esos cambios sin afectar a las aplicaciones consumidoras.

### Pendientes funcionales de interpretacion

Tambien queda pendiente aclarar el significado exacto de ciertos campos devueltos por los servicios, especialmente `fechaModificacion`, `motivo` y `usuarioAnterior` en `buscarDatosExpedientePorCodigosTrata`.

La documentacion actual indica que pueden no venir informados, pero no especifica bajo que condiciones ocurre. Esta ambiguedad impide utilizarlos como indicadores confiables de movimientos o como base de sincronizacion incremental.

### Pendientes internos

A nivel interno deben definirse las politicas definitivas de TTL, el mecanismo inicial de identificacion de aplicaciones consumidoras, la profundidad de auditoria, la estrategia de pruebas locales y el momento en que se incorporara un Worker con tareas programadas.

Estas decisiones no impiden avanzar con el diseño, pero deben quedar registradas para evitar supuestos implicitos.

## 19. Conclusion

### Valor institucional del proxy

El **Servicio Sistema Web Proxy GDEBA-DVBA** se propone como una pieza de infraestructura institucional para ordenar y fortalecer la integracion entre los sistemas internos de la Direccion Provincial de Vialidad y la plataforma GDEBA.

Su implementacion permitira centralizar credenciales, tokenizacion, consumo SOAP, cache, auditoria, trazabilidad y normalizacion de errores. Esto reducira duplicaciones, mejorara la seguridad y facilitara el mantenimiento de las integraciones.

### Alcance equilibrado

El proxy no debe entenderse como reemplazo de los sistemas de negocio existentes. Cada sistema consumidor conservara sus reglas funcionales y su seguridad propia. El proxy actuara como capa transversal de soporte, encargada de resolver de manera comun y gobernada la comunicacion con GDEBA.

Esta separacion permite mantener una arquitectura mas clara: los sistemas de dominio deciden que necesitan hacer; el proxy resuelve como comunicarse con GDEBA de manera segura, auditable y eficiente.

### Evolucion futura

La arquitectura propuesta permite comenzar con los servicios actualmente disponibles y, al mismo tiempo, preparar el sistema para mejoras futuras. Si GDEBA incorpora filtros por reparticion, sincronizacion por fecha, nuevos metodos autorizados o una migracion hacia REST, el proxy podra adaptarse sin trasladar esa complejidad a las aplicaciones consumidoras.

De esta manera, el proyecto no solo resuelve una necesidad tecnica inmediata, sino que establece una base institucional para futuras integraciones con GDEBA.
