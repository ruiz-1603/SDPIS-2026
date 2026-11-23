<#
.SYNOPSIS
    Configura los user-secrets de autenticacion (login y logout) de SDPIS.Api.

.DESCRIPTION
    Guarda en user-secrets la configuracion de Autenticacion:* que usan el
    inicio y el cierre de sesion (HU-001 / HU-028):
      - Autenticacion:ClaveFirmaJwt  (64 bytes aleatorios en base64)
      - Autenticacion:Proveedor      (Local)
      - Autenticacion:Emisor         (SDPIS)
      - Autenticacion:Audiencia      (SDPIS-Web)
      - Autenticacion:MinutosExpiracion (60)

    La clave JWT se genera solo si todavia no existe, salvo que se use
    -RegenerarClaveJwt. Cada equipo tiene su propia clave: no hace falta
    compartirla. Los user-secrets quedan en el perfil del usuario de Windows,
    fuera del repositorio, y se cargan solos al iniciar la API en Development.

    No toca la configuracion de Oracle (appsettings.Development.json).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\configurar-entorno.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\configurar-entorno.ps1 -RegenerarClaveJwt
#>
[CmdletBinding()]
param(
    [switch]$RegenerarClaveJwt
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$proyecto = Join-Path $raiz 'SDPIS.Api'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'No se encontro el comando dotnet. Instale el SDK de .NET 10 y vuelva a ejecutar el script.'
}
if (-not (Test-Path (Join-Path $proyecto 'SDPIS.Api.csproj'))) {
    throw "No se encontro SDPIS.Api.csproj en $proyecto. Ejecute el script desde el repositorio."
}

function Set-Secreto([string]$clave, [string]$valor) {
    & dotnet user-secrets set $clave $valor --project $proyecto | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No fue posible guardar el user-secret $clave." }
}

Write-Host "`n== User-secrets de autenticacion (login y logout)" -ForegroundColor Cyan

$existentes = & dotnet user-secrets list --project $proyecto 2>$null
$tieneClaveJwt = [bool]($existentes | Where-Object { $_ -like 'Autenticacion:ClaveFirmaJwt = *' })

if ($tieneClaveJwt -and -not $RegenerarClaveJwt) {
    Write-Host 'Autenticacion:ClaveFirmaJwt ya existe: se conserva (use -RegenerarClaveJwt para cambiarla).'
} else {
    $bytes = New-Object byte[] 64
    $generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generador.GetBytes($bytes) } finally { $generador.Dispose() }
    Set-Secreto 'Autenticacion:ClaveFirmaJwt' ([Convert]::ToBase64String($bytes))
    Write-Host 'Autenticacion:ClaveFirmaJwt generada (64 bytes aleatorios).'
}

Set-Secreto 'Autenticacion:Proveedor' 'Local'
Set-Secreto 'Autenticacion:Emisor' 'SDPIS'
Set-Secreto 'Autenticacion:Audiencia' 'SDPIS-Web'
Set-Secreto 'Autenticacion:MinutosExpiracion' '60'

Write-Host 'User-secrets configurados:'
& dotnet user-secrets list --project $proyecto | ForEach-Object {
    if ($_ -like 'Autenticacion:ClaveFirmaJwt = *') { '  Autenticacion:ClaveFirmaJwt = (oculta)' } else { "  $_" }
}

Write-Host "`n== Listo" -ForegroundColor Cyan
Write-Host 'Para iniciar la API: dotnet run --project SDPIS.Api --launch-profile http'
