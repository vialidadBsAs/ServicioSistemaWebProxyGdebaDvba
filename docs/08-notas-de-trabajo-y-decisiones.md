# Notas de Trabajo y Decisiones Operativas

Version: 0.3  
Fecha: 31/05/2026  
Estado: Documento de continuidad

## 1. Proposito de este documento

Este documento existe para conservar el contexto de trabajo que se genero durante la construccion inicial del proyecto. No reemplaza a la documentacion tecnica formal, pero ayuda a retomar el desarrollo desde otra maquina, desde otra sesion de Codex o despues de varios dias sin perder decisiones importantes.

La conversacion de trabajo fue extensa e incluyo decisiones de arquitectura, problemas de Git, dudas sobre GDEBA, criterios de cache, autenticacion, auditoria y estructura de solucion. Como el historial del chat no debe ser la unica fuente de verdad, las conclusiones relevantes se consolidan aca.

## 2. Estado general del proyecto

El proyecto ya cuenta con una solucion .NET 8 inicial, separada por capas, con documentacion base, commit inicial y repositorio remoto en GitHub.

La solucion todavia esta en una etapa temprana. El objetivo de lo construido hasta ahora no fue cerrar la integracion real con GDEBA, sino dejar una base ordenada para poder avanzar sin mezclar responsabilidades tecnicas, reglas propias del proxy y detalles de transporte SOAP.

## 3. Repositorio, rutas y continuidad de Codex

La ruta real actual del proyecto es:

```text
C:\Users\admin\source\repos\ServicioSistemaWebProxyGdebaDvba
```

Durante el trabajo inicial existieron rutas anteriores bajo `Documents`. Si se retoma el desarrollo desde Visual Studio o Codex, conviene verificar primero `git status` dentro de la ruta actual y no asumir que otra carpeta es el repositorio activo.

## 4. Identidad Git y GitHub

El repositorio remoto debe permanecer bajo el propietario:

```text
vialidadBsAs
```

Los commits deben quedar atribuidos al usuario:

```text
pdelucca <pablofranciscodelucca@hotmail.com>
```

Durante las pruebas se detecto que el email:

```text
pdelucca@vialidad.gba.gov.ar
```

era valido a nivel Git, pero GitHub lo atribuia visualmente a `vialidadBsAs`. Por eso, para mantener la autoria personal correcta en GitHub, el repositorio local debe usar el email `pablofranciscodelucca@hotmail.com`.

Comandos recomendados dentro del repo:

```powershell
git config user.name "pdelucca"
git config user.email "pablofranciscodelucca@hotmail.com"
git log --format=fuller -1
```

La verificacion debe mostrar `Author` y `Commit` con el mismo usuario y email.

## 5. Remoto GitHub

El repositorio remoto se trabaja bajo `vialidadBsAs`, pero puede pushearse con `pdelucca` siempre que esa cuenta tenga permisos.

Si por algun motivo hubiera que recrear el remoto, conviene crearlo vacio: sin README, sin `.gitignore` y sin licencia. Luego se configura el remoto local y se sube `master`.

```powershell
git remote add origin https://github.com/vialidadBsAs/ServicioSistemaWebProxyGdebaDvba.git
git push -u origin master
```

Si el remoto ya existe, no debe recrearse ni forzarse nada sin revisar primero `git remote -v`, `git status` y `git log --format=fuller -1`.

## 6. Finales de linea en Windows

Visual Studio puede mostrar avisos de finales de linea inconsistentes, y Git puede emitir mensajes como:

```text
LF will be replaced by CRLF
```

Esto no significa que el proyecto este roto. Indica que Windows y Git estan normalizando finales de linea. Si se vuelve un problema recurrente, se puede agregar un `.gitattributes` para fijar una politica explicita por tipo de archivo.

## 7. Decision de arquitectura

Se decidio usar una arquitectura limpia con separacion en proyectos:

```text
Domain
Application
Infrastructure
Api
Worker
```

La razon principal es que, aunque el proyecto parece inicialmente un simple consumidor de servicios externos, en realidad tiene responsabilidades institucionales propias: auditoria, trazabilidad, cache, control de aplicaciones consumidoras, configuracion de ambientes, aislamiento de credenciales y evolucion futura del canal de integracion.

Separar las capas permite que un cambio tecnico de GDEBA, por ejemplo migrar de SOAP a REST, no obligue a modificar controladores, casos de uso o reglas internas del proxy.

## 8. Responsabilidad de cada capa

`Domain` contiene conceptos propios del proxy. No debe conocer HTTP, SOAP, EF Core, SQL Server ni archivos de configuracion. Dentro de esta capa hay tipos de distinta naturaleza: entidades, value objects, enumeraciones y clases base. No deben interpretarse todos como entidades de negocio equivalentes.

