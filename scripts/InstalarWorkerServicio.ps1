<#
.SYNOPSIS
    Instala (o reconfigura) el Worker como servicio de Windows en el servidor.

.DESCRIPTION
    Se ejecuta como administrador sobre la carpeta ya copiada desde
    publish\Worker. Crea el origen del visor de eventos, la carpeta de logs y el
    servicio con cuenta virtual (NT SERVICE\<nombre>), inicio automatico
    retrasado y reinicio ante fallas. Si el servicio ya existe solo actualiza la
    ruta del ejecutable. Los secretos de produccion viajan en
    appsettings.Secrets.json dentro de la carpeta publicada (docs/12 §7).
    Ver docs/13-publicacion-iis.md.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RutaInstalacion,
    [string] $NombreServicio = 'ProxyGdebaDvbaWorker'
)

$ErrorActionPreference = 'Stop'

$ejecutable = Join-Path $RutaInstalacion 'ServicioSistemaWebProxyGdebaDvba.Worker.exe'
if (-not (Test-Path -LiteralPath $ejecutable)) { throw "No se encontro '$ejecutable'. Copiar antes el contenido de publish\Worker." }

# El host registra en el log Application con el nombre del ensamblado como origen.
# Una cuenta virtual no puede crear el origen, por eso se crea aca.
$origenEventos = 'ServicioSistemaWebProxyGdebaDvba.Worker'
if (-not [System.Diagnostics.EventLog]::SourceExists($origenEventos)) {
    [System.Diagnostics.EventLog]::CreateEventSource($origenEventos, 'Application')
}

# Carpeta del log en archivo (Serilog, appsettings.Production.json): la cuenta virtual del servicio necesita escribir en ella.
$carpetaLogs = Join-Path $RutaInstalacion 'logs'
New-Item -ItemType Directory -Path $carpetaLogs -Force | Out-Null
icacls $carpetaLogs /grant "NT SERVICE\${NombreServicio}:(OI)(CI)M" | Out-Null

$servicio = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue
if ($servicio) {
    if ($servicio.Status -ne 'Stopped') { Stop-Service -Name $NombreServicio }
    sc.exe config $NombreServicio binPath= "`"$ejecutable`"" | Out-Null
}
else {
    New-Service -Name $NombreServicio -BinaryPathName "`"$ejecutable`"" -DisplayName 'Proxy GDEBA DVBA - Worker' -Description 'Workers programados del proxy GDEBA (descubrimiento, detallado y enriquecimiento documental).' -StartupType Automatic | Out-Null
    sc.exe config $NombreServicio obj= "NT SERVICE\$NombreServicio" start= delayed-auto | Out-Null
    sc.exe failure $NombreServicio reset= 86400 actions= restart/60000/restart/60000/restart/300000 | Out-Null
}

if (-not (Test-Path -LiteralPath (Join-Path $RutaInstalacion 'appsettings.Secrets.json'))) {
    Write-Warning "No se encontro appsettings.Secrets.json en '$RutaInstalacion': el servicio arrancara sin credencial SQL ni credenciales GDEBA."
}

Write-Host "Servicio '$NombreServicio' configurado sobre '$ejecutable'. Iniciar con: Start-Service $NombreServicio"
