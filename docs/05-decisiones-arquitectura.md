# Decisiones Arquitectonicas

Este documento registra decisiones iniciales. Debe actualizarse a medida que madure el proyecto.

## ADR-001: Implementar un proxy institucional independiente

Fecha: 2026-05-28

Decision:

Implementar el Servicio Sistema Web Proxy GDEBA-DVBA como servicio independiente consumido por aplicaciones internas.

Contexto:

Varios sistemas requieren acceder a GDEBA. Centralizar la integracion evita duplicar credenciales, SOAP, JWT, cache y auditoria.

Consecuencias:

- Las aplicaciones consumidoras consumen una API interna estable.
- El proxy concentra seguridad tecnica, cache, auditoria y adaptacion a GDEBA.
- Requiere gobernar claramente su alcance para no absorber logica funcional de otros sistemas.

## ADR-002: Mantener fuera del proxy la logica de negocio de sistemas consumidores

Fecha: 2026-05-28

Decision:

El proxy no decidira reglas propias de Obras, Licitaciones, Certificaciones u otros dominios.

Contexto:

El proxy es una capa de soporte transversal. Las decisiones funcionales pertenecen al backend de cada sistema consumidor.

Consecuencias:

- El proxy expone capacidades de integracion GDEBA.
- Los sistemas consumidores orquestan sus casos de uso de negocio.
- Se reduce el riesgo de convertir el proxy en un backend monolitico de multiples dominios.

## ADR-003: Usar arquitectura limpia y orientacion al dominio

Fecha: 2026-05-28

Decision:

Separar el sistema en Domain, Application, Infrastructure y API.

Contexto:

El proxy tiene reglas propias: cache, autorizacion interna, auditoria, consumo externo y normalizacion.

Consecuencias:

- Las reglas centrales quedan desacopladas de EF, SOAP, SQL Server y HTTP.
- Aumenta la mantenibilidad ante cambios de GDEBA.

## ADR-004: Usar URF, Repository y Unit of Work para SQL Server

Fecha: 2026-05-28

Decision:

Utilizar URF con Repository y Unit of Work para persistencia local en SQL Server.

Contexto:

La institucion ya utiliza este enfoque para desacoplar dominio e infraestructura.

Consecuencias:

- Mantiene consistencia con practicas existentes.
- Los repositorios representan la base local/cache, no los servicios externos.

## ADR-005: Usar Gateways/Adapters para GDEBA

Fecha: 2026-05-28

Decision:

Representar el consumo de GDEBA mediante interfaces tipo Gateway o Adapter.

Contexto:

GDEBA es un sistema externo, no la persistencia local. SOAP puede cambiar o migrar a REST.

Consecuencias:

- La capa Application depende de interfaces.
- La implementacion SOAP queda encapsulada.
- Si aparece REST, se reemplaza o agrega otra implementacion.

## ADR-006: Soportar sincronizacion actual y futura sin duplicar el sistema

Fecha: 2026-05-28

Decision:

Prever estrategias de sincronizacion reemplazables: modo actual por consulta general y modo futuro incremental si GDEBA habilita filtros.

Contexto:

Actualmente no hay filtros por reparticion o fecha suficientes. Se solicito a GDEBA evaluar mejoras.

Consecuencias:

- En fase inicial se implementa el modo compatible con servicios actuales.
- La arquitectura queda preparada para una estrategia incremental futura.
- La API interna no deberia cambiar por mejoras externas.

## ADR-007: Identificar aplicacion consumidora internamente

Fecha: 2026-05-28

Decision:

El proxy debe identificar que aplicacion interna realiza cada solicitud para auditoria y control.

Contexto:

Cada aplicacion institucional tiene su propio esquema de seguridad. El proxy necesita trazabilidad, pero no necesariamente reemplazar la autenticacion funcional de cada sistema.

Consecuencias:

- GDEBA vera al proxy como aplicacion consumidora.
- Internamente se registrara la aplicacion solicitante.
- Queda pendiente definir mecanismo tecnico: API key interna, certificado, header firmado, token interno o integracion futura con servicio troncal de seguridad.

## ADR-008: Separar datos GDEBA y control de cache

Fecha: 2026-05-31

Decision:

Separar fisicamente las entidades que reproducen datos GDEBA de las entidades que controlan frescura, fuente, vencimiento y estado operativo de cache.

Contexto:

El proxy necesita poder responder desde base local sin que el usuario consumidor dependa de saber si la consulta fue resuelta contra GDEBA o contra cache. Aun asi, los datos funcionales del expediente, sus movimientos, documentos y tratas no deben mezclarse con metadatos operativos como fechas de consulta, vencimiento o fuente de respuesta.

Consecuencias:

- `Expediente`, `MovimientoExpediente`, `DocumentoGdeba`, `DocumentoArchivoLocal` y `TrataGdeba` representan datos funcionales.
- `ExpedienteCacheControl`, `HistorialExpedienteCacheControl`, `DocumentoCacheControl` y `TrataCacheControl` representan control de cache.
- El historial no se duplica como entidad separada: el historial esta compuesto por movimientos.
- Los archivos documentales se guardan local o externamente; SQL Server conserva referencias y metadatos, no el binario.

## ADR-009: Usar NumeroGdebaCompleto como value object comun

Fecha: 2026-05-31

Decision:

Modelar el identificador compuesto de GDEBA con `NumeroGdebaCompleto`.

Contexto:

Expedientes, documentos e informes usan un formato comun de identificador. El numero numerico es solo una parte del identificador completo; tambien importan tipo, anio, sistema y reparticion.

Consecuencias:

- No se usa `NumeroExpediente` como value object general.
- No se crean identificadores separados para documento si el formato base es el mismo.
- Las entidades persisten las partes relevantes para busqueda, indices y reglas futuras.

## ADR-010: Centralizar el monitoreo operativo de expedientes seguidos

Fecha: 2026-07-28

Decision:

El proxy administrara el monitoreo operativo de los expedientes seleccionados
para seguimiento prioritario. Debe deduplicar los expedientes, planificar sus
refrescos y consumir las cuotas GDEBA de acuerdo con la prioridad institucional.
En la primera etapa, el proxy tambien podra conservar la asociacion entre el
usuario institucional y los expedientes que sigue para validar la factibilidad
de la interfaz de consulta.

Contexto:

La restriccion de invocaciones y la cantidad de expedientes impiden que cada
aplicacion o usuario programe consultas GDEBA por su cuenta. El proxy es la unica
pieza que conoce conjuntamente cache, cuotas, consultas interactivas y Workers.
Por eso debe decidir que expedientes consultar y ejecutar una sola actualizacion
aunque existan varios seguidores.

Consecuencias:

- El Worker prioritario pertenece al proxy y comparte su control de cuotas.
- Una actualizacion del expediente se ejecuta una sola vez aunque tenga varios
  seguidores.
- El cambio se detecta durante la consolidacion local, independientemente de
  quien provoco la consulta GDEBA.
- La identidad humana proviene de `DVBA-Auth`; no se duplican usuarios ni
  contrasenas en el proxy.
- Las notificaciones persistentes y SignalR quedan como evolucion posterior.
  SignalR sera un canal de entrega y no la fuente de verdad.
- Si la interfaz demuestra viabilidad y obtiene un backend especifico, la
  preferencia humana y la entrega de alertas podran trasladarse. La planificacion
  GDEBA y la deteccion generica de cambios permaneceran en el proxy.

## ADR-011: Integrar el acceso humano con DVBA-Auth

Fecha: 2026-07-28

Decision:

La primera interfaz de consulta accedera directamente al backend del proxy y
utilizara el servicio institucional `DVBA-Auth` para autenticar usuarios. La
aplicacion institucional se llama exactamente `Expedientes`. El acceso general
exige pertenecer a esa aplicacion y los recursos administrativos exigen ademas
el rol `Admin`.

Contexto:

`DVBA-Auth` utiliza ASP.NET Core Identity y agrega contexto de aplicacion a la
asignacion de roles. La relacion efectiva es usuario, rol y aplicacion mediante
`AspNetUserRoles.UserId`, `RoleId` y `AppAccessId`. El token informa las
aplicaciones habilitadas para el usuario y los roles activos dentro de cada una.
El backend de Obras valida JWT Bearer, exige el claim `AppAccess` de su
aplicacion y transforma los claims segun ese contexto.

Consecuencias:

- El proxy no incorpora tablas locales de ASP.NET Core Identity ni administra
  contrasenas.
- `Expedientes` debe existir en `DVBA-Auth.Applications` y el usuario autorizado
  debe tener el rol `Admin` activo para esa aplicacion.
- El usuario debe autenticarse nuevamente despues de la asignacion para recibir
  un token actualizado.
- El proxy validara el token institucional y aplicara politicas basadas en
  `AppAccess` y roles.
- Angular realiza el login directamente contra `DVBA-Auth`; el proxy no recibe
  credenciales ni crea una sesion paralela.
- La configuracion de issuer, audience, firma y endpoints sera externa y
  segura.
- `DVBA-Auth.Applications` no reemplaza `AplicacionConsumidora` del proxy: la
  primera controla acceso humano y la segunda identifica consumo tecnico para
  auditoria y cuotas.
- Antes de implementar se debe revisar el emisor del token y
  `ApplicationClaimsTransformation` de una aplicacion institucional existente,
  para reproducir el contrato real sin copiar configuracion obsoleta.


## ADR-012: GDEBA no accesible: corte sin reintentos fuera de agenda

Fecha: 2026-10-13

Decision:

"GDEBA no accesible" se limita a lo inequivoco: rechazo de acceso del gateway
IOP (HTTP 401/403) o ausencia de respuesta (falla de red o timeout de
transporte), definido en un unico lugar (`GdebaNoAccesibleException.EsNoAccesible`).
Esas invocaciones no consumen cuota. Ante esa condicion cada corrida de worker
se corta en la primera llamada y queda `Fallida` con el motivo; no hay
reintentos ni pausas fuera de la agenda configurada: la proxima corrida
programada vuelve a intentar en su turno.

Contexto:

Al publicar en SI-INTRAVIAL, con la IP aun no habilitada en el gateway, el
gateway devolvia 403: cada llamada contaba como consumo, la corrida abortaba con
stack trace y no quedaba claro el motivo. Los datos de
`IntegracionGdeba_Invocaciones` mostraron que los errores lentos de expedientes
antiguos llegan como HTTP 500 tras ~28-30 s: son respuestas de GDEBA y no deben
confundirse con falta de acceso (ver `11-analisis-rendimiento-worker-detallado.md`,
donde tambien se decidio no agregar timeout propio).

Se implemento y descarto una pausa compartida entre workers con reintentos
escalonados: desplazaba corridas a horarios no planificados, podia superponerlas
con la ventana de otro worker (las ventanas se separan a proposito) y sumaba
filas al historial.

Consecuencias:

- Un corte de acceso nunca mueve una corrida fuera de su horario ni de su
  ventana; el historial tiene las mismas filas que un dia normal, en `Fallida`.
- Un error devuelto por GDEBA sigue siendo falla de ese pedido: consume cuota y
  la corrida continua (el descubrimiento sigue con la siguiente consulta).
- La Api trata el 403 como sin conexion: responde desde la copia local y marca
  el sensor de conexion, sin registrar el expediente como rechazado por GDEBA.
- Las ordenes manuales se ejecutan siempre y sirven para verificar el acceso.
