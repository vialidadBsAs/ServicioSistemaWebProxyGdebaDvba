# Guia Tecnica de Arquitectura e Implementacion

Version: 0.2  
Fecha: 29/05/2026  
Estado: Borrador tecnico inicial

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

`Domain` contiene conceptos propios del proxy. No debe conocer detalles de HTTP, SOAP, EF Core, SQL Server ni archivos de configuracion.

El dominio debe responder a la pregunta: **que conceptos existen en el proxy y que reglas propias tienen?** Pero esos conceptos no son todos del mismo tipo. Es importante distinguir entidades, value objects, enumeraciones y clases base, porque cada una cumple una funcion distinta.

#### Entidades

Las entidades representan conceptos con identidad propia dentro del sistema. Tienen un identificador y pueden tener ciclo de vida. Se las reconoce porque no alcanza con comparar sus valores: interesa saber que son una ocurrencia o registro concreto.

Ejemplos actuales:

- `AplicacionConsumidora`
- `ExpedienteCache`
- `RegistroAuditoria`

`AplicacionConsumidora` representa una aplicacion interna que puede consumir el proxy. En el futuro podria tener estado activo/inactivo, permisos, nombre visible, responsable tecnico o politicas de consumo.

`ExpedienteCache` representa una copia local administrada de informacion de un expediente. Tiene identidad propia porque puede tener fecha de cacheo, vencimiento, estado parcial y futuras reglas de refresco.

`RegistroAuditoria` representa una ocurrencia concreta en el tiempo: una aplicacion hizo una operacion sobre un recurso, en un ambiente determinado y con cierto resultado. Eso no es un simple valor; es un evento auditable.

#### Value Objects

Los value objects representan valores del dominio que se comparan por su contenido, no por identidad. Suelen concentrar validacion, normalizacion o reglas pequeñas asociadas a un dato.

Ejemplo actual:

- `NumeroExpediente`

`NumeroExpediente` normaliza el texto recibido para evitar diferencias por espacios multiples. Dos instancias con el mismo valor normalizado representan el mismo numero de expediente. Por eso corresponde modelarlo como value object y no como entidad.

#### Enumeraciones

Las enumeraciones representan conjuntos cerrados de valores permitidos. Sirven para evitar strings libres en reglas internas.

Ejemplos actuales:

- `AmbienteGdeba`
- `FuenteRespuesta`

`AmbienteGdeba` permite expresar si una operacion se ejecuto contra HML o PROD. `FuenteRespuesta` indica si una respuesta provino de cache, de GDEBA o de una cache usada como fallback. No son entidades ni value objects complejos; son categorias controladas.

#### Clases Base

`Entity` es una clase base tecnica de dominio para compartir el concepto de identidad entre entidades. No representa una regla funcional por si misma y no deberia confundirse con un concepto de negocio.

La intencion de `Domain` es expresar conceptos propios del proxy, pero diferenciando claramente la naturaleza de cada elemento. No debe presentarse una entidad, un value object y un enum como si fueran objetos equivalentes.

### 3.2 Application

`Application` contiene los casos de uso del proxy. Es la capa que coordina una operacion concreta.

Ejemplo actual:

```text
ConsultarExpedienteService
```

Esta clase no deberia saber si GDEBA se consume por SOAP, REST, mock o una respuesta grabada. Solo sabe que necesita un `IGdebaExpedienteGateway`.

Eso es importante: `Application` define interfaces que representan necesidades del caso de uso. Luego `Infrastructure` provee la implementacion concreta.

### 3.3 Infrastructure

`Infrastructure` contiene los detalles tecnicos:

- Implementacion fake de GDEBA.
- Implementacion futura SOAP.
- Configuracion de GDEBA.
- Auditoria por logging.
- Acceso futuro a SQL Server.
- Repositorios URF futuros.
- Cliente JWT futuro.

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

## 4. Camino de una Request

El endpoint inicial implementado es:

```http
GET /api/gdeba/expedientes/{numeroExpediente}
```

El flujo conceptual es:

```text
Request HTTP
  -> ApplicationIdentificationMiddleware
  -> ExpedientesController
  -> IConsultarExpedienteService
  -> ConsultarExpedienteService
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
var result = await _consultarExpedienteService.ConsultarAsync(
    new ConsultarExpedienteRequest(numeroExpediente, forceRefresh),
    cancellationToken);
```

Esta separacion es importante. El controlador no debe saber si existe SOAP, fake, cache, auditoria o SQL Server. Su responsabilidad es actuar como adaptador HTTP.

### 4.3 Servicio de Aplicacion

`ConsultarExpedienteService` coordina el caso de uso.

En terminos simples hace esto:

1. Convierte el texto recibido en un `NumeroExpediente`.
2. Invoca `IGdebaExpedienteGateway`.
3. Registra auditoria.
4. Devuelve un resultado con informacion de fuente y fecha.

Hoy el gateway es fake. Mas adelante podra ser SOAP real, sin que el controlador cambie.

### 4.4 Gateway GDEBA

`IGdebaExpedienteGateway` representa la necesidad de consultar expedientes en GDEBA.

La implementacion actual puede ser:

- `FakeGdebaExpedienteGateway`
- `SoapGdebaExpedienteGateway`

La eleccion no se hace editando el controlador ni el servicio de aplicacion. Se hace por configuracion mediante `GatewayMode`.

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
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddGdebaIntegration(builder.Configuration);
```

Esto mantiene el `Program.cs` limpio y permite que el Worker reutilice el mismo esquema.

### 8.1 AddApplication

Registra casos de uso propios de Application.

Ejemplo:

```csharp
services.AddScoped<IConsultarExpedienteService, ConsultarExpedienteService>();
```

Esta capa no registra implementaciones tecnicas de GDEBA ni acceso a datos.

### 8.2 AddInfrastructure

Registra servicios tecnicos generales.

Actualmente:

- `ICurrentApplicationAccessor`
- `IAuditoriaService`

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

la auditoria puede registrar:

```text
Aplicacion = obras
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
- `InMemoryAuditoriaService` en Infrastructure.

Actualmente se registra en logs, no en base de datos.

El caso de uso `ConsultarExpedienteService` registra:

- Aplicacion consumidora.
- Operacion.
- Recurso consultado.
- Ambiente GDEBA.
- Fuente de respuesta.
- Resultado.
- Fecha.

Mas adelante deberia persistirse en SQL Server.

### 10.1 Por Que Auditoria No Esta en el Controller

La auditoria no deberia depender del controlador. Si manana el mismo caso de uso se ejecuta desde un Worker, desde una cola o desde otro endpoint, tambien deberia poder auditarse.

Por eso la auditoria se invoca desde Application.

### 10.2 Que Falta Mejorar

Faltan aspectos importantes:

- Duracion de la llamada.
- Correlation Id.
- Error normalizado.
- Persistencia real.
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

Cuando se implemente SOAP real, deberia hacerse en `SoapGdebaExpedienteGateway`.

La API y Application no deberian cambiar por ese reemplazo.

## 12. Como Agregar un Nuevo Metodo GDEBA

Supongamos que se quiere agregar:

```text
buscarHistorialPasesExpediente
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
        NumeroExpediente numero,
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
| Auditoria persistida | Infrastructure, usando contrato de Application |
| Cache persistida | Infrastructure, usando contratos de Application/Domain |

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
- Worker inicial.
- Middleware `X-Application-Id`.
- Configuracion GDEBA por ambiente.
- Selector de gateway por `GatewayMode`.
- Gateway fake.
- Gateway SOAP reservado.
- Auditoria inicial por logging.
- `.gitignore` para Visual Studio y .NET.

Pendiente:

- Cliente JWT real.
- Cliente SOAP real.
- Implementacion de `buscarDatosExpedientePorCodigosTrata`.
- Implementacion de `buscarHistorialPasesExpediente`.
- SQL Server.
- EF Core/URF real.
- Cache persistida.
- Auditoria persistida.
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
