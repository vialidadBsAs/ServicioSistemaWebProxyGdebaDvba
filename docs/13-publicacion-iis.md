# Publicacion en IIS (SI-INTRAVIAL)

Fecha: 2026-10-13
Estado: escenario preparado (rama `feature/publicacion-iis` en backend y front)

## 1. Topologia

Todo corre en el servidor IIS `SI-INTRAVIAL`, por HTTP, mismo host:

| Pieza  | Donde                                   | Que es                                   |
|--------|-----------------------------------------|------------------------------------------|
| Front  | `/apps/Expedientes` | Angular estatico (`ConsultaExpedientes`) |
| Api    | `/apps/Expedientes/ServicioSistemaWebProxyGdebaDvba` | ASP.NET Core 8 en proceso (ANCM)         |
| Worker | Servicio de Windows `ProxyGdebaDvbaWorker` | Workers programados                   |
| Base   | `SI-DB-01` / `ServicioSistemaWebProxyGdebaDvba` | SQL Server                       |

- El login sigue igual: el front se autentica contra DVBA-Auth en la intranet y
  la Api valida el JWT con la misma `MiLLave`, issuer y audience de hoy.
- Front y Api comparten host, por lo que no hace falta CORS. El front llama a
  la Api con rutas relativas `ServicioSistemaWebProxyGdebaDvba/api/...`, que con
  `baseHref /apps/Expedientes/` resuelven a la aplicacion hija. En desarrollo,
  `proxy.conf.json` quita ese prefijo y deriva a `https://localhost:7120`.
- El Worker no se hospeda en IIS: IIS recicla y apaga el app pool sin
  requests, y los workers programados moririan. Corre como servicio de Windows.

## 2. Entorno y configuracion

- Api y Worker arrancan en `Production` por defecto (sin variable de entorno).
  Verificar que el servidor **no** tenga `ASPNETCORE_ENVIRONMENT` ni
  `DOTNET_ENVIRONMENT` definidas a nivel maquina.
- `appsettings.Production.json` (versionado, sin secretos): SQL `SI-DB-01`,
  `ProxyGdebaLogin=LoginProd`, GDEBA `PROD` con `GatewayMode=Soap`.
- `appsettings.Development.json` no se publica (`CopyToPublishDirectory=Never`).
- Secretos por variables de entorno (criterio de `docs/12` §7):

| Variable                                                        | Api | Worker |
|-----------------------------------------------------------------|-----|--------|
| `SqlLogins__LoginProd` (`User Id=...;Password=...`)             | si  | si     |
| `ConnectionStrings__MiLLave`                                    | si  | no     |
| `Gdeba__Environments__PROD__Jwt__Username` / `__Password`       | si  | si     |
| `Gdeba__Environments__PROD__Soap__Services__ConsultaDocumento__UsuarioConsulta` | si | si |

  - Api: variables de la aplicacion IIS en `applicationHost.config`
    (`scripts/ConfigurarApiIis.ps1`). No van en el `web.config` publicado porque
    este se reemplaza en cada despliegue.
  - Worker: variables del servicio, valor `Environment` de su clave de registro
    (`scripts/InstalarWorkerServicio.ps1`). Esa clave es legible por usuarios
    autenticados del servidor; un vault o DPAPI quedan como mejora.
  - Son los mismos valores de desarrollo: los scripts leen `MiLLave` de
    `appsettings.Development.json` (`-ArchivoClave`) y el resto del
    `secrets.json` de User Secrets (`-ArchivoSecrets`). No se tipea nada.

## 3. Preparacion del servidor (una vez)

1. Instalar el **ASP.NET Core 8 Hosting Bundle** (incluye el runtime para el
   Worker) y reiniciar IIS (`iisreset`).
