# Guia Tecnica de Arquitectura e Implementacion

Version: 0.3  
Fecha: 31/05/2026  
Estado: Borrador tecnico actualizado al feature de modelo persistente

## 1. Proposito de Esta Guia

Este documento explica como debe entenderse y evolucionarse la solucion **Servicio Sistema Web Proxy GDEBA-DVBA** desde el punto de vista tecnico.

No es un inventario de archivos. La intencion es que sirva como guia para un desarrollador que abre la solucion en Visual Studio 2022 y necesita responder preguntas concretas:

- Que responsabilidad tiene cada capa.
- Como entra una request HTTP y que recorrido hace.
- Donde se implementa un nuevo metodo GDEBA.
- Como se cambia de una implementacion fake a una implementacion SOAP real.
- Como se maneja la configuracion de ambientes HML y PROD.
- Que decisiones tecnicas no deben romperse.
- Que errores de diseño deben evitarse.

La solucion todavia esta en una etapa inicial. Por eso este documento describe tanto lo que ya esta implementado como el criterio tecnico que deberia guiar los siguientes pasos.

## 2. Idea Central de la Arquitectura

El proxy no debe ser una simple coleccion de controladores que llaman servicios SOAP. Tampoco debe convertirse en el backend funcional de los sistemas internos de Obras, Licitaciones o Certificaciones, etc.

La idea central es construir una capa institucional de integracion con GDEBA. Esa capa debe resolver, de forma centralizada, los problemas tecnicos comunes:

- Autenticacion tecnica contra GDEBA.
- Seleccion de ambiente HML o PROD.
- Consumo de servicios SOAP autorizados.
- Posible evolucion futura a REST.
- Cache local.
- Auditoria.
- Identificacion de la aplicacion consumidora.
- Normalizacion de respuestas y errores.

Para lograrlo, la solucion se organiza con un enfoque de arquitectura limpia. Esto significa que las reglas y casos de uso del proxy no deben depender directamente de ASP.NET Core, Entity Framework, SQL Server, SOAP, JWT ni archivos `appsettings.json`.

La direccion deseada es:

```text
API / Worker
    usan Application
        usa Domain
        define interfaces
    usan Infrastructure
        implementa interfaces de Application
```

La capa `Application` expresa lo que el proxy necesita hacer. La capa `Infrastructure` resuelve como se hace tecnicamente.

## 3. Lectura Rapida de los Proyectos

La solucion esta separada en proyectos con responsabilidades distintas.

```text
ServicioSistemaWebProxyGdebaDvba.sln
  src/
    ServicioSistemaWebProxyGdebaDvba.Api
    ServicioSistemaWebProxyGdebaDvba.Application
    ServicioSistemaWebProxyGdebaDvba.Domain
    ServicioSistemaWebProxyGdebaDvba.Infrastructure
    ServicioSistemaWebProxyGdebaDvba.Worker
```

### 3.1 Domain

`Domain` contiene conceptos propios del proxy. No debe conocer detalles de HTTP, SOAP, controladores, archivos `appsettings.json` ni clientes externos.

El dominio debe responder a la pregunta: **que conceptos existen en el proxy y que reglas propias tienen?** Pero esos conceptos no son todos del mismo tipo. Es importante distinguir datos GDEBA, control de cache, auditoria, value objects, enumeraciones y clases base.

#### Entidades de Datos GDEBA

Las entidades de datos GDEBA representan la informacion funcional que el proxy decide reproducir localmente para poder responder con independencia parcial de GDEBA.

Ejemplos actuales:

- `Expediente`
- `MovimientoExpediente`
- `DocumentoGdeba`
- `DocumentoArchivoLocal`
- `ExpedienteDocumento`
- `TipoDocumentoGdeba`
- `TrataGdeba`

`Expediente` representa la definicion local de un expediente GDEBA con sus datos propios. No es reemplazado por un value object: el value object solo modela el numero compuesto.

`MovimientoExpediente` representa el historial/pase del expediente. No existe una entidad separada llamada historial para guardar los mismos datos, porque el historial esta compuesto por movimientos.

`TrataGdeba` representa el catalogo de tratas informado por GDEBA. La descripcion de la trata no debe duplicarse innecesariamente en cada ocurrencia de expediente cuando puede relacionarse con una entidad de catalogo.

`DocumentoArchivoLocal` representa la ubicacion y metadatos del archivo guardado local o externamente. La base no guarda el binario del PDF/Word; guarda referencias, tipo de archivo, hash, longitud y fechas de descarga/verificacion.

#### Entidades de Control de Cache

Las entidades de control de cache representan frescura, fechas de consulta, vencimiento y fuente de respuesta. Estan separadas fisicamente de las entidades de datos para no mezclar informacion funcional GDEBA con metadatos operativos del proxy.

Ejemplos actuales:

- `ExpedienteCacheControl`
- `HistorialExpedienteCacheControl`
- `DocumentoCacheControl`
- `TrataCacheControl`

`ExpedienteCacheControl` controla la frescura de los datos principales del expediente.

`HistorialExpedienteCacheControl` controla la frescura del historial/movimientos. Se mantiene separado porque el expediente y sus movimientos pueden tener politicas de refresco diferentes.

`DocumentoCacheControl` controla la frescura de la metadata del documento. El archivo local se modela aparte mediante `DocumentoArchivoLocal`, porque una cosa es saber si la metadata esta fresca y otra es saber donde esta almacenado el archivo descargado.

#### Auditoria y Aplicaciones Consumidoras

`AplicacionConsumidora` representa una aplicacion interna que puede consumir el proxy. Puede evolucionar hacia estado activo/inactivo, permisos, nombre visible, responsable tecnico o politicas de consumo.

`RegistroAuditoria` representa una ocurrencia concreta en el tiempo. Debe relacionarse con `AplicacionConsumidora` mediante clave foranea, no almacenar solo un texto suelto que despues no pueda relacionarse.

