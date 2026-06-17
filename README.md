# Servicio Sistema Web Proxy GDEBA-DVBA

Servicio institucional de interoperabilidad entre los sistemas internos de la Direccion Provincial de Vialidad de la Provincia de Buenos Aires y los servicios web autorizados de GDEBA.

## Proposito del proyecto

El objetivo principal del proyecto es centralizar el consumo de servicios GDEBA en una capa comun, segura, auditable y reutilizable por distintas aplicaciones internas de DVBA.

La solucion nace para evitar que cada sistema institucional implemente por separado la autenticacion tecnica, el armado de peticiones SOAP, el tratamiento de respuestas, la cache, la auditoria y la configuracion de ambientes. En lugar de distribuir esa complejidad en Obras, Licitaciones, Seguimiento, Ejecucion u otros sistemas futuros, el proxy concentra esa responsabilidad y expone una API interna mas estable.

El proxy no reemplaza la logica de negocio de los sistemas consumidores. Por ejemplo, si un sistema de certificacion de obra debe decidir cuando asociar un expediente a un certificado, esa regla sigue perteneciendo al backend de certificacion. El proxy solo provee el soporte tecnico e institucional para consultar o ejecutar operaciones autorizadas contra GDEBA.

## Alcance actual

La primera version del proyecto define la base de arquitectura, configuracion y extensibilidad. Ya existe modelo persistente inicial, control de cuotas, mensajeria para cache de expedientes y un primer proceso worker para enriquecer metadata documental. La integracion SOAP real todavia se completa por capacidad y depende de la configuracion de ambiente.

Actualmente la solucion incluye:

- API interna con controladores clasicos.
- Worker para procesos en segundo plano.
- Separacion por capas segun arquitectura limpia.
- Middleware para identificar la aplicacion consumidora mediante `X-Application-Id`.
- Configuracion GDEBA por ambiente (`HML` y `PROD`).
- Selector de implementacion de gateway mediante `GatewayMode`.
- Gateways fake y SOAP para expedientes/documentos segun capacidad implementada.
- Auditoria configurable y control de cuotas por invocacion GDEBA.
- Enriquecimiento asincronico de metadata documental mediante `buscarDetallePorNumero`.
- Documentacion tecnica y funcional en `docs/`.

## Estructura de la solucion

```text
src/
  ServicioSistemaWebProxyGdebaDvba.Api
  ServicioSistemaWebProxyGdebaDvba.Application
  ServicioSistemaWebProxyGdebaDvba.Domain
  ServicioSistemaWebProxyGdebaDvba.Infrastructure
  ServicioSistemaWebProxyGdebaDvba.Worker
```

### Domain

Contiene conceptos propios del proxy y no debe depender de HTTP, SOAP, EF Core, SQL Server ni archivos de configuracion.

En esta capa viven entidades como `AplicacionConsumidora`, `ExpedienteCache` y `RegistroAuditoria`; value objects como `NumeroExpediente`; y enumeraciones como `AmbienteGdeba` y `FuenteRespuesta`.

### Application

Contiene los casos de uso, contratos internos, DTOs e interfaces que expresan lo que el sistema necesita hacer sin atarse a una tecnologia concreta.

Esta capa define abstracciones como `IGdebaExpedienteGateway`, `IAuditoriaService`, `ICurrentApplicationAccessor` e `IGdebaExecutionContext`. Las implementaciones reales quedan fuera, en Infrastructure.

### Infrastructure

Contiene implementaciones tecnicas: gateways fake/SOAP, acceso a SQL Server, auditoria persistida, control de cuotas, configuracion GDEBA y registro de dependencias.

Tambien es la capa donde se decide, por configuracion, si se trabaja contra una implementacion fake, SOAP real o eventualmente REST.

### Api

Expone endpoints HTTP internos para las aplicaciones consumidoras.

La API no deberia construir XML SOAP, leer credenciales GDEBA ni conocer detalles de bajo nivel de integracion. Su tarea es recibir solicitudes internas, invocar casos de uso y devolver respuestas normalizadas.

### Worker

Ejecuta procesos en segundo plano: consumo de mensajes de cache, refrescos programados y enriquecimiento asincronico de documentos. Es un host separado de la API, con su propio `appsettings.json` y su propio contenedor de dependencias.

El worker de enriquecimiento documental decide cuando correr, que operacion controlar y que lote procesar. La logica reutilizable de enriquecimiento vive en Application y aplica reglas del aggregate `DocumentoGdeba`.

## Configuracion GDEBA

La configuracion principal esta en `appsettings.json`, seccion `Gdeba`.

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

`CurrentEnvironment` indica el ambiente activo de GDEBA. `GatewayMode` permite cambiar la implementacion sin modificar codigo. `Environments` separa endpoints y parametros por ambiente, porque HML y PROD no necesariamente comparten URLs.

Las credenciales no deben quedar hardcodeadas ni subirse al repositorio. La implementacion final deberia resolverlas mediante variables de entorno, user secrets, secret manager o el mecanismo institucional que se defina.

## Primeros endpoints

Health:

```http
GET /api/health
```

Expedientes:

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
X-Application-Id: obras
```

Control de cuotas:

```http
GET /api/gdeba/cuotas?fecha=2026-06-17
```

`X-Application-Id` es una convencion interna del proxy. En el estado actual identifica a la aplicacion consumidora para auditoria, pero todavia no autoriza ni bloquea solicitudes.

## Servicios prioritarios

Los metodos GDEBA que quedaron identificados como prioritarios para avanzar son:

- `buscarDatosExpedientePorCodigosTrata`
- `buscarHistorialPasesExpediente`

Estos servicios son importantes porque impactan directamente en la estrategia de cache, refresco de datos y sincronizacion local. Tambien son los que hoy presentan mayor dificultad operativa por la falta de filtros incrementales o por reparticion.

## Documentacion del proyecto

La documentacion viva se encuentra en `docs/`.

Los documentos mas relevantes para retomar el trabajo son:

- `docs/00-contexto-proyecto.md`
- `docs/01-servicios-autorizados.md`
- `docs/02-arquitectura-propuesta.md`
- `docs/03-politica-cache-sincronizacion.md`
- `docs/04-integracion-gdeba.md`
- `docs/05-decisiones-arquitectura.md`
- `docs/06-pendientes-gdeba.md`
- `docs/07-arquitectura-tecnica-implementacion.md`
- `docs/08-notas-de-trabajo-y-decisiones.md`

El archivo `docs/08-notas-de-trabajo-y-decisiones.md` funciona como memoria de continuidad: resume decisiones tomadas durante el trabajo inicial, problemas resueltos y criterios para seguir desde otra maquina o desde otra sesion de Codex.

## Estado pendiente

Quedan como proximos pasos tecnicos:

- Validar cliente JWT real contra GDEBA.
- Completar y validar clientes SOAP reales respetando `Content-Type: application/xml; charset=UTF-8`.
- Aplicar y revisar migraciones EF Core sobre SQL Server.
- Definir autorizacion real de aplicaciones consumidoras.
- Persistir auditoria.
- Completar cache por tipo de dato y politica de frescura.
- Implementar los metodos GDEBA prioritarios.
- Agregar pruebas automatizadas.