2. Instalar el modulo **URL Rewrite** de IIS (lo usa el `web.config` del front).
3. Crear las carpetas fisicas, por ejemplo:
   la de `apps/Expedientes` (front), su subcarpeta
   `ServicioSistemaWebProxyGdebaDvba` (Api, convertida en aplicacion IIS) y
   `C:\Servicios\ProxyGdebaDvbaWorker`. Al actualizar el front se reemplazan
   solo sus archivos, sin vaciar la carpeta (borraria la Api).
4. App pool de la Api: `.NET CLR version = No Managed Code`, pipeline
   integrado, identidad `ApplicationPoolIdentity`. Opcional: `Start Mode =
   AlwaysRunning` e `Idle Time-out = 0` para evitar el arranque en frio.
5. `apps/Expedientes` (front) es contenido estatico: carpeta o directorio
   virtual, no necesita ser aplicacion IIS. La unica aplicacion IIS es
   `apps/Expedientes/ServicioSistemaWebProxyGdebaDvba` (Api, con ese app pool).
6. Correr `scripts/ConfigurarApiIis.ps1 -ArchivoClave <appsettings.Development.json> -ArchivoSecrets <secrets.json>`
   y borrar las copias de los dos JSON al terminar.
7. Login SQL de produccion en `SI-DB-01` con `db_datareader`, `db_datawriter`
   y `EXECUTE`, sin DDL (`docs/12` §7). Las migraciones se corren aparte, con
   un login con `db_ddladmin`.
8. Verificar salida a internet del servidor hacia `iop.gba.gob.ar` (HTTPS 443).

## 4. Publicacion

En la maquina de desarrollo:

```powershell
dotnet publish src/ServicioSistemaWebProxyGdebaDvba.Api -c Release -p:PublishProfile=SiIntravial
dotnet publish src/ServicioSistemaWebProxyGdebaDvba.Worker -c Release -p:PublishProfile=SiIntravial
```

Salida en `publish\Api` y `publish\Worker` (ignorada por git). Desde Visual
Studio, perfil `SiIntravial` de cada proyecto.

Front (`ConsultaExpedientes`): `ng build` (configuracion `production`, con
`baseHref /apps/Expedientes/`). Se copia el contenido de
`dist/consulta-expedientes/browser`, que incluye el `web.config`.

## 5. Despliegue en SI-INTRAVIAL

1. **Api**: dejar un `app_offline.htm` en la carpeta (libera los archivos
   bloqueados), copiar `publish\Api`, quitar `app_offline.htm`.
2. **Worker**: primera vez,
   `scripts/InstalarWorkerServicio.ps1 -RutaInstalacion C:\Servicios\ProxyGdebaDvbaWorker -ArchivoSecrets <secrets.json>`
   y `Start-Service ProxyGdebaDvbaWorker`. Despliegues siguientes:
   `Stop-Service`, copiar `publish\Worker`, `Start-Service`.
   Cuenta virtual `NT SERVICE\ProxyGdebaDvbaWorker`, inicio automatico
   retrasado, reinicio ante fallas.
3. **Front**: copiar `dist/consulta-expedientes/browser`.

Orden ante cambios de esquema: migracion -> Api -> Worker -> Front.

## 6. Diagnostico

- Worker: visor de eventos, log *Application*, origen
  `ServicioSistemaWebProxyGdebaDvba.Worker` (Warning en adelante, mas arranque
  y parada).
- Api: si no levanta (HTTP 500.30 / 500.31), activar temporalmente
  `stdoutLogEnabled="true"` en el `web.config` desplegado y crear la carpeta
  `logs` con escritura para `IIS AppPool\<pool>`. Volver a desactivarlo.
- La Api hoy solo registra en Console/Debug; un log persistente en produccion
  queda pendiente.

## 7. Pendientes asociados

- Script de limpieza post-restore de la base (perfiles y seguimientos de
  prueba, decision sobre auditoria y ejecuciones de workers).
- El perfil Web Deploy anterior (`IISProfile`, servidor y ruta `apps/ExpBack`
  viejos) queda sin uso.