#### Value Objects

Los value objects representan valores del dominio que se comparan por su contenido, no por identidad. Suelen concentrar validacion, normalizacion o reglas pequeñas asociadas a un dato.

Ejemplo actual:

- `NumeroGdebaCompleto`

`NumeroGdebaCompleto` representa el formato comun usado por GDEBA para expedientes, documentos y otros tipos documentales. No debe llamarse simplemente `Numero`, porque el numero numerico es solo una parte del identificador completo.

Partes persistidas del identificador:

- `GdebaNumeroCompleto`
- `GdebaTipo`
- `GdebaAnio`
- `GdebaNumero`
- `GdebaSistema`
- `GdebaReparticion`

#### DocumentoGdeba y Enriquecimiento Documental

`DocumentoGdeba` distingue entre el numero de actuacion que aparece en el expediente y el numero especial que aparece al consultar el detalle del documento.

El numero de actuacion es el identificador detectado en listados de documentos del expediente, por ejemplo `RS-2023-33144875-GDEBA-DVMIYSPGP`. Con ese dato ya se puede registrar la existencia del documento y relacionarlo con el expediente, aunque todavia no se conozca su metadata completa.

El numero especial es el identificador propio del modulo documental, por ejemplo `RESO-2023-1743-GDEBA-DVMIYSPGP`. Este dato no viene en `buscarExpediente` ni en `consultarExpedienteDetallado`; se obtiene al consultar el detalle documental. Por eso `DocumentoGdeba` permite nacer con metadata incompleta y enriquecerse posteriormente.

El enriquecimiento se implementa sobre `buscarDetallePorNumero` del gateway documental. La operacion unitaria permite enriquecer manualmente un documento especifico, y el proceso por lote reutiliza esa misma operacion para documentos pendientes. Esto evita duplicar reglas cuando una consulta interactiva futura necesite completar metadata al mostrar historicos de documentos oficiales o vinculados.

El tipo documental se separa en codigo, codigo GDEBA, nombre, descripcion, familia, estado y tipo de produccion. Esto evita asumir que toda resolucion se identifica solo con `RESO`, ya que el catalogo GDEBA puede contener varios acronimos relacionados con resoluciones, como `RESO`, `RESCO`, `RESFC` u otros. Para soportar reglas futuras se agrega `TipoDocumentoGdeba`, con banderas como `EsResolucion`.

Una respuesta real de `consultarTipoDocumento` para `RESO` mostro que `acronimo` y `codigoTipoDocumentoGDEBA` no significan lo mismo: `acronimo=RESO` y `codigoTipoDocumentoGDEBA=RS`. Tambien confirmo atributos booleanos como `esAutomatica`, `esComunicable`, `esConfidencial`, `esEmbebido`, `esEspecial`, `esFirmaConjunta`, `esFirmaExterna`, `esManual`, `esNotificable`, `tieneTemplate` y `tieneToken`.

En esta primera etapa `TipoDocumentoGdeba` se mantiene como catalogo consultable por codigo, pero no se fuerza una clave foranea desde `DocumentoGdeba`. La razon es permitir cache progresiva: un documento puede enriquecerse con `TipoDocumentoCodigo` antes de que el catalogo haya sido sincronizado.

El tipo (`EX`, `IF`, etc.) diferencia si el identificador corresponde a expediente, informe/documento u otro elemento. Por eso no corresponde tener value objects separados solo por llamarse expediente o documento si el formato base es el mismo.

#### Enumeraciones

Las enumeraciones representan conjuntos cerrados de valores permitidos. Sirven para evitar strings libres en reglas internas.

Ejemplos actuales:

- `AmbienteGdeba`
- `FuenteRespuesta`

`AmbienteGdeba` permite expresar si una operacion se ejecuto contra HML o PROD. `FuenteRespuesta` indica si una respuesta provino de cache, de GDEBA o de una cache usada como fallback. No son entidades ni value objects complejos; son categorias controladas.

#### Clases Base

`DomainEntity` es una clase base tecnica de dominio para compartir identidad y compatibilidad con URF Trackable. Hereda de `URF.Core.EF.Trackable.Entity` para que los repositorios trackeables de URF puedan funcionar correctamente.

Se evita llamarla simplemente `Entity` para no confundirla con la clase `Entity` provista por URF ni con un concepto funcional del negocio.

La intencion de `Domain` es expresar conceptos propios del proxy, pero diferenciando claramente la naturaleza de cada elemento. No debe presentarse una entidad, un value object y un enum como si fueran objetos equivalentes.

### 3.2 Application

`Application` contiene los casos de uso del proxy. Es la capa que coordina una operacion concreta.

Ejemplo actual:

```text
ExpedienteService
```

Esta clase no deberia saber si GDEBA se consume por SOAP, REST, mock o una respuesta grabada. Solo sabe que necesita un `IGdebaExpedienteGateway`.

Eso es importante: `Application` define interfaces que representan necesidades del caso de uso. Luego `Infrastructure` provee la implementacion concreta.

### 3.3 Infrastructure

`Infrastructure` contiene los detalles tecnicos:

- Implementacion fake de GDEBA.
- Implementaciones SOAP para consulta de expediente y consulta documental.
- Configuracion de GDEBA.
- Acceso a SQL Server mediante EF Core.
- Registros URF `Repository`, `TrackableRepository` y `UnitOfWork`.
- Cliente JWT para autenticacion tecnica contra GDEBA.

Esta capa responde a la pregunta: **como se implementan tecnicamente las necesidades definidas por Application?**

Por ejemplo, Application pide un `IGdebaExpedienteGateway`. Infrastructure decide si ese gateway es fake, SOAP o REST, segun configuracion.

### 3.4 Api

`Api` es el borde HTTP del sistema. Expone endpoints internos para aplicaciones institucionales.

La API debe encargarse de:

- Recibir requests HTTP.
- Validar entrada basica.
- Invocar servicios de Application.
- Devolver respuestas HTTP.
- Ejecutar middleware transversal.

