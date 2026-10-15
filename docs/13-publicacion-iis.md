2026-10-13 17:50:55.291 -03:00 [INF] Microsoft.Hosting.Lifetime: Application started. Press Ctrl+C to shut down.
2026-10-13 17:50:55.324 -03:00 [INF] Microsoft.Hosting.Lifetime: Hosting environment: Production
2026-10-13 17:50:55.325 -03:00 [INF] Microsoft.Hosting.Lifetime: Content root path: C:\inetpub\wwwroot\apps\expedientes\ServicioSistemaWebProxyGdebaDvba
2026-10-13 17:50:55.381 -03:00 [WRN] Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware: Failed to determine the https port for redirect.
# Publicacion en IIS (SI-INTRAVIAL)

Fecha: 2026-10-13
Estado: instalado en SI-INTRAVIAL el 13/10/2026 (ver seccion 8). Pendiente la
habilitacion de la IP del servidor en el gateway IOP de GDEBA.

Las secciones 1 a 7 son la guia para instalar o volver a desplegar. La seccion
8 es el registro de lo que se hizo en la instalacion inicial.

## 1. Topologia

Todo corre en el servidor IIS `SI-INTRAVIAL`, por HTTP, mismo host:

| Pieza  | Donde | Que es |
|--------|-------|--------|
| Front  | `/apps/Expedientes` | Angular estatico (`ConsultaExpedientes`), carpeta sin app pool |
| Api    | `/apps/Expedientes/ServicioSistemaWebProxyGdebaDvba` | ASP.NET Core 8 en proceso (ANCM), aplicacion IIS hija |
| Worker | Servicio de Windows `ProxyGdebaDvbaWorker` | Workers programados |
| Base   | `SI-DB-01` / `ServicioSistemaWebProxyGdebaDvba` | SQL Server (la misma base que desarrollo) |

- El login sigue igual: el front se autentica contra DVBA-Auth en la intranet y
  la Api valida el JWT con la misma `MiLLave`, issuer y audience.
- Front y Api comparten host, por lo que no hace falta CORS. El front llama a
  la Api con rutas relativas `ServicioSistemaWebProxyGdebaDvba/api/...`, que con
  `baseHref /apps/Expedientes/` resuelven a la aplicacion hija.
- En desarrollo el destino de la Api lo decide el proxy de `ng serve`:
  `npm start` (`proxy.conf.json`, Api local en `https://localhost:7120`) o
  `npm run start:servidor` (`proxy.servidor.conf.json`, Api de SI-INTRAVIAL).
  El proxy es del servidor de desarrollo de Node: no existe en el front
  publicado y evita CORS porque la llamada a la Api la hace `ng serve`.
- El Worker no se hospeda en IIS: IIS recicla y apaga el app pool sin
  requests, y los workers programados moririan. Corre como servicio de Windows.
- Desarrollo y produccion comparten la base: **el Worker de desarrollo no debe
  correr mientras el del servidor este activo** (tomarian las mismas corridas y
  duplicarian consumo de cuota).

## 2. Entorno y configuracion

- Api y Worker arrancan en `Production` por defecto (sin variable de entorno).
  Verificar que el servidor **no** tenga `ASPNETCORE_ENVIRONMENT` ni
  `DOTNET_ENVIRONMENT` definidas a nivel maquina.
- `appsettings.Production.json` (versionado, sin secretos): SQL `SI-DB-01`,
  `ProxyGdebaLogin=LoginProd`, GDEBA `PROD` con `GatewayMode=Soap` y la seccion
  `Serilog` del log en archivo.
- `appsettings.Development.json` no se publica (`CopyToPublishDirectory=Never`).
- Secretos por variables de entorno (criterio de `docs/12` §7). En el servidor
  no se usan User Secrets: solo se leen en `Development`.

| Variable | Api | Worker |
|----------|-----|--------|
| `SqlLogins__LoginProd` (`User Id=...;Password=...`) | si | si |
| `ConnectionStrings__MiLLave` | si | no |
| `Gdeba__Environments__PROD__Jwt__Username` / `__Password` | si | si |
| `Gdeba__Environments__PROD__Soap__Services__ConsultaDocumento__UsuarioConsulta` | si | si |

  - Api: variables de la aplicacion IIS en
    `C:\Windows\System32\inetsrv\config\applicationHost.config`
    (`scripts/ConfigurarApiIis.ps1`). No van en el `web.config` publicado porque
    este se reemplaza en cada despliegue. Se ven en el Administrador de IIS,
    aplicacion -> Editor de configuracion -> `system.webServer/aspNetCore` ->
    `environmentVariables`.
  - Worker: variables del servicio, valor `Environment` de su clave de registro
    (`scripts/InstalarWorkerServicio.ps1`). Esa clave es legible por usuarios
    autenticados del servidor; un vault, DPAPI o el cifrado de la seccion de IIS
    quedan como mejora.
  - Son los mismos valores de desarrollo: los scripts leen `MiLLave` de
    `appsettings.Development.json` (`-ArchivoClave`, se copia tal cual) y el
    resto del `secrets.json` de User Secrets (`-ArchivoSecrets`). No se tipea nada.
    Las copias de esos archivos se borran del servidor al terminar.

