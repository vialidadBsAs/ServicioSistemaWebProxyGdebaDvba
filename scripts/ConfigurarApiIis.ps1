<#
.SYNOPSIS
    Carga los secretos de produccion de la Api como variables de entorno de la
    aplicacion IIS (applicationHost.config), fuera del web.config publicado.

.DESCRIPTION
    Se ejecuta una vez en SI-INTRAVIAL, como administrador, despues de crear la
    aplicacion IIS. Las variables quedan en applicationHost.config y sobreviven a
    cada publicacion (el web.config publicado se reemplaza en cada despliegue).
    Ver docs/13-publicacion-iis.md.

.PARAMETER ArchivoClave
    Copia de appsettings.Development.json de la Api: de ahi se lee MiLLave.

.PARAMETER ArchivoSecrets
    Copia del secrets.json de User Secrets de la Api
    (%APPDATA%\Microsoft\UserSecrets\dotnet-ServicioSistemaWebProxyGdebaDvba.Api\secrets.json):
    credencial SQL y credenciales GDEBA. Son los mismos valores de desarrollo.
#>
[CmdletBinding()]
param(
    [string] $SitioIis = 'Default Web Site',
    [string] $RutaAplicacion = 'apps/Expedientes/ServicioSistemaWebProxyGdebaDvba',
    [Parameter(Mandatory)] [string] $ArchivoClave,
    [Parameter(Mandatory)] [string] $ArchivoSecrets
)

$ErrorActionPreference = 'Stop'
Import-Module IISAdministration

# ConvertFrom-Json de Windows PowerShell no admite las lineas comentadas con // que si acepta .NET.
$contenidoClave = (Get-Content -LiteralPath $ArchivoClave -Raw -Encoding UTF8) -replace '(?m)^\s*//.*$', ''
$configuracionClave = $contenidoClave | ConvertFrom-Json
$clave = $configuracionClave.ConnectionStrings.MiLLave
if ([string]::IsNullOrEmpty($clave)) { throw "El archivo '$ArchivoClave' no tiene ConnectionStrings:MiLLave." }

$secrets = Get-Content -LiteralPath $ArchivoSecrets -Raw -Encoding UTF8 | ConvertFrom-Json

$variables = [ordered]@{
    'SqlLogins__LoginProd'                                             = $secrets.'SqlLogins:LoginDev'
    'ConnectionStrings__MiLLave'                                       = $clave
    'Gdeba__Environments__PROD__Jwt__Username'                         = $secrets.'Gdeba:Environments:PROD:Jwt:Username'
    'Gdeba__Environments__PROD__Jwt__Password'                         = $secrets.'Gdeba:Environments:PROD:Jwt:Password'
    'Gdeba__Environments__PROD__Soap__Services__ConsultaDocumento__UsuarioConsulta' = $secrets.'Gdeba:Environments:PROD:Soap:Services:ConsultaDocumento:UsuarioConsulta'
}

$ubicacion = "$SitioIis/$RutaAplicacion"
Start-IISCommitDelay
$seccion = Get-IISConfigSection -SectionPath 'system.webServer/aspNetCore' -Location $ubicacion
$coleccion = Get-IISConfigCollection -ConfigElement $seccion -CollectionName 'environmentVariables'
foreach ($nombre in $variables.Keys) {
    Remove-IISConfigCollectionElement -ConfigCollection $coleccion -ConfigAttribute @{ name = $nombre } -Confirm:$false -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
    New-IISConfigCollectionElement -ConfigCollection $coleccion -ConfigAttribute @{ name = $nombre; value = $variables[$nombre] }
}
Stop-IISCommitDelay

# Carpeta del log en archivo (Serilog, appsettings.Production.json): la identidad del app pool necesita escribir en ella.
$aplicacion = (Get-IISServerManager).Sites[$SitioIis].Applications["/$RutaAplicacion"]
$carpetaLogs = Join-Path ([Environment]::ExpandEnvironmentVariables($aplicacion.VirtualDirectories['/'].PhysicalPath)) 'logs'
New-Item -ItemType Directory -Path $carpetaLogs -Force | Out-Null
icacls $carpetaLogs /grant "IIS AppPool\$($aplicacion.ApplicationPoolName):(OI)(CI)M" | Out-Null

Write-Host "Variables cargadas en '$ubicacion': $($variables.Keys -join ', ')"
Write-Host "Carpeta de logs con escritura para el app pool: $carpetaLogs"
Write-Host "Reciclar el app pool de la Api para que tome los valores."