La API no debe construir XML SOAP, no debe leer credenciales GDEBA y no debe implementar reglas de cache directamente.

### 3.5 Worker

`Worker` es el componente preparado para tareas en segundo plano.

En este tipo de proyecto es importante porque la cache y la sincronizacion no siempre dependen de una request de usuario. Algunas tareas probablemente deban ejecutarse por agenda:

- Refrescar expedientes relevantes.
- Sincronizar datos por trata.
- Actualizar historiales.
- Reintentar procesos fallidos.
- Limpiar registros tecnicos.

El Worker debe reutilizar Application e Infrastructure. No debe tener una arquitectura paralela.

El Worker es un host separado de la API. Por eso tiene su propio contenedor de dependencias, su propio ciclo de vida y su propio `appsettings.json`. Cuando registra `AddApplication`, `AddInfrastructure` y `AddGdebaIntegration`, esas capas leen el `IConfiguration` del proceso Worker, no el de la API.

Actualmente el Worker ejecuta dos responsabilidades de fondo:

- Mantener vivos los consumers de MassTransit para trabajos asincronicos de cache.
- Ejecutar enriquecimiento documental programado para documentos con metadata incompleta.

Para enriquecimiento documental, el Worker conserva las decisiones operativas: si el proceso esta habilitado, si la hora local cae dentro de la ventana no pico, que operacion GDEBA se controla para cuota y cuantos documentos se autorizan en el lote. La consulta del consumo se realiza mediante `IConsultaCuotasGdeba`, pero la decision de ejecutar o no ejecutar el lote queda en el Worker.

La logica reutilizable de enriquecer un documento no vive en el Worker. Esta en Application mediante `IDocumentoMetadataEnrichmentService`, que expone una operacion unitaria por documento y una operacion por lote de pendientes. Ambas terminan aplicando reglas del aggregate `DocumentoGdeba`.

### 3.6 Mensajeria con MassTransit y RabbitMQ

Para desacoplar la respuesta inmediata de la API del procesamiento pesado de cache, se incorpora mensajeria mediante MassTransit sobre RabbitMQ.

La API no debe ejecutar trabajo pesado con `Task.Run` ni procesar colecciones grandes antes de responder cuando el endpoint solo necesita devolver cabecera. En su lugar, publica un mensaje durable. El Worker consume ese mensaje y ejecuta la consolidacion de cache.

El flujo definido para el detalle de expediente es:

```text
ExpedienteService
    consulta GDEBA
    arma respuesta liviana
    solicita cache asincronica mediante IExpedienteCacheAsyncPublisher
    responde al cliente

MassTransitExpedienteCacheAsyncPublisher
    publica CachearDetalleExpedienteV1

RabbitMQ
    recibe y entrega el mensaje

CachearDetalleExpedienteConsumer
    consume CachearDetalleExpedienteV1

IExpedienteCacheAsyncProcessor
    ejecuta CachearDetalleAsync

ExpedienteService
    ejecuta ConsolidarDetalleEnCacheAsync
    consolida cabecera, documentos, relaciones y adjuntos
    actualiza estado de cache
```

La abstraccion de Application para solicitar trabajos asincronicos de cache es `IExpedienteCacheAsyncPublisher`. Su implementacion concreta con MassTransit es `MassTransitExpedienteCacheAsyncPublisher`, ubicada en Infrastructure.

La abstraccion de Application para ejecutar esos trabajos es `IExpedienteCacheAsyncProcessor`. El consumer `CachearDetalleExpedienteConsumer` pertenece a Infrastructure porque depende de MassTransit, pero no contiene regla de negocio: solo adapta el mensaje `CachearDetalleExpedienteV1` y delega en `IExpedienteCacheAsyncProcessor`.

La cola inicial configurada es:

```text
gdeba.cachear-detalle-expediente
```

Esta decision evita desperdiciar respuestas grandes de GDEBA sin poner al usuario a esperar la hidratacion completa de cache. El Worker es solo el host que mantiene vivo MassTransit; la logica reutilizable de consolidacion sigue estando en Application y Domain.

## 4. Camino de una Request

Los endpoints principales implementados para expedientes son:

```http
GET /api/gdeba/expedientes/{numeroExpediente}/cabecera
GET /api/gdeba/expedientes/{numeroExpediente}/documentos
GET /api/gdeba/expedientes/{numeroExpediente}/adjuntos
GET /api/gdeba/expedientes/{numeroExpediente}/pases
GET /api/gdeba/expedientes/{numeroExpediente}/relaciones
GET /api/gdeba/expedientes/{numeroExpediente}/completo
GET /api/gdeba/expedientes/{numeroExpediente}/detalle
GET /api/gdeba/expedientes/{numeroExpediente}/movimientos
GET /api/gdeba/expedientes/{numeroExpediente}/sin-cache
GET /api/gdeba/estadisticas/expedientes-por-trata
GET /api/gdeba/cuotas?fecha=YYYY-MM-DD
```

El endpoint `/sin-cache` es una consulta directa contra GDEBA y no representa la consulta funcional normal del proxy con politica de cache.

El endpoint de estadisticas de expedientes por trata es una lectura local sobre
expedientes ya persistidos. No ejecuta sincronizacion, no consulta GDEBA y no
consume cuota. Devuelve totales agrupados por trata y acepta filtros opcionales
por `codigoTrata`, `fechaDesde`, `fechaHasta` y `estado`.

El flujo conceptual de estadisticas es:

```text
Request HTTP
  -> EstadisticasController
  -> IEstadisticasService
  -> EstadisticasService
  -> IEstadisticasReadStore
  -> EstadisticasReadStore
  -> URF SelectSqlAsync
  -> dbo.fn_EstadisticaExpedientesPorTrata
  -> Response HTTP
```

El flujo conceptual es:

```text
Request HTTP
  -> ApplicationIdentificationMiddleware
  -> ExpedientesController
  -> IExpedienteService
  -> ExpedienteService
  -> IGdebaExpedienteGateway
  -> IAuditoriaService
  -> Response HTTP
```

### 4.1 Entrada por Middleware

Antes de llegar al controlador, la request pasa por `ApplicationIdentificationMiddleware`.

Este middleware lee el header:

```http
X-Application-Id
```

Si el header existe, guarda el identificador de la aplicacion en `ICurrentApplicationAccessor`. Eso permite que mas adelante el servicio de aplicacion o la auditoria sepan que aplicacion interna hizo la solicitud.

Actualmente este header solo identifica. No autoriza ni bloquea.

### 4.2 Entrada al Controlador

Luego la request llega a `ExpedientesController`.

El controlador no implementa logica de integracion. Solo traduce HTTP hacia un caso de uso:

```csharp
var result = await _expedienteService.ConsultarDetalleAsync(
    new ConsultarExpedienteDetalladoRequest(numeroExpediente, forceRefresh),
    cancellationToken);
```

Esta separacion es importante. El controlador no debe saber si existe SOAP, fake, cache, auditoria o SQL Server. Su responsabilidad es actuar como adaptador HTTP.

### 4.3 Servicio de Aplicacion

`ExpedienteService` coordina los casos de uso actuales de expediente.

En terminos simples hace esto:

1. Convierte el texto recibido en un `NumeroGdebaCompleto`.
2. Invoca `IGdebaExpedienteGateway`.
3. Registra auditoria.
4. Devuelve un resultado con informacion de fuente y fecha.

El gateway puede ser fake o SOAP real segun `GatewayMode`. El controlador no cambia cuando se reemplaza la implementacion.

### 4.4 Gateway GDEBA

`IGdebaExpedienteGateway` representa la necesidad de consultar expedientes en GDEBA.

La implementacion actual puede ser:

- `FakeGdebaExpedienteGateway`
- `SoapGdebaExpedienteGateway`

La eleccion no se hace editando el controlador ni el servicio de aplicacion. Se hace por configuracion mediante `GatewayMode`.

### 4.5 Morfologia de ConsultarDetalle

El metodo `ConsultarDetalleAsync`, implementado en `ExpedienteService`, representa la consulta bajo demanda de datos detallados del expediente. En esta operacion se invoca `consultarExpedienteDetallado`, que puede devolver cabecera, documentos, relaciones y adjuntos.

Como esa respuesta puede contener colecciones grandes, el endpoint no debe bloquear la respuesta al usuario esperando que toda la informacion sea consolidada en cache. La regla definida es: responder con la cabecera liviana y publicar un mensaje para que el Worker procese la cache completa del detalle.

Esta operacion no consulta movimientos. La decision es intencional: si el endpoint no va a devolver historial de pases, no debe demorar la respuesta esperando una llamada adicional a GDEBA. Los movimientos se resuelven mediante un endpoint propio, apoyado tambien por cache.

Esta lectura en tres niveles permite entender el metodo sin entrar directamente al codigo fuente. El primer nivel muestra la forma general, el segundo explica el flujo funcional y el tercero expresa el pseudocodigo operativo que deberia mantenerse como referencia al evolucionar la implementacion.

#### Nivel 1 - Morfologia General

```text
ConsultarDetalle
    Preparar
    Evaluar cache
    Consultar GDEBA si hace falta
    Publicar cache pesada o aplicar fallback
    Auditar
    Persistir
    Responder
```

#### Nivel 2 - Flujo Funcional

```text
ConsultarDetalle

    Preparar consulta
        - normalizar numero de expediente
        - buscar expediente local
        - preparar contexto de respuesta

    Evaluar cache
        - si el cache local esta completo y vigente, responder desde cache
        - si se solicita refresco forzado, no responder desde cache aunque este vigente

    Consultar GDEBA
        - consultar solo cuando no se puede responder desde cache

    Publicar cache pesada o aplicar fallback
        - si GDEBA devuelve detalle, publicar mensaje para cachear documentos, relaciones y adjuntos
        - la respuesta inmediata se arma con cabecera liviana
        - si GDEBA no devuelve detalle pero existe expediente local, responder con fallback cache
        - si GDEBA no devuelve detalle y no existe expediente local, responder sin datos

    Auditar
        - registrar aplicacion consumidora, operacion, recurso, ambiente, fuente y resultado

    Persistir
        - guardar auditoria de la consulta inmediata
        - la consolidacion pesada de cache queda a cargo del Worker

    Responder
        - devolver cabecera del expediente cuando exista
        - informar fuente de respuesta
        - informar fecha de resolucion
        - informar fecha de cache cuando corresponda
```

#### Nivel 3 - Pseudocodigo

```text
ConsultarDetalle(request)

    PrepararConsulta()

    if PuedeResponderDesdeCache()
        ResolverDesdeCache()
    else
        detalle = ConsultarGdeba()

        if detalle existe
            SolicitarCacheDetalleAsync()
            ResolverCabeceraDesdeGdeba()
        else
            if ExisteExpedienteLocal()
                MarcarErrorActualizacion()
                ResolverDesdeFallbackCache()
            else
                ResolverSinDatos()

    RegistrarAuditoria()
    GuardarCambios()
    DevolverResultado()
```

La regla importante es que la respuesta HTTP no queda a la cola del procesamiento pesado de colecciones. `ExpedienteService` solicita el trabajo asincronico mediante `IExpedienteCacheAsyncPublisher`; MassTransit publica `CachearDetalleExpedienteV1` y el consumer delega la consolidacion en `IExpedienteCacheAsyncProcessor`.

### 4.6 Morfologia de ConsultarMovimientos

El metodo `ConsultarMovimientosAsync`, implementado tambien en `ExpedienteService`, resuelve el historial de pases o movimientos del expediente. Esta informacion proviene del metodo GDEBA `buscarHistorialPasesExpediente` y tiene una importancia operativa central para el seguimiento del tramite.