## 3. Preparacion del servidor (una vez)

Todo en PowerShell **como administrador** (sin elevacion, los cmdlets de IIS
fallan con "No se encuentra ningun proveedor con el nombre 'WebAdministration'").

1. Instalar el **ASP.NET Core 8 Hosting Bundle** (columna *Run apps - Runtime*,
   instalador unico x64/x86; incluye el runtime del Worker) y correr `iisreset`.
   Verificar: `Get-WebGlobalModule -Name AspNetCoreModuleV2`.
2. Instalar **URL Rewrite 2.1** (x64; el idioma solo cambia los textos del
   instalador). Lo usa el `web.config` del front para que F5 en una ruta interna
   devuelva `index.html`. Verificar: `Get-WebGlobalModule -Name RewriteModule`.
3. Instalar **Web Deploy** (x64), instalacion personalizada con **Remote Agent
   Service**, y dejar el servicio `MsDepSvc` automatico e iniciado. Es el destino
   del perfil `IISProfile` de la Api.
4. App pool de la Api: `.NET CLR version = Sin codigo administrado`, pipeline
   integrado, identidad `ApplicationPoolIdentity`, **Cargar perfil de usuario =
   True** (sin perfil, ASP.NET Core avisa en cada arranque que las claves de
   DataProtection son efimeras), `Habilitar aplicaciones de 32 bits = False`.
5. Carpetas: la del front (`/apps/Expedientes`), su subcarpeta
   `ServicioSistemaWebProxyGdebaDvba` convertida en aplicacion IIS con ese pool
   (es la unica aplicacion IIS; el front es contenido estatico) y
   `C:\Servicios\ProxyGdebaDvbaWorker` para el Worker. Al actualizar el front se
   reemplazan solo sus archivos, sin vaciar la carpeta (borraria la Api).
6. Correr `scripts/ConfigurarApiIis.ps1 -ArchivoClave <appsettings.Development.json> -ArchivoSecrets <secrets.json>`.
   Carga las variables de la Api y crea `logs` con escritura para el app pool.
7. Login SQL de produccion en `SI-DB-01` con `db_datareader`, `db_datawriter`
   y `EXECUTE`, sin DDL (`docs/12` §7). Las migraciones se corren aparte, con
   un login con `db_ddladmin`.
8. Pedir la habilitacion de la IP del servidor en el gateway IOP de GDEBA y
   verificar la salida hacia `iop.gba.gob.ar` (HTTPS 443).

## 4. Publicacion

Api (Visual Studio): proyecto Api -> Publicar -> perfil **`IISProfile`** (Web
Deploy, Remote Agent, `http://SI-INTRAVIAL`, aplicacion
`Default Web Site/apps/Expedientes/ServicioSistemaWebProxyGdebaDvba`, `win-x64`,
usuario `administrador`). Usa app offline y no borra archivos extra del servidor
(la carpeta `logs` se conserva).

Worker (Visual Studio o CLI): perfil a carpeta **`SiIntravial`**, salida en
`publish\Worker` (ignorada por git):

```powershell
dotnet publish src/ServicioSistemaWebProxyGdebaDvba.Worker -c Release -p:PublishProfile=SiIntravial
```

La Api tambien tiene un perfil `SiIntravial` a carpeta (`publish\Api`) como
alternativa a Web Deploy.

Front (`ConsultaExpedientes`): `ng build` (configuracion `production`, con
`baseHref /apps/Expedientes/`). Se copia el contenido de
`dist/consulta-expedientes/browser`, que incluye el `web.config`.

## 5. Despliegue en SI-INTRAVIAL

1. **Api**: publicar con `IISProfile`. Alternativa sin Web Deploy: dejar un
   `app_offline.htm` en la carpeta, copiar `publish\Api` y quitarlo.
2. **Worker**: primera vez, copiar `publish\Worker` a
   `C:\Servicios\ProxyGdebaDvbaWorker` y correr
   `scripts/InstalarWorkerServicio.ps1 -RutaInstalacion C:\Servicios\ProxyGdebaDvbaWorker -ArchivoSecrets <secrets.json>`
   y `Start-Service ProxyGdebaDvbaWorker`. Despliegues siguientes:
   `Stop-Service ProxyGdebaDvbaWorker`, copiar `publish\Worker`,
   `Start-Service ProxyGdebaDvbaWorker`. Cuenta virtual
   `NT SERVICE\ProxyGdebaDvbaWorker`, inicio automatico retrasado, reinicio ante
   fallas.
