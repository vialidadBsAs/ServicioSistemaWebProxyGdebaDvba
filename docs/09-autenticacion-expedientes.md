# Autenticacion institucional de Expedientes

Fecha: 2026-07-29
Estado: backend implementado, validacion integrada pendiente

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

La configuracion institucional confirmada utiliza:

```text
Aplicacion: Expedientes
Rol: Admin
Usuario: usuario institucional de prueba existente
```

En `DVBA-Auth` ya se registro la aplicacion y se concedio el rol inicial. La
relacion que debe conservarse es:

1. Aplicacion `Expedientes` en `Applications`.
2. Usuario institucional autorizado.
3. Rol `Admin` activo dentro de `Expedientes`.
4. Nueva autenticacion del usuario para emitir un token actualizado.

Se debe utilizar la funcionalidad administrativa de `DVBA-Auth` cuando exista.
No se deben asumir identificadores numericos ni insertar datos sin revisar las
restricciones del modelo institucional.

## 4. Flujo objetivo

DVBA-Auth centraliza la autenticacion y la sesion institucional. Angular conoce
el endpoint del hub y realiza el login directamente. El proxy no interviene en
el intercambio de credenciales.

```text
Angular
  -> DVBA-Auth /api/Account/Login
DVBA-Auth
  -> Angular con identidad, aplicaciones, roles y token
Angular
  -> API del proxy con Authorization: Bearer <token>
Proxy
  -> valida el token y aplica las politicas de Expedientes
```

El codigo revisado de Obras confirma la segunda parte del flujo:

- Autenticacion `JwtBearer`.
- Validacion de issuer, audience, lifetime y firma.
- Uso de `ClaimTypes.Role`.
- Politica global que exige `AppAccess=Obras`.
- `ApplicationClaimsTransformation` parametrizada con el nombre de la
  aplicacion.

El contrato de entrada de `POST /api/Account/Login` fue confirmado mediante el
OpenAPI institucional: recibe `userName` y `password`. Ese contrato pertenece a
la comunicacion entre Angular y DVBA-Auth, no a la API del proxy.

## 5. Responsabilidades del proxy

Durante esta primera etapa el proxy:

- Recibe el JWT en cada peticion protegida.
- Valida issuer, audience, vigencia y firma.
- Exige acceso a `Expedientes` para los recursos generales.
- Exige ademas el rol `Admin` para los recursos administrativos.
- Aplica roles y politicas de autorizacion.
- Usa el identificador institucional estable del usuario para auditoria,
  consultas recientes y seguimientos.
- Conserva el control de cuotas, cache y Workers prioritarios.

El proxy no:

- Expone un endpoint de login.
- Recibe ni reenvia credenciales humanas.
- Administra o duplica la sesion institucional.
- Crea usuarios institucionales.
- Administra contrasenas.
- Replica tablas de ASP.NET Core Identity.
- Expone secretos de aplicacion en Angular.

## 6. Dos conceptos de aplicacion

No se deben unificar estos modelos:

### Aplicacion de seguridad

`DVBA-Auth.Applications` representa una aplicacion a la que puede acceder un
usuario humano. `Expedientes` pertenece a este catalogo.

### Aplicacion consumidora del proxy

`AplicacionConsumidora` identifica un sistema que consume el proxy y permite
aplicar auditoria, origen y control de cuotas. No representa una identidad
humana ni reemplaza el modelo de `DVBA-Auth`.

## 7. Configuracion tecnica

La implementacion debe externalizar como opciones, de acuerdo con el contrato
real de `DVBA-Auth`:

- Nombre de aplicacion: `Expedientes`.
- Issuer.
- Audience.
- Clave de validacion de firma en `ConnectionStrings:MiLLave`.
- Nombres y formato de claims.
- Politica de expiracion y renovacion.

La clave y los tokens no se registran en logs. El endpoint de login configurado
por Angular usa actualmente HTTP; antes de un uso fuera de la red institucional
debe confirmarse si existe una variante HTTPS para evitar transmitir
credenciales sin cifrado de transporte.

La configuracion del sistema Obras sirve como referencia funcional. La
compatibilidad institucional mantiene issuer y audience `Localhost` y la clave
en `ConnectionStrings:MiLLave`; esos valores deben confirmarse con un token real.

## 8. Contrato con Angular

Angular conoce DVBA-Auth para autenticarse y la API del proxy para consultar
recursos. Debe:

- Enviar las credenciales directamente a DVBA-Auth.
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
2. El token incluya acceso a `Expedientes` y el rol `Admin` para esa
   aplicacion.
3. Una llamada sin token reciba `401 Unauthorized`.
4. Un usuario autenticado sin acceso a `Expedientes` reciba
   `403 Forbidden`.
5. El usuario de prueba pueda ejecutar una consulta de expediente autorizada.
6. Un rol de otra aplicacion no otorgue permisos en `Expedientes`.
7. Ninguna credencial o token quede registrado en auditoria o logs.

## 11. Implementacion actual

El proxy implementa:

- Autenticacion `JwtBearer` para recibir el token institucional.
- Lectura de la clave mediante `ConnectionStrings:MiLLave`.
- Politica `Expedientes-Acceso`: exige token valido y
  `AppAccess=Expedientes`.
- Politica `Expedientes-Admin`: exige ademas el rol `Admin`.
- Proteccion administrativa de cuotas, consulta sin cache y descubrimiento
  manual por trata.
- `GET /api/health` anonimo para monitoreo tecnico.

No existen endpoints de login o sesion en el proxy. Antes de la prueba
integrada se deben confirmar con un token real los valores efectivos de issuer,
audience y claims.

## 12. Alta y roles de usuarios (runbook operativo)

El proxy no administra usuarios, credenciales ni roles: los consume del token
emitido por DVBA-Auth. El alta la ejecuta el **administrador de la aplicacion
DVBA-Auth** (rol institucional de esa plataforma), con estos pasos:

1. **Crear el usuario** en DVBA-Auth. La contrasena la gestiona luego el propio
   usuario desde su perfil en la aplicacion (`CambiarContraseña` directo contra
   DVBA-Auth; el proxy nunca ve credenciales).
2. **Asignar la aplicacion**: el token debe incluir el claim `AppAccess` con el
   valor `Expedientes` (el `ApplicationName` configurado). Sin este claim el
   usuario no pasa la politica de acceso base.
3. **Asignar rol segun perfil** (claim `role_Expedientes`):

   | Rol | Alcance |
   |---|---|
   | `admin` | Todo: Administracion (workers, cuotas), Temas y tratas, y lo de usuario final. |
   | `super` | Temas y tratas (consulta masiva por tratas, documentos y definicion de temas), mas lo de usuario final. |
   | (sin rol) | Usuario final: consulta por numero, busqueda por caratula, seguimiento y perfil. |

4. **En el proxy no hay nada que hacer**: el primer login crea el perfil local
   automaticamente, y la compuerta de usuario GDEBA le exige cargar su usuario
   GDEBA personal para operar consultas interactivas contra GDEBA.

Notas operativas:

- Los roles viajan dentro del JWT: tras un cambio de rol en DVBA-Auth, el
  usuario debe cerrar sesion y volver a ingresar para que el token nuevo lo
  refleje.
- Politicas vigentes en la API: `Expedientes-Acceso` (token + AppAccess),
  `Expedientes-Admin` (rol `admin`), `Expedientes-GestionTemas` (rol `super` o
  `admin`; protege la consulta masiva por tratas, la consulta de documentos,
  los valores de filtro y el CRUD de temas). La busqueda puntual por caratula
  (`GET consultas/expedientes/caratula`) es de acceso general.
