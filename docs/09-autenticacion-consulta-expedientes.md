# Autenticacion de ConsultaExpedientes

Fecha: 2026-07-28
Estado: decision documentada, implementacion pendiente

## 1. Proposito

Este documento define el criterio para incorporar validacion de identidad y
autorizacion humana al backend del proxy durante la primera etapa de la interfaz
de consulta de expedientes.

La decision es deliberadamente incremental. Angular consumira directamente la
API del proxy para validar la factibilidad funcional y la recepcion de los
usuarios. No se creara inicialmente un backend especifico para la interfaz.

## 2. Sistema institucional de identidad

La identidad pertenece a `DVBA-Auth`, una aplicacion .NET basada en ASP.NET Core
Identity y Entity Framework. El modelo observado contiene las tablas Identity
habituales y dos conceptos institucionales relevantes:

- `Applications`: catalogo de aplicaciones a las que puede acceder un usuario.
- `AspNetUserRoles.AppAccessId`: contexto de aplicacion para la asignacion de un
  rol a un usuario.

La relacion efectiva de autorizacion es:

```text
Usuario + Rol + Aplicacion
```

Los roles son genericos y pueden reutilizarse. Su activacion para una persona
queda acotada a una aplicacion concreta. El token informa las aplicaciones a
las que accede el usuario y los roles activos dentro de cada aplicacion.

## 3. Configuracion inicial

La prueba inicial utilizara:

```text
Aplicacion: ConsultaExpedientes
Rol: consulta
Usuario: usuario institucional de prueba existente
```

En `DVBA-Auth` se debe:

1. Crear `ConsultaExpedientes` en `Applications` si todavia no existe.
2. Obtener el identificador real de la aplicacion creada.
3. Obtener el `UserId` del usuario de prueba.
4. Obtener el `RoleId` del rol existente `consulta`.
5. Registrar la asociacion usuario, rol y aplicacion sin duplicarla.
6. Volver a autenticar al usuario para emitir un token actualizado.

Se debe utilizar la funcionalidad administrativa de `DVBA-Auth` cuando exista.
No se deben asumir identificadores numericos ni insertar datos sin revisar las
restricciones del modelo institucional.

## 4. Flujo objetivo

El backend de la aplicacion es quien integra el servicio institucional. En esta
primera etapa ese backend es el proxy.

```text
Angular
  -> endpoint de autenticacion del proxy
Proxy
  -> DVBA-Auth mediante el contrato institucional
DVBA-Auth
  -> identidad, aplicaciones y roles autorizados
Proxy
  -> token o respuesta de sesion prevista por el contrato
Angular
  -> API del proxy con Authorization: Bearer <token>
Proxy
  -> valida el token y aplica las politicas de ConsultaExpedientes
```

El codigo revisado de Obras confirma la segunda parte del flujo:

- Autenticacion `JwtBearer`.
- Validacion de issuer, audience, lifetime y firma.
- Uso de `ClaimTypes.Role`.
- Politica global que exige `AppAccess=Obras`.
- `ApplicationClaimsTransformation` parametrizada con el nombre de la
  aplicacion.

Todavia no se reviso el codigo que emite el token ni la implementacion de
`ApplicationClaimsTransformation`. Esos archivos son requisitos de analisis
antes de implementar la integracion.

## 5. Responsabilidades del proxy

Durante esta primera etapa el proxy:

- Expone el punto de entrada utilizado por Angular para autenticarse.
- Delega la validacion inicial al servicio institucional.
- Valida el JWT en las peticiones posteriores.
- Exige acceso a `ConsultaExpedientes`.
- Aplica roles y politicas de autorizacion.
- Usa el identificador institucional estable del usuario para auditoria,
  consultas recientes y seguimientos.
- Conserva el control de cuotas, cache y Workers prioritarios.

El proxy no:

- Crea usuarios institucionales.
- Administra contrasenas.
- Replica tablas de ASP.NET Core Identity.
- Expone secretos de aplicacion en Angular.