`Application` contiene casos de uso, DTOs e interfaces. Esta capa expresa lo que el proxy necesita hacer, pero no decide como se conecta tecnicamente a GDEBA ni como se persiste en SQL Server.

`Infrastructure` contiene implementaciones concretas: gateways, configuracion, acceso futuro a base de datos, servicios de auditoria y clientes tecnicos.

`Api` expone endpoints internos. No debe construir XML SOAP ni leer credenciales GDEBA.

`Worker` queda reservado para procesos de fondo, especialmente sincronizacion y refresco de cache.

## 9. GatewayMode

Se agrego `GatewayMode` para decidir la implementacion de integracion desde configuracion y no desde codigo.

Modos previstos:

- `Fake`: implementacion simulada para desarrollo.
- `Soap`: implementacion real contra servicios SOAP GDEBA.
- `Rest`: reservado por si GDEBA migra o expone servicios REST en el futuro.

Esta decision evita tener que cambiar registros manuales de dependencias cada vez que se quiera alternar entre una implementacion de prueba y una real.

## 10. Ambientes GDEBA

GDEBA tiene endpoints distintos por ambiente. Por eso la configuracion separa `HML` y `PROD`.

El ambiente activo se define con:

```json
"CurrentEnvironment": "HML"
```

La resolucion del ambiente no debe quedar hardcodeada en los casos de uso. Para eso se agrego `IGdebaExecutionContext`, que permite a Application conocer el ambiente activo sin depender directamente de `GdebaOptions`.

## 11. Middleware de aplicacion consumidora

Se implemento `ApplicationIdentificationMiddleware`, que lee el header:

```http
X-Application-Id
```

Este header es una convencion interna del proxy. En el estado actual sirve para identificar que aplicacion interna realizo la solicitud y registrar esa informacion en auditoria.

Por ahora no autoriza ni bloquea. Mas adelante puede evolucionar para validar aplicaciones habilitadas, aplicar politicas de consumo, registrar cuotas o rechazar consumidores desconocidos.

## 12. Autenticacion contra GDEBA

La autorizacion tecnica contra GDEBA se realiza mediante el servicio JWT:

```text
https://iop.gba.gob.ar/servicios/JWT/1/REST/jwt
```

El mecanismo informado es Basic Auth con username y password, para obtener un token JWT que luego se usa en las invocaciones autorizadas.

Decision posterior del feature `soap-expediente-gateway`:

- Se agrego `GdebaJwtTokenProvider` en Infrastructure.
- El provider resuelve la configuracion del ambiente activo y obtiene el token con Basic Auth.
- La respuesta puede venir como texto plano o como JSON con propiedades habituales de token.
- Las credenciales siguen perteneciendo a configuracion segura; no deben hardcodearse ni registrarse.

Las credenciales no deben enviarse por chat, no deben hardcodearse y no deben subirse al repositorio. La resolucion definitiva debe hacerse mediante configuracion segura: variables de entorno, user secrets, secret manager o mecanismo institucional equivalente.

## 13. SOAP y codificacion UTF-8

Durante las pruebas se detecto un punto no documentado en los contratos revisados: cuando la peticion XML contiene acentos o caracteres especiales, el servicio requiere declarar explicitamente el charset.

Header requerido:

```http
Content-Type: application/xml; charset=UTF-8
```

Sin ese charset, la respuesta podia no fallar tecnicamente, pero devolvia datos vacios. Este comportamiento fue indicado por el equipo tecnico que desarrolla el servicio GDEBA.

La implementacion SOAP real debe respetar ese header desde el inicio.

Decision posterior del feature `soap-expediente-gateway`:

- `SoapGdebaExpedienteGateway` implementa `consultarExpedienteDetallado` y `buscarHistorialPasesExpediente`.
- `GdebaExceptionMiddleware` transforma errores de integracion GDEBA en `502 Bad Gateway` con `ProblemDetails`.
- Cada invocacion SOAP registra servicio, metodo, origen, ambiente, estado HTTP, duracion y resultado mediante el modulo de control de cuotas.
- Se agrego `GET /api/gdeba/cuotas` para consultar consumos diarios por operacion.

## 14. Servicios GDEBA prioritarios

Los metodos mas importantes para la siguiente etapa son:

- `buscarDatosExpedientePorCodigosTrata`
- `buscarHistorialPasesExpediente`

`buscarDatosExpedientePorCodigosTrata` es clave para obtener expedientes por trata, pero hoy tiene una limitacion operativa importante: obliga a recuperar muchos datos y filtrar localmente. Se solicito a GDEBA evaluar filtros por reparticion/dependencia y, eventualmente, criterios adicionales que permitan reducir el volumen.