La politica inicial definida para movimientos es diaria. Si los movimientos fueron sincronizados correctamente dentro de la vigencia configurada, el proxy puede responder desde cache. Si no existe cache vigente, o si la solicitud usa `forceRefresh=true`, se consulta GDEBA y se consolida nuevamente el historial local.

#### Nivel 1 - Morfologia General

```text
ConsultarMovimientos
    Preparar
    Evaluar cache de movimientos
    Consultar GDEBA si hace falta
    Consolidar movimientos o aplicar fallback
    Auditar
    Persistir
    Responder
```

#### Nivel 2 - Flujo Funcional

```text
ConsultarMovimientos

    Preparar consulta
        - normalizar numero de expediente
        - buscar expediente local
        - preparar contexto de respuesta

    Evaluar cache de movimientos
        - si el historial local esta completo y vigente, responder desde cache
        - si se solicita refresco forzado, no responder desde cache aunque este vigente

    Consultar GDEBA
        - consultar buscarHistorialPasesExpediente solo cuando no se puede responder desde cache

    Consolidar o aplicar fallback
        - si GDEBA devuelve historial, consolidar movimientos en el expediente local
        - si GDEBA no devuelve historial pero existe expediente local, responder con fallback cache
        - si GDEBA no devuelve historial y no existe expediente local, responder sin datos

    Auditar
        - registrar aplicacion consumidora, operacion, recurso, ambiente, fuente y resultado

    Persistir
        - guardar movimientos, estado de cache y auditoria en una unica unidad de trabajo

    Responder
        - devolver movimientos cuando existan
        - informar fuente de respuesta
        - informar fecha de resolucion
        - informar fecha de cache cuando corresponda
```

#### Nivel 3 - Pseudocodigo

```text
ConsultarMovimientos(request)

    PrepararConsulta()

    if PuedeResponderMovimientosDesdeCache()
        ResolverMovimientosDesdeCache()
    else
        historial = ConsultarHistorialGdeba()

        if historial existe
            ConsolidarMovimientos()
            ActualizarEstadoCacheHistorial()
            ResolverDesdeGdeba()
        else
            if ExisteExpedienteLocal()
                MarcarErrorActualizacionHistorial()
                ResolverDesdeFallbackCache()
            else
                ResolverSinDatos()

    RegistrarAuditoria()
    GuardarCambios()
    DevolverResultado()
```

La separacion entre `ConsultarDetalle` y `ConsultarMovimientos` no significa que existan dos expedientes distintos. El aggregate root sigue siendo `Expediente`. Lo que se separa es la estrategia de abastecimiento y frescura de datos, porque GDEBA expone la informacion detallada/documental y el historial de pases mediante operaciones diferentes.

## 5. Regla Principal de Diseño

La regla principal es:

> Los casos de uso del proxy deben depender de interfaces, no de implementaciones externas.

Esto significa:

- Application no debe construir XML SOAP.
- Application no debe leer `appsettings.json`.
- Application no debe depender de clases de Infrastructure.
- API no debe llamar directamente a clases SOAP.
- Infrastructure puede implementar interfaces definidas por Application.

Este criterio permite que el proxy pueda cambiar internamente sin afectar a las aplicaciones consumidoras.

Por ejemplo, si hoy se usa SOAP y mañana GDEBA ofrece REST, deberia agregarse o reemplazarse una implementacion en Infrastructure, pero el endpoint interno deberia mantenerse estable.

## 6. Configuracion GDEBA

La seccion `Gdeba` del `appsettings.json` concentra la configuracion de integracion.

La estructura actual es:

```json
"Gdeba": {
  "CurrentEnvironment": "HML",
  "GatewayMode": "Fake",
  "Environments": {
    "HML": {
      "Jwt": { },
      "Soap": { }
    },
    "PROD": {
      "Jwt": { },
      "Soap": { }
    }
  }
}
```

### 6.1 CurrentEnvironment

`CurrentEnvironment` define que ambiente esta activo.

Valores previstos:

```text
HML
PROD
```

Esto no es decorativo. Es necesario porque HML y PROD tienen endpoints distintos.

Antes de esta separacion, existia el riesgo de cambiar el ambiente a PROD pero seguir apuntando a endpoints HML. Por eso se reestructuro la configuracion para que cada ambiente tenga su propio bloque.

### 6.2 GatewayMode

`GatewayMode` define que implementacion concreta se usara para hablar con GDEBA.

Valores previstos:

```text
Fake
Soap
Rest
```

Actualmente:

- `Fake` funciona y devuelve una respuesta simulada.
- `Soap` esta reservado pero aun no implementa el consumo real.
- `Rest` esta reservado para una evolucion futura.

Esto permite cambiar comportamiento sin editar codigo. Por ejemplo:

```json
"GatewayMode": "Fake"
```

puede cambiarse a:

```json
"GatewayMode": "Soap"
```

cuando la implementacion SOAP real este disponible.

### 6.3 Environments

`Environments` contiene la configuracion especifica de cada ambiente.

Ejemplo HML:

```json
"HML": {
  "Soap": {
    "ConsultaExpedienteWsdl": "https://iop.hml.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl"
  }
}
```

Ejemplo PROD:

```json
"PROD": {
  "Soap": {
    "ConsultaExpedienteWsdl": "https://iop.gba.gob.ar/servicios/GDEBA/1/SOAP/consultaExpediente?wsdl"
  }
}
```

Esta separacion permite agregar diferencias por ambiente sin duplicar codigo.

## 7. Por Que Existe IGdebaExecutionContext

El servicio de aplicacion necesita registrar auditoria indicando el ambiente usado. Pero Application no deberia depender de `GdebaOptions`, porque `GdebaOptions` es una clase de configuracion ubicada en Infrastructure.

Para resolver eso se agrego:

```csharp
public interface IGdebaExecutionContext
{
    AmbienteGdeba Ambiente { get; }
    string EnvironmentName { get; }
}
```

Application depende de esta interfaz. Infrastructure implementa la interfaz leyendo `GdebaOptions`.

Esto evita hardcodear:

```csharp
AmbienteGdeba.Hml
```

en los casos de uso.

Tambien evita que Application lea directamente configuracion tecnica.

### 7.1 Ambiente vs EnvironmentName

Hay dos representaciones del ambiente:

```csharp
AmbienteGdeba Ambiente
string EnvironmentName
```

`EnvironmentName` es la clave textual de configuracion, por ejemplo:

```text
HML
PROD
```

`Ambiente` es el enum interno:

```csharp
AmbienteGdeba.Hml
AmbienteGdeba.Prod
```

La razon de tener ambos es practica:

- El string sirve para buscar configuracion en `Environments`.
- El enum sirve para auditoria y logica interna tipada.

Si el nombre `EnvironmentName` resulta confuso, puede renombrarse a `EnvironmentKey` o `AmbienteKey` en una mejora posterior.

## 8. Inyeccion de Dependencias

La composicion de dependencias se hace en `Program.cs`, pero los registros se agrupan en metodos de extension.

En la API:

```csharp
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGdebaIntegration(builder.Configuration);
```

Esto mantiene el `Program.cs` limpio y permite que el Worker reutilice el mismo esquema.

### 8.1 AddApplication

Registra casos de uso propios de Application y servicios transversales cuya implementacion pertenece a Application.

Ejemplo:

```csharp
services.AddScoped<IExpedienteService, ExpedienteService>();
```

Tambien registra `IAuditoriaService` segun la configuracion `Auditoria:Mode`:

- `InMemory`: implementacion simple por logging.
- `Persisted`: implementacion persistida usando abstracciones URF.

Esta capa no registra implementaciones tecnicas de GDEBA. Cuando usa persistencia, debe hacerlo mediante abstracciones de aplicacion/URF, no tomando dependencia directa de `DbContext`.

### 8.2 AddInfrastructure

Registra servicios tecnicos generales.

Actualmente:

- `ICurrentApplicationAccessor`
- `DbContext`
- `IUnitOfWork`
- `IRepository<>`
- `ITrackableRepository<>`

Los registros de EF Core/URF se activan cuando existe la connection string `ProxyGdeba`.

No registra el gateway GDEBA. Eso se separo para que el modo de integracion sea configurable.

### 8.3 AddGdebaIntegration

Registra la integracion GDEBA segun configuracion.

Lee:

```json
Gdeba:GatewayMode
```

Y decide:

```csharp
Fake -> FakeGdebaExpedienteGateway
Soap -> SoapGdebaExpedienteGateway
Rest -> no implementado
```

Tambien valida que exista configuracion para el ambiente seleccionado.

## 9. Middleware de Identificacion de Aplicacion

El middleware `ApplicationIdentificationMiddleware` es una pieza transversal del pipeline HTTP.

No hereda de una clase base ni implementa una interfaz porque ASP.NET Core reconoce middlewares por convencion:

- Constructor con `RequestDelegate`.
- Metodo publico `Invoke` o `InvokeAsync`.
- Primer parametro `HttpContext`.

Su responsabilidad actual es leer:

```http
X-Application-Id
```

y guardar ese valor en:

```csharp
ICurrentApplicationAccessor.Current
```

Esto permite que la auditoria registre que aplicacion interna hizo el pedido.

### 9.1 Que hace hoy

Hoy identifica.

Si llega:

```http
X-Application-Id: obras
```

la auditoria puede asociar la solicitud a la aplicacion consumidora:

```text
AplicacionConsumidora.Codigo = obras
```

### 9.2 Que no hace todavia

Hoy no autoriza.

Eso significa que no rechaza pedidos si falta el header y tampoco valida una clave secreta.

En una etapa posterior puede evolucionar a:

- Exigir `X-Application-Id`.
- Exigir `X-Api-Key`.
- Validar contra base local.
- Validar permisos por operacion.
- Integrar con un servicio troncal de seguridad.

La decision de hacerlo primero como identificacion blanda permite avanzar con auditoria sin mezclar todavia una politica de seguridad que no esta completamente definida.

## 10. Auditoria

La auditoria inicial se diseña como una preocupacion transversal.

Los elementos actuales son:

- `RegistroAuditoria` en Domain.
- `IAuditoriaService` en Application.
- `InMemoryAuditoriaService` en Application.
- `PersistedAuditoriaService` en Application.

La implementacion se elige por configuracion:

```json
"Auditoria": {
  "Mode": "Persisted"
}
```

`InMemory` permite registrar en logs sin persistir. `Persisted` persiste mediante URF y relaciona cada registro con `AplicacionConsumidora`.

Los casos de uso de `ExpedienteService` registran:

- Aplicacion consumidora.
- Operacion.
- Recurso consultado.
- Ambiente GDEBA.
- Fuente de respuesta.
- Resultado.
- Fecha.

La persistencia ya esta modelada, pero queda pendiente generar/aplicar migraciones y ajustar detalles operativos de auditoria cuando se definan duracion, correlation id y errores normalizados.

### 10.1 Por Que Auditoria No Esta en el Controller

La auditoria no deberia depender del controlador. Si manana el mismo caso de uso se ejecuta desde un Worker, desde una cola o desde otro endpoint, tambien deberia poder auditarse.

Por eso la auditoria se invoca desde Application.

### 10.2 Que Falta Mejorar

Faltan aspectos importantes:

- Duracion de la llamada.
- Correlation Id.
- Error normalizado.
- Enmascaramiento de datos sensibles.
- Identificacion robusta de aplicacion.

## 11. Fake, SOAP y REST

La existencia de `FakeGdebaExpedienteGateway` no es un detalle menor. Es una decision practica para poder avanzar con la arquitectura sin depender inmediatamente de GDEBA.

