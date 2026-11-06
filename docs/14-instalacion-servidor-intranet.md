# Instalacion en el servidor de intranet

Fecha: 2026-11-06
Servidor: `intranet.vialidad.gba.gov.ar` (10.25.96.60), Windows Server 2012 R2, IIS 8.5.
Es el mismo servidor que hospeda DVBA-Auth. No tiene escritorio remoto habilitado:
toda la administracion se hace por PowerShell remoto desde la maquina de desarrollo.

Este documento es la guia practica de lo que se instalo el 6/11/2026 y de como
volver a desplegar. La referencia completa sigue siendo `docs/13-publicacion-iis.md`;
los secretos se explican en `docs/12-credenciales-sql-y-secrets.md`.

## 1. Que queda instalado y donde

| Pieza | Donde | Como corre |
|-------|-------|------------|
| Front | `C:\inetpub\wwwroot\apps\expedientes` → `http://intranet.vialidad.gba.gov.ar/apps/expedientes/` | Angular estatico, carpeta sin app pool |
| Api | Subcarpeta `ServicioSistemaWebProxyGdebaDvba` de la anterior, aplicacion IIS con pool `ServicioProxyGdeba` | ASP.NET Core 8 en proceso (ANCM) |
| Worker | `C:\Servicios\ProxyGdebaDvbaWorker` | Servicio de Windows `ProxyGdebaDvbaWorker`, cuenta `NT SERVICE\ProxyGdebaDvbaWorker` |
| Base | `SI-DB-01` / `ServicioSistemaWebProxyGdebaDvba` | La misma base que desarrollo |

El front llama a la Api con rutas relativas `ServicioSistemaWebProxyGdebaDvba/api/...`
que, con `baseHref /apps/Expedientes/`, resuelven a la aplicacion hija. Por eso el
front **tiene** que estar en `/apps/expedientes`, no en otra carpeta: en
`/apps/front/Expedientes` las llamadas a la Api daban 404.

## 2. Requisitos del servidor (una sola vez)

- ASP.NET Core 8 Hosting Bundle. Verificar: `Get-WebGlobalModule -Name AspNetCoreModuleV2`.
- URL Rewrite 2.1 (lo usa el `web.config` del front para que F5 en una ruta
  interna devuelva `index.html`). Verificar: `Get-WebGlobalModule -Name RewriteModule`.
- Web Deploy con Remote Agent Service, solo si se publica la Api con el perfil
  `Intranet` de Visual Studio. Si no esta, se publica a carpeta y se copia por `C$`.
- Login SQL con acceso a `SI-DB-01` desde la IP del servidor.
- IP del servidor habilitada en el gateway IOP de GDEBA (pendiente al 6/11).

## 3. Acceso por PowerShell remoto (sin escritorio remoto)

Todo desde una PowerShell **como administrador** en la maquina de desarrollo.

Agregar el servidor a los hosts de confianza, una sola vez. La lista ya tenia a
`SI-DB-01` y `SI-INTRAVIAL`; `-Concatenate` agrega sin pisar:

```powershell
Set-Item WSMan:\localhost\Client\TrustedHosts -Value intranet.vialidad.gba.gov.ar -Concatenate -Force
```

Abrir la sesion. Usuario con dominio, `VIALIDAD\administrador`:

```powershell
Enter-PSSession -ComputerName intranet.vialidad.gba.gov.ar -Credential (Get-Credential)
```

Dentro de la sesion el prompt cambia a `[intranet.vialidad.gba.gov.ar]: PS ...`.
Se sale con `Exit-PSSession`.

Los archivos se suben desde una PowerShell local con `Copy-Item` al recurso
administrativo `\\intranet.vialidad.gba.gov.ar\C$`. Que se copia y adonde:

| Que | Desde (maquina de desarrollo) | Hacia (servidor) | Comando en |
|-----|-------------------------------|------------------|------------|
| Front | `ConsultaExpedientes\dist\consulta-expedientes\browser\*` | `C:\inetpub\wwwroot\apps\expedientes\` | seccion 5 |
| Api | No se copia: se publica con el perfil `Intranet` (Web Deploy). Sin Web Deploy, `publish\Api\*` | `C:\inetpub\wwwroot\apps\expedientes\ServicioSistemaWebProxyGdebaDvba\` | seccion 6.2 |
| Worker | `publish\Worker\*` | `C:\Servicios\ProxyGdebaDvbaWorker\` | seccion 7 |
| Script del Worker | `scripts\InstalarWorkerServicio.ps1` | `C:\servicios\` | seccion 7 |

Los ejecutables nativos (`icacls.exe`, `sc.exe`) fallan en la sesion remota con
"El nombre del directorio no es valido" porque la sesion no tiene carpeta temporal.
Antes de correr un script que los use, fijar:

```powershell
$env:TEMP = 'C:\Windows\Temp'; $env:TMP = $env:TEMP; Set-Location C:\
```

## 4. Secretos: `appsettings.Secrets.json`

Las credenciales (SQL, `MiLLave`, GDEBA) no se configuran en el servidor. Viven en
un archivo `appsettings.Secrets.json` junto al `appsettings.json` de la Api y del
Worker, **en la maquina que publica**. Git lo ignora. Si existe, se publica con la
aplicacion y se carga al final de la configuracion. Si falta, la aplicacion arranca
sin credenciales y falla al tocar la base.

| Clave | Api | Worker |
|-------|-----|--------|
| `ConnectionStrings:MiLLave` | si | no |
| `SqlLogins:LoginProd` (`User Id=...;Password=...`) | si | si |
| `Gdeba:Environments:PROD:Jwt:Username` / `:Password` | si | si |
| `Gdeba:Environments:PROD:Soap:Services:ConsultaDocumento:UsuarioConsulta` | si | si |

Son los mismos valores de desarrollo (`MiLLave` de `appsettings.Development.json`,
el resto de User Secrets). Se armaron el 6/11 y se conservan. No hay que copiar
nada aparte ni correr scripts de variables de entorno.

## 5. Front

En `ConsultaExpedientes`:

```powershell
npm run build
```

Compila en configuracion `production` (ya no existe `--prod`). Salida en
`dist\consulta-expedientes\browser`, que incluye el `web.config` del SPA.

Copiar **sobre** la carpeta del servidor, sin vaciarla (adentro esta la Api):

```powershell
Copy-Item .\dist\consulta-expedientes\browser\* -Destination \\intranet.vialidad.gba.gov.ar\C$\inetpub\wwwroot\apps\expedientes\ -Recurse -Force
```

Si el build falla con "bundle initial exceeded maximum budget", el limite esta en
`angular.json` (`budgets`); el 6/11 se subio a 1,5 MB / 2 MB.

## 6. Api

### 6.1 Crear la aplicacion IIS (una sola vez)

Publicar solo copia archivos: si la subcarpeta no es una aplicacion IIS, toda
llamada responde 500 sin cuerpo. En la sesion remota:

```powershell
New-WebAppPool -Name ServicioProxyGdeba
Set-ItemProperty IIS:\AppPools\ServicioProxyGdeba -Name managedRuntimeVersion -Value ''
Set-ItemProperty IIS:\AppPools\ServicioProxyGdeba -Name processModel.loadUserProfile -Value $true
New-WebApplication -Site 'Default Web Site' -Name 'apps/expedientes/ServicioSistemaWebProxyGdebaDvba' -PhysicalPath 'C:\inetpub\wwwroot\apps\expedientes\ServicioSistemaWebProxyGdebaDvba' -ApplicationPool ServicioProxyGdeba
```

Verificar con `Get-WebApplication`: tiene que listar la aplicacion con su pool.

### 6.2 Publicar

Visual Studio: proyecto Api → Publicar → perfil **`Intranet`** (Web Deploy, host
`intranet.vialidad.gba.gov.ar`, aplicacion `apps/expedientes/...`, usuario
`VIALIDAD\administrador`; la contraseña la pide y la guarda en el `.user`).
Usa app offline y no borra archivos extra (conserva `logs`).

Alternativa sin Web Deploy: publicar a carpeta (`publish\Api`) y copiar por `C$`.
La Api corre en proceso y bloquea sus dll, asi que en la sesion remota:
`Stop-WebAppPool ServicioProxyGdeba`, copiar, `Start-WebAppPool ServicioProxyGdeba`.

### 6.3 Carpeta de logs (una sola vez)

La identidad del pool necesita escribir en `logs`. Con `icacls` (ver la nota de
`$env:TEMP` en la seccion 3) o, sin ejecutables nativos:

```powershell
$carpeta = 'C:\inetpub\wwwroot\apps\expedientes\ServicioSistemaWebProxyGdebaDvba\logs'
New-Item -ItemType Directory -Force $carpeta | Out-Null
$acl = Get-Acl $carpeta
$acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule('IIS AppPool\ServicioProxyGdeba', 'Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
Set-Acl $carpeta $acl
Restart-WebAppPool ServicioProxyGdeba
```

### 6.4 Verificar

En el navegador, sin login:

`http://intranet.vialidad.gba.gov.ar/apps/expedientes/ServicioSistemaWebProxyGdebaDvba/api/gdeba/estado`

Responde `{"accesible": ...}`. Si da 500, el detalle esta en
`<carpeta de la Api>\logs\api-AAAAMMDD.log`.

## 7. Worker

Publicar a carpeta desde la raiz del repo backend (`publish\Worker`, ignorada por git):

```powershell
dotnet publish src/ServicioSistemaWebProxyGdebaDvba.Worker -c Release -p:PublishProfile=SiIntravial
```

Copiar al servidor y copiar tambien el script de instalacion fuera de `wwwroot`:

```powershell
New-Item -ItemType Directory -Force \\intranet.vialidad.gba.gov.ar\C$\Servicios\ProxyGdebaDvbaWorker
Copy-Item .\publish\Worker\* -Destination \\intranet.vialidad.gba.gov.ar\C$\Servicios\ProxyGdebaDvbaWorker\ -Recurse -Force
Copy-Item .\scripts\InstalarWorkerServicio.ps1 -Destination \\intranet.vialidad.gba.gov.ar\C$\servicios\ -Force
```

Primera vez, en la sesion remota (con `$env:TEMP` fijado, seccion 3). El script
crea el origen del visor de eventos, el servicio con cuenta virtual, inicio
automatico retrasado, reinicio ante fallas y los permisos de `logs`; avisa si
falta `appsettings.Secrets.json`:

```powershell
C:\servicios\InstalarWorkerServicio.ps1 -RutaInstalacion C:\Servicios\ProxyGdebaDvbaWorker
Start-Service ProxyGdebaDvbaWorker
```

Despliegues siguientes: `Stop-Service ProxyGdebaDvbaWorker`, copiar `publish\Worker`,
`Start-Service ProxyGdebaDvbaWorker`.

Verificar:

```powershell
Get-Service ProxyGdebaDvbaWorker
Get-Content C:\Servicios\ProxyGdebaDvbaWorker\logs\worker-AAAAMMDD.log -Tail 20
```

Tiene que decir `Hosting environment: Production` y el latido con el estado de cada worker.

**Un solo Worker activo.** Desarrollo, SI-INTRAVIAL e intranet comparten la base: si
dos workers corren, toman las mismas corridas y duplican el consumo de cuota. El de
SI-INTRAVIAL debe quedar parado y deshabilitado (`Stop-Service` y
`Set-Service -StartupType Disabled`), y el Worker local no debe correr mientras el de
intranet este activo.

## 8. Pendientes al 6/11/2026

- Habilitacion de la IP de intranet en el gateway IOP de GDEBA.
- Borrar del servidor las copias de `secrets.json` y `appsettings.Development.json`
  que quedaron en `C:\desarrollo-instaladores\desarrollo-soft\expedientesviales\script`.
- Confirmar que el Worker de SI-INTRAVIAL quedo parado y deshabilitado.
- Login SQL propio de produccion, sin DDL (hoy se usa la credencial de desarrollo).