`buscarHistorialPasesExpediente` es importante para movimientos y pases. Seria especialmente util contar con filtros por fecha para trabajar de forma incremental y evitar traer historiales completos en cada invocacion.

## 15. Cache y sincronizacion

La cache local es una responsabilidad central del proxy. No se trata solo de optimizar rendimiento; tambien permite reducir llamadas a GDEBA, mejorar disponibilidad ante cortes de conectividad y dar una respuesta mas estable a los sistemas internos.

La politica de cache no debe ser unica para todos los datos. Un PDF o documento oficial puede tener una frescura distinta a un expediente en tramitacion. Los expedientes, pases y estados requieren politicas mas cuidadosas porque pueden cambiar con frecuencia.

El Worker sera importante si se decide ejecutar refrescos programados, sincronizaciones por trata, precargas o procesos incrementales. Sin Worker, la cache solo se actualizaria como consecuencia de requests HTTP, lo cual simplifica el despliegue pero limita la estrategia de actualizacion.

Decision posterior del feature `modelo-cache-persistente`:

- Separar fisicamente datos GDEBA y control de cache dentro del dominio.
- Persistir datos funcionales en entidades como `Expediente`, `MovimientoExpediente`, `DocumentoGdeba`, `DocumentoArchivoLocal`, `TipoDocumentoGdeba` y `TrataGdeba`.
- Persistir frescura/control operativo en entidades como `ExpedienteCacheControl`, `HistorialExpedienteCacheControl`, `DocumentoCacheControl` y `TrataCacheControl`.
- Mantener `MovimientoExpediente` como el dato real del historial; no duplicarlo con otra entidad de historial.
- Guardar archivos documentales fuera de la base de datos, local o externamente, dejando en SQL Server solo referencias y metadatos de archivo.
- Considerar que la mayoria de los documentos seran PDF, pero permitir otros archivos de trabajo como Word.

Respecto de documentos, se decidio distinguir `NumeroActuacionCompleto` y `NumeroEspecialCompleto`. El primero aparece en los listados del expediente, por ejemplo `RS-2023-33144875-GDEBA-DVMIYSPGP`; el segundo aparece al consultar el detalle documental, por ejemplo `RESO-2023-1743-GDEBA-DVMIYSPGP`. Son identificadores del mismo documento en contextos distintos, por lo que se guardan en la misma entidad `DocumentoGdeba`.

Tambien se agrego `TipoDocumentoGdeba` como catalogo para reglas futuras. Esto permite detectar resoluciones por configuracion y no solo por un acronimo puntual. El catalogo guarda codigo, codigo GDEBA, nombre, descripcion, familia, estado, tipo de produccion y atributos booleanos informados por GDEBA. En esta etapa no se fuerza clave foranea desde `DocumentoGdeba` hacia el catalogo, para no bloquear documentos parcialmente enriquecidos si el catalogo todavia no fue sincronizado.

Una respuesta real de `consultarTipoDocumento` para `RESO` confirmo que `acronimo=RESO` y `codigoTipoDocumentoGDEBA=RS`. Tambien confirmo los tags booleanos `esAutomatica`, `esComunicable`, `esConfidencial`, `esEmbebido`, `esEspecial`, `esFirmaConjunta`, `esFirmaExterna`, `esManual`, `esNotificable`, `tieneTemplate` y `tieneToken`.

## 16. Auditoria y trazabilidad

La auditoria debe permitir responder, como minimo:

- Que aplicacion interna consulto.
- Que operacion pidio.
- Que identificador funcional uso, por ejemplo numero de expediente.
- En que ambiente GDEBA se ejecuto.
- Si la respuesta vino de cache o de GDEBA.
- Si la operacion fue exitosa.
- Fecha y hora de resolucion.

En el estado actual la auditoria ya tiene implementacion configurable:

- `InMemory`, para logging simple.
- `Persisted`, para persistir mediante URF.

`RegistroAuditoria` debe relacionarse con `AplicacionConsumidora`; no debe quedar como un texto aislado sin relacion al consumidor registrado. En todos los modos se debe evitar registrar credenciales, tokens, XML sensible o contenido documental innecesario.

## 17. Seguridad interna

Por ahora el proxy se considera un servicio interno consumido por aplicaciones institucionales conocidas. Aun asi, conviene distinguir entre identificacion y autorizacion.

Identificacion significa saber quien llama, por ejemplo mediante `X-Application-Id`. Autorizacion significa decidir si esa aplicacion puede ejecutar determinada operacion.

El proyecto todavia no implementa autorizacion real de consumidores. Esta decision queda pendiente porque la institucion tambien evalua un servicio troncal de seguridad y roles. El proxy debe poder integrarse con esa estrategia sin duplicar innecesariamente reglas de seguridad.