3. **Front**: copiar `dist/consulta-expedientes/browser` sobre la carpeta de
   `/apps/Expedientes`.

Orden ante cambios de esquema: migracion -> Api -> Worker -> Front.

Si la politica de ejecucion bloquea los scripts:
`Set-ExecutionPolicy -Scope Process Bypass` (solo para esa consola) o
`powershell -ExecutionPolicy Bypass -File <script> ...`.

## 6. Diagnostico

- Log en archivo (Serilog), uno por dia, 30 dias de retencion:
  - Api: `<carpeta de la Api>\logs\api-AAAAMMDD.log`.
  - Worker: `C:\Servicios\ProxyGdebaDvbaWorker\logs\worker-AAAAMMDD.log`.
  - Nivel, retencion y ruta se ajustan en la seccion `Serilog` de
    `appsettings.Production.json` (basta reciclar el pool o reiniciar el
    servicio). Se excluyen el SQL de EF, el detalle HTTP de cada llamada, el
    envelope SOAP y el aviso de EF sobre `Skip/Take` sin `OrderBy`.
  - La identidad de cada proceso necesita escritura en su carpeta `logs`; los
    scripts de instalacion la otorgan. En una instalacion existente:
    `icacls "<carpeta de la Api>\logs" /grant "IIS_IUSRS:(OI)(CI)M"` (o
    `IIS AppPool\<pool>`) y
    `icacls "C:\Servicios\ProxyGdebaDvbaWorker\logs" /grant "NT SERVICE\ProxyGdebaDvbaWorker:(OI)(CI)M"`.
    El selector de usuarios de Windows suele no encontrar las identidades
    `IIS AppPool\...`; `icacls` si.
- Latido del Worker: al arrancar y todos los dias a `Latido:HoraLocal`
  (`appsettings.json`, por defecto 08:00) escribe una linea por worker con su
  estado (habilitado o no), su agenda, las ejecuciones por estado desde el latido
  anterior (al arrancar, las ultimas 24 h) y la ultima ejecucion. Un dia sin
  latido indica que el servicio no estuvo activo; un "Application started" sin
  "Application is shutting down" previo indica una caida (el detalle queda en el
  visor de eventos, *Sistema*, origen *Service Control Manager*; el servicio se
  reinicia solo).
- Worker: ademas, visor de eventos, log *Application*, origen
  `ServicioSistemaWebProxyGdebaDvba.Worker` (Warning en adelante, mas arranque
  y parada).
- Api: si no levanta (HTTP 500.30 / 500.31), el error es previo a Serilog:
  activar temporalmente `stdoutLogEnabled="true"` en el `web.config`
  desplegado. Volver a desactivarlo.
- Advertencia esperada en cada arranque de la Api:
  `HttpsRedirectionMiddleware: Failed to determine the https port`. El sitio es
  solo HTTP; se mantiene `UseHttpsRedirection` para cuando haya certificado.
- GDEBA no accesible (por ejemplo IP no habilitada): la corrida queda `Fallida`
  con el motivo y no consume cuota (ver `docs/04` y ADR-012).

## 7. Pendientes asociados

- Habilitacion de la IP de SI-INTRAVIAL en el gateway IOP; despues, verificar
  con una orden manual y reactivar las corridas programadas.
- Login SQL propio de produccion (hoy se usa la credencial de desarrollo
  `dev_proxygdeba`, que tiene DDL).
- Script de limpieza post-restore de la base (perfiles y seguimientos de
  prueba, decision sobre auditoria y ejecuciones de workers).

## 8. Registro de la instalacion inicial (13/10/2026)

### 8.1 Servidor