El fake permite:

- Probar controladores.
- Probar inyeccion de dependencias.
- Probar middleware.
- Probar auditoria.
- Probar respuesta de API.
- Desarrollar frontend o consumidores internos sin tener SOAP listo.

La implementacion SOAP de expediente vive en `SoapGdebaExpedienteGateway`. Actualmente cubre `consultarExpedienteDetallado` y `buscarHistorialPasesExpediente`, obtiene JWT mediante `GdebaJwtTokenProvider`, arma el envelope SOAP en Infrastructure, parsea la respuesta XML y registra la invocacion para control de cuotas.

La implementacion SOAP documental vive en `SoapGdebaDocumentoGateway` y cubre `buscarDetallePorNumero`.

La API y Application no deberian cambiar por ese reemplazo.

## 12. Como Agregar un Nuevo Metodo GDEBA

Supongamos que se quiere agregar:

```text
buscarDatosExpedientePorCodigosTrata
```

No conviene empezar creando directamente un controlador que arme XML.

El flujo recomendado es:

### Paso 1: Definir el Caso de Uso en Application

Crear una interfaz, por ejemplo:

```csharp
public interface IConsultarHistorialPasesService
{
    Task<ConsultarHistorialPasesResult> ConsultarAsync(
        ConsultarHistorialPasesRequest request,
        CancellationToken cancellationToken);
}
```

### Paso 2: Definir DTOs Internos

Crear modelos internos que representen lo que el proxy quiere devolver, no necesariamente la forma exacta del XML SOAP.

Por ejemplo:

```csharp
public sealed record PaseExpedienteDto(
    DateTimeOffset? FechaOperacion,
    string? TipoOperacion,
    string? Motivo,
    string? Usuario);
```

### Paso 3: Extender el Gateway

Agregar una operacion a un gateway existente o crear uno nuevo:

```csharp
public interface IGdebaExpedienteGateway
{
    Task<IReadOnlyList<PaseExpedienteDto>> BuscarHistorialPasesAsync(
        NumeroGdebaCompleto numero,
        CancellationToken cancellationToken);
}
```

### Paso 4: Implementar Fake

Agregar una respuesta simulada en el fake para poder probar el endpoint sin SOAP real.

### Paso 5: Implementar SOAP Real

Implementar la llamada real en Infrastructure.

Alli deben resolverse:

- Envelope SOAP.
- Namespaces.
- Headers.
- JWT.
- `Content-Type: application/xml; charset=UTF-8`.
- Faults SOAP.
- Parseo de respuesta.

### Paso 6: Crear Endpoint

Agregar un controlador o accion:

```http
GET /api/gdeba/expedientes/{numero}/historial-pases
```

El controlador solo debe llamar al servicio de Application.

### Paso 7: Auditoria y Cache

Definir que se audita y si corresponde cachear la respuesta.

Para historial de pases, probablemente haya que cachear con TTL corto o medio, porque puede cambiar durante la tramitacion.

## 13. Donde Va Cada Cosa

Esta tabla sirve como regla practica:

| Necesidad | Proyecto recomendado |
|---|---|
| Nuevo endpoint HTTP | Api |
| Nuevo caso de uso | Application |
| Nueva interfaz de servicio externo | Application |
| Implementacion SOAP | Infrastructure |
| Implementacion fake | Infrastructure |
| Entidad o regla conceptual del proxy | Domain |
| Repositorio SQL Server | Infrastructure |
| Configuracion de endpoints GDEBA | appsettings + Infrastructure |
| Middleware HTTP | Api |
| Auditoria persistida | Application, usando abstracciones URF y entidades de Domain |
| Cache persistida | Domain + Infrastructure/Persistence, usando EF Core/URF |

## 14. Cosas Que Deben Evitarse

### 14.1 No Poner SOAP en Controllers

Los controladores no deben construir XML, manejar WSDL ni interpretar faults SOAP.

Si eso ocurre, la API queda acoplada a GDEBA y se pierde el beneficio del proxy como capa estable.

### 14.2 No Inyectar GdebaOptions en Application

Application no deberia depender de `GdebaOptions` porque esa clase pertenece a Infrastructure.

Si Application necesita saber el ambiente, debe usar `IGdebaExecutionContext`.

### 14.3 No Hardcodear HML o PROD

El ambiente debe venir de configuracion.

Hardcodear:

```csharp
AmbienteGdeba.Hml
```

en un caso de uso genera errores cuando se cambie a PROD.

### 14.4 No Usar Repository Para GDEBA

Repository debe reservarse para persistencia local.

GDEBA es un sistema externo, por eso se usa Gateway o Adapter.

### 14.5 No Exponer SOAP Crudo en la API Interna

El proxy no debe ser un pasamanos generico de XML.

Debe exponer operaciones internas conocidas, autorizadas y auditables.

## 15. Estado Tecnico Actual

Actualmente esta implementado:

- Solucion .NET 8.
- Proyectos separados.
- API con controladores clasicos.
- Worker con consumers de mensajeria y enriquecimiento documental programado.
- Middleware `X-Application-Id`.
- Configuracion GDEBA por ambiente.
- Selector de gateway por `GatewayMode`.
- Gateways fake y SOAP para expediente.
- Gateways fake y SOAP para consulta documental `buscarDetallePorNumero`.
- Auditoria configurable `InMemory`/`Persisted`.
- Control de cuotas e invocaciones GDEBA, incluyendo origen `WorkerProgramado`.
- Modelo persistente inicial para expedientes, movimientos, documentos, archivos locales, tratas, cache control, aplicaciones consumidoras y auditoria.
- EF Core con `ProxyGdebaDbContext` y configuraciones explicitas.
- URF para `Repository`, `TrackableRepository` y `UnitOfWork`.
- `.gitignore` para Visual Studio y .NET.
- Primera version de comportamiento de aggregate roots mediante clases parciales, sin mover las entidades existentes.
- Enriquecimiento de `DocumentoGdeba` con metadata e historial documental.