## 18. Persistencia y URF

Se preve SQL Server como base local.

El equipo suele trabajar con una abstraccion sobre Entity Framework mediante Unit of Work y Repository, usando URF. La idea general es conservar ese criterio tambien en este proyecto para desacoplar dominio/aplicacion de infraestructura de persistencia.

Esto aplica a datos propios del proxy, por ejemplo cache, auditoria, aplicaciones consumidoras o configuracion persistida. No debe confundirse Repository con clientes de servicios externos: para GDEBA corresponde hablar de gateways o clients, no de repositorios.

Decision posterior del feature `modelo-cache-persistente`:

- Las entidades del dominio heredan de `DomainEntity`.
- `DomainEntity` hereda de `URF.Core.EF.Trackable.Entity` para ser compatible con repositorios trackeables de URF.
- Se evita llamar `Entity` a la clase base propia para no chocar conceptualmente ni nominalmente con URF.
- Infrastructure registra `DbContext`, `IUnitOfWork`, `IRepository<>` y `ITrackableRepository<>` cuando existe connection string `ProxyGdeba`.
- Para persistencia local se usa URF; para GDEBA se mantienen gateways/adapters.
- Existen configuraciones EF explicitas en `Infrastructure/Persistence/Configurations`. Las migraciones no reemplazan esas configuraciones; generan archivos de migracion y snapshot a partir del modelo configurado.
- Todavia queda pendiente generar/aplicar migraciones contra SQL Server.

## 19. Modelo Persistente Inicial

El modelo persistente inicial busca reproducir datos relevantes de GDEBA sin que el consumidor tenga que saber si la respuesta vino en ese momento de GDEBA o de la base local.

El identificador comun de GDEBA se modela con `NumeroGdebaCompleto`, no con nombres especificos como `NumeroExpediente` o `NumeroDocumento`. El formato completo se compone de partes: tipo, anio, numero, sistema y reparticion. El tipo (`EX`, `IF`, etc.) indica la naturaleza del elemento.

Las tratas se modelan con `TrataGdeba` como catalogo propio, porque GDEBA provee un servicio especifico para consultarlas. No corresponde duplicar sin necesidad codigo y descripcion de trata en cada ocurrencia de expediente si se puede relacionar con el catalogo.

La separacion conceptual queda asi:

- Datos GDEBA: expediente, movimientos, documentos, archivos locales, relaciones expediente-documento y tratas.
- Control de cache: fechas de deteccion, consulta, actualizacion, vencimiento, fuente y estado de frescura.
- Auditoria: registros de operaciones internas y aplicacion consumidora asociada.
- Infraestructura de persistencia: `DbContext`, configuraciones EF, URF y futura migracion SQL Server.

## 20. Criterio para agregar un nuevo metodo GDEBA

Para sumar un metodo nuevo conviene seguir este orden:

1. Definir el caso de uso en Application.
2. Definir request/response internos, sin exponer SOAP crudo.
3. Extender o crear una interfaz de gateway.
4. Implementar respuesta fake para desarrollo.
5. Agregar endpoint en Api.
6. Registrar auditoria.
7. Implementar SOAP real en Infrastructure.
8. Incorporar cache si corresponde.
9. Agregar pruebas.

Este orden permite avanzar de forma incremental sin esperar a tener resuelta toda la infraestructura externa.

## 21. Cosas a evitar

No construir XML SOAP en controladores.

No leer headers HTTP dentro de servicios de Application.

No hardcodear `HML` o `PROD` en casos de uso.

No inyectar `GdebaOptions` directamente en Application.

No exponer SOAP crudo como contrato de la API interna.

No registrar credenciales, tokens, XML sensible o documentos completos en logs.

No trasladar reglas de negocio de Obras, Licitaciones o Certificaciones al proxy.

No mezclar en una misma entidad los datos funcionales GDEBA con el control operativo de cache cuando tengan responsabilidades diferentes.

No duplicar entidades de historial y movimiento si representan el mismo hecho.

No guardar binarios documentales en la base salvo que exista una decision explicita posterior que justifique ese cambio.

## 22. Pendientes tecnicos

Quedan pendientes para siguientes iteraciones:

- Validar cliente JWT real contra HML/PROD.
- Validar clientes SOAP reales contra HML/PROD.
- Generar/aplicar migraciones EF Core sobre SQL Server.
- Implementar servicios de cache sobre el modelo persistente.
- Definir tabla o mecanismo para aplicaciones consumidoras habilitadas.
- Implementar cache con politica de frescura.
- Implementar `buscarDatosExpedientePorCodigosTrata`.
- Agregar pruebas unitarias y de integracion.
- Revisar `.gitattributes` para finales de linea.