## 6. Dos conceptos de aplicacion

No se deben unificar estos modelos:

### Aplicacion de seguridad

`DVBA-Auth.Applications` representa una aplicacion a la que puede acceder un
usuario humano. `ConsultaExpedientes` pertenece a este catalogo.

### Aplicacion consumidora del proxy

`AplicacionConsumidora` identifica un sistema que consume el proxy y permite
aplicar auditoria, origen y control de cuotas. No representa una identidad
humana ni reemplaza el modelo de `DVBA-Auth`.

## 7. Configuracion tecnica pendiente

La implementacion debe externalizar como opciones, de acuerdo con el contrato
real de `DVBA-Auth`:

- Nombre de aplicacion: `ConsultaExpedientes`.
- Endpoint institucional de autenticacion.
- Issuer.
- Audience.
- Material de validacion de firma.
- Nombres y formato de claims.
- Politica de expiracion y renovacion.

Endpoints, claves, tokens y credenciales deben permanecer en user secrets,
variables de entorno o el mecanismo institucional correspondiente. No se
hardcodean ni se registran en logs.

La configuracion del sistema Obras sirve como referencia funcional, no como
codigo para copiar literalmente. En particular, el nuevo codigo no debe copiar
valores `Localhost`, CORS abierto ni claves ubicadas semanticamente como cadenas
de conexion.

## 8. Contrato con Angular

Angular conocera solamente la API del proxy. Debe:

- Enviar las credenciales por el flujo institucional que se defina.
- Conservar el token con el mecanismo seguro acordado.
- Enviar `Authorization: Bearer <token>` en las llamadas protegidas.
- Resolver navegacion y visibilidad de opciones segun claims, sin reemplazar la
  autorizacion obligatoria del backend.
- Tratar respuestas `401` como falta o vencimiento de autenticacion.
- Tratar respuestas `403` como identidad valida sin permiso suficiente.

La interfaz no debe decidir por si sola si un usuario puede consultar,
refrescar, administrar cuotas o configurar Workers.

## 9. Seguimientos y monitoreo prioritario

En la primera etapa el proxy puede guardar la relacion entre el identificador
institucional del usuario y los expedientes seguidos. Esta decision permite
probar la funcionalidad sin agregar un tercer backend.

El monitoreo operativo permanece bajo responsabilidad del proxy porque este
administra cuotas, cache y planificacion de Workers. Varios seguidores del mismo
expediente no deben producir varias consultas GDEBA.

Si la interfaz obtiene posteriormente un backend especifico, las preferencias
humanas, recientes y entrega de notificaciones pueden trasladarse. El proxy
seguira siendo responsable de planificar consultas GDEBA y detectar cambios.

## 10. Criterios de aceptacion de seguridad

La primera integracion se considerara valida cuando:

1. El usuario de prueba pueda autenticarse mediante `DVBA-Auth`.
2. El token incluya acceso a `ConsultaExpedientes` y el rol `consulta` para esa
   aplicacion.
3. Una llamada sin token reciba `401 Unauthorized`.
4. Un usuario autenticado sin acceso a `ConsultaExpedientes` reciba
   `403 Forbidden`.
5. El usuario de prueba pueda ejecutar una consulta de expediente autorizada.
6. Un rol de otra aplicacion no otorgue permisos en `ConsultaExpedientes`.
7. Ninguna credencial o token quede registrado en auditoria o logs.

## 11. Trabajo previo a la implementacion

Antes de modificar el pipeline de seguridad del proxy se debe obtener y revisar:

- `ApplicationClaimsTransformation` de Obras.
- Controller o servicio de autenticacion utilizado por Obras.
- Codigo de emision del JWT en `DVBA-Auth`.
- DTOs de autenticacion y estructura real de claims.
- Configuracion sin secretos de issuer, audience y endpoint.
- Flujo Angular existente de login, interceptor y guards.

Esta revision determinara el contrato concreto. No se debe completar esa
informacion por inferencia.