### 15.1 Aggregate Roots Mediante Partials

Para mantener la estructura actual de entidades y conservar simple la persistencia con EF Core/URF, el comportamiento inicial de los aggregate roots se implementa con clases parciales.

La entidad principal permanece en su ubicacion actual. Por ejemplo, `Expediente` sigue en `Domain/Entities/Gdeba/Expediente.cs`. El comportamiento del aggregate root se agrega en `Domain/AggregateRoots/Expediente.AggregateRoot.cs`, usando el mismo namespace y la misma clase parcial.

Esto permite que repositorios, `DbContext` y configuraciones EF sigan trabajando con `Expediente` y `DocumentoGdeba`, mientras la logica propia del agregado queda separada visualmente del estado persistente.

En esta primera version se consideran aggregate roots:

- `Expediente`, para la vista local/cacheada del expediente GDEBA, deteccion de documentos, relaciones, adjuntos y control de cache.
- `DocumentoGdeba`, para metadata documental, enriquecimiento y futura descarga/cache documental.

No se crean clases independientes llamadas `ExpedienteAggregateRoot` ni se reorganiza el directorio `Entities`. El aggregate root es la propia entidad principal.

En Application, las operaciones de expediente se exponen mediante un unico `IExpedienteService`. Esto evita replicar la granularidad de los metodos SOAP en clases de caso de uso separadas y mantiene un unico punto de entrada funcional para consultas de expediente, detalle, historial y futuras sincronizaciones por trata.

### 15.3 Estadisticas Locales

El modulo de estadisticas se modela como una capacidad general, no como una
operacion propia de expedientes ni de tratas. La primera estadistica implementada
es `ObtenerTotalesExpedientesPorTrataAsync`, que devuelve totales de expedientes
agrupados por trata.

Componentes iniciales:

- `EstadisticasController`: expone el endpoint HTTP.
- `IEstadisticasService` y `EstadisticasService`: caso de uso de Application.
- `IEstadisticasReadStore` y `EstadisticasReadStore`: lectura local mediante
  URF.
- `EstadisticaExpedientesPorTrataReadModel`: modelo tecnico de lectura.
- `dbo.fn_EstadisticaExpedientesPorTrata`: funcion SQL Server que calcula el
  agregado.

El endpoint inicial es:

```http
GET /api/gdeba/estadisticas/expedientes-por-trata
```

Los filtros son opcionales:

```http
GET /api/gdeba/estadisticas/expedientes-por-trata?codigoTrata=...&fechaDesde=YYYY-MM-DD&fechaHasta=YYYY-MM-DD&estado=...
```

Reglas de filtros:

- Si `codigoTrata` no se informa, se incluyen todas las tratas.
- Si `fechaDesde` no se informa, no se aplica limite inferior.
- Si `fechaHasta` no se informa, no se aplica limite superior.
- Si `estado` no se informa, se incluyen todos los estados.
- `fechaHasta` se transforma en limite exclusivo del dia siguiente para incluir
  todo el dia solicitado sin aplicar funciones sobre la columna
  `FechaCaratulacion`.

La consulta agregada se resuelve en SQL Server mediante:

```sql
dbo.fn_EstadisticaExpedientesPorTrata(
    @CodigoTrata,
    @FechaDesde,
    @FechaHastaExclusiva,
    @Estado)
```

Esta funcion se crea por migracion EF Core y no representa una tabla funcional
del dominio. Su resultado se mapea con un read model keyless configurado con
`HasNoKey()` y `ToView(null)`, para que URF pueda ejecutar la lectura mediante
`SelectSqlAsync` sin materializar expedientes.

La construccion de parametros se realiza desde Application con valores
posiblemente nulos. No se concatena SQL con valores de entrada del usuario. Si un
filtro no viene informado, se envia `DBNull.Value` para que la funcion aplique
la regla `@Parametro IS NULL OR ...`.

El manejo de errores tecnicos de la consulta pertenece al read store. Ante una
falla de SQL Server, URF, permisos, timeout o ausencia de la funcion, el read
store registra el error y lanza una `InvalidOperationException` con mensaje
funcional. Las cancelaciones se propagan mediante `OperationCanceledException`
sin registrarse como error.

Pendiente:

- Completar validacion de clientes SOAP reales contra ambiente GDEBA.
- Implementacion de `buscarDatosExpedientePorCodigosTrata`.
- Migraciones EF Core y creacion efectiva de base SQL Server.
- Servicios de lectura/escritura de cache sobre el modelo persistente.
- Validacion real de aplicaciones consumidoras.
- Tests automatizados.

## 16. Criterio Para Continuar

La recomendacion es avanzar por capacidades verticales pequeñas.

Una capacidad vertical completa deberia incluir:

1. Endpoint interno.
2. Caso de uso en Application.
3. Gateway fake.
4. Auditoria.
5. Configuracion necesaria.
6. Prueba manual o automatizada.
7. Implementacion SOAP real cuando corresponda.

Los dos proximos candidatos naturales son:

- `buscarDatosExpedientePorCodigosTrata`
- `buscarHistorialPasesExpediente`

Ambos son importantes para el problema de sincronizacion, cache y frescura de datos.

## 17. Resumen de la Filosofia Tecnica

La filosofia tecnica de esta solucion puede resumirse asi:

> Las aplicaciones internas deben hablar con una API institucional estable.  
> La API institucional debe ejecutar casos de uso propios del proxy.  
> Los casos de uso deben depender de interfaces.  
> La infraestructura debe resolver los detalles cambiantes de GDEBA.  
> La configuracion debe permitir elegir ambiente e implementacion sin tocar codigo.  
> La auditoria y la identificacion de aplicacion deben ser transversales.

Si se respeta esa regla, el proxy podra crecer sin convertirse en una mezcla de controladores, XML, credenciales, SQL y reglas funcionales ajenas.
