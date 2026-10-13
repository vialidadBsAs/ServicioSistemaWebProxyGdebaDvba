<#
.SYNOPSIS
    Instala (o reconfigura) el Worker como servicio de Windows en SI-INTRAVIAL.

.DESCRIPTION
    Se ejecuta como administrador sobre la carpeta ya copiada desde
    publish\Worker. Crea el origen del visor de eventos, el servicio con cuenta
    virtual (NT SERVICE\<nombre>), inicio automatico retrasado, reinicio ante
    fallas y las variables de entorno con los secretos de produccion.
    Si el servicio ya existe solo actualiza ruta y variables.
    Ver docs/13-publicacion-iis.md.

.PARAMETER ArchivoSecrets
    Copia del secrets.json de User Secrets del Worker o de la Api (tienen los
    mismos valores): credencial SQL y credenciales GDEBA.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RutaInstalacion,
    [Parameter(Mandatory)] [string] $ArchivoSecrets,
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

$secrets = Get-Content -LiteralPath $ArchivoSecrets -Raw -Encoding UTF8 | ConvertFrom-Json

# Variables de entorno propias del servicio (valor multi-string 'Environment' de su clave de registro).
$variables = @(
    "SqlLogins__LoginProd=$($secrets.'SqlLogins:LoginDev')"
    "Gdeba__Environments__PROD__Jwt__Username=$($secrets.'Gdeba:Environments:PROD:Jwt:Username')"
    "Gdeba__Environments__PROD__Jwt__Password=$($secrets.'Gdeba:Environments:PROD:Jwt:Password')"
    "Gdeba__Environments__PROD__Soap__Services__ConsultaDocumento__UsuarioConsulta=$($secrets.'Gdeba:Environments:PROD:Soap:Services:ConsultaDocumento:UsuarioConsulta')"
)
$claveRegistro = "HKLM:\SYSTEM\CurrentControlSet\Services\$NombreServicio"
Set-ItemProperty -LiteralPath $claveRegistro -Name 'Environment' -Type MultiString -Value $variables

Write-Host "Servicio '$NombreServicio' configurado sobre '$ejecutable'. Iniciar con: Start-Service $NombreServicio"