| Paso | Detalle | Verificacion |
|------|---------|--------------|
| Hosting Bundle | ASP.NET Core 8.0.31 (instalador unico x64/x86) + `iisreset` | `Get-WebGlobalModule -Name AspNetCoreModuleV2` devuelve `%ProgramFiles%\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll` |
| URL Rewrite | 2.1 x64 | `Get-WebGlobalModule -Name RewriteModule` devuelve `%SystemRoot%\system32\inetsrv\rewrite.dll` |
| Web Deploy | 10.0.2001 x64, personalizada con Remote Agent Service | Publicacion de la Api con `IISProfile` exitosa |
| Front | Carpeta `C:\inetpub\wwwroot\apps\expedientes` (`/apps/Expedientes`), sin app pool | Login y consultas desde `http://SI-INTRAVIAL/apps/Expedientes/` |
| Api | Subcarpeta `ServicioSistemaWebProxyGdebaDvba` convertida en aplicacion; pool `ServicioProxyGdeba`, sin codigo administrado, Cargar perfil de usuario = True | Log de arranque: `Hosting environment: Production`, content root `C:\inetpub\wwwroot\apps\expedientes\ServicioSistemaWebProxyGdebaDvba` |
| Variables de la Api | `ConfigurarApiIis.ps1` desde `C:\temp`, con `-ExecutionPolicy Bypass` | Variables visibles en el Editor de configuracion. Borrar de `C:\temp` las copias de `appsettings.Development.json` y `secrets.json` |
| Worker | `C:\Servicios\ProxyGdebaDvbaWorker` (el servidor no tiene disco `D:`), `InstalarWorkerServicio.ps1` | Servicio `ProxyGdebaDvbaWorker` en ejecucion; log `Application started. Hosting environment: Production; Content root path: C:\Servicios\ProxyGdebaDvbaWorker` |
| Logs | Api: `logs` con Modificar para `IIS_IUSRS` (decision de la instalacion; el pool es miembro del grupo). Worker: `logs` con Modificar para `NT SERVICE\ProxyGdebaDvbaWorker` | `api-AAAAMMDD.log` y `worker-AAAAMMDD.log` generados |
| Base | `SI-DB-01`, misma base de desarrollo; `LoginProd` con la credencial de desarrollo | Corridas registradas en la base de produccion |

La "Conexion de prueba" de la aplicacion en el Administrador de IIS muestra una
advertencia en *Autorizacion* ("No se puede verificar el acceso a la ruta");
es normal con autenticacion de paso a traves y no indica un problema.

### 8.2 Prueba contra GDEBA

- El token JWT de GDEBA se obtiene (HTTP 200): las credenciales cargadas son
  correctas y el endpoint JWT acepta pedidos desde el servidor.
- Las llamadas SOAP reciben **HTTP 403 en ~36 ms**: el gateway IOP rechaza la IP
  de SI-INTRAVIAL, todavia no habilitada.
- Orden manual de descubrimiento: queda `Fallida` con "GDEBA no accesible desde
  este servidor: el gateway rechazo el pedido con HTTP 403", sin stack trace. La
  invocacion se registra sin consumo de cuota (confirmar en Cuotas GDEBA).
- Las corridas programadas quedaron deshabilitadas hasta la habilitacion de la
  IP. El Worker de desarrollo se mantiene detenido.
- El front de desarrollo se probo contra la Api del servidor con
  `npm run start:servidor`.

### 8.3 Problemas encontrados y solucion

| Problema | Causa | Solucion |
|----------|-------|----------|
| Rutas `/api/...` del front iban a la raiz del servidor | Rutas absolutas; en desarrollo las resolvia el proxy de `ng serve` | Rutas relativas `ServicioSistemaWebProxyGdebaDvba/api/...` + `baseHref`; los interceptores reconocen ese prefijo |
| `Get-WebGlobalModule`: "No se encuentra ningun proveedor 'WebAdministration'" | Consola sin elevacion | PowerShell como administrador |
| `ConvertFrom-Json`: "Se esperaba ':' o '}'" | Windows PowerShell 5.1 no admite la linea comentada con `//` de `appsettings.Development.json` | El script descarta las lineas comentadas antes de parsear |
| El script pedia credenciales por teclado | Eran los mismos valores de desarrollo | El script los lee del `secrets.json` de User Secrets (`-ArchivoSecrets`) |
| "El elemento de coleccion de configuracion no existe" (x5) | Primera carga: el script borra cada variable antes de crearla | Inofensivo; se silencio en el script |
| Parametro `ArchivoSecrets` no encontrado | Quedaba la version anterior del script en `C:\temp` | Volver a copiar el script |
| Perfil `IISProfile` apuntaba al servidor viejo (`10.25.97.36`, `apps/ExpBack`) | Perfil heredado | Actualizado a SI-INTRAVIAL, ruta nueva y `win-x64` (Visual Studio mostraba "Portable") |
| Publicacion Web Deploy imposible | Faltaba Web Deploy en el servidor | Instalado con Remote Agent Service |
| El perfil `SiIntravial` del Worker no aparecia en Visual Studio | Formato de perfil web en un proyecto Worker | Perfil con `PublishProtocol=FileSystem` y `_TargetId=Folder`; recargar el proyecto |
| `Join-Path`: "No existe ninguna unidad con el nombre 'D'" | Ruta de ejemplo con `D:` | `C:\Servicios\ProxyGdebaDvbaWorker` |
| 403 consumia cuota y la corrida abortaba con stack trace | El 403 contaba como "el servidor respondio" | Rama `feature/worker-errores-gdeba` (ADR-012) |
| Advertencias de DataProtection en cada arranque de la Api | Pool sin perfil de usuario | Cargar perfil de usuario = True |
| Log del Worker lleno de lineas HTTP y avisos de EF | Niveles por defecto | Overrides de `Serilog` en `appsettings.Production.json` |
