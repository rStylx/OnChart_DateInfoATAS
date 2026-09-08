param(
    [string]$Configuration = "Release",
    [string]$AtasPath = "C:\Program Files (x86)\ATAS Platform"
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "src\UsHolidays\UsHolidays.csproj"
$target = Join-Path $env:APPDATA "ATAS\Indicators"

if (-not (Test-Path $AtasPath)) {
    throw "ATAS not found at $AtasPath - pass -AtasPath with the right folder"
}

dotnet build $project -c $Configuration -p:AtasPath=$AtasPath
if ($LASTEXITCODE -ne 0) { throw "build failed" }

$dll = Join-Path $PSScriptRoot "src\UsHolidays\bin\$Configuration\UsHolidays.dll"

if (-not (Test-Path $target)) {
    New-Item -ItemType Directory -Path $target | Out-Null
}

# ATAS trzyma dll zablokowany dopoki chodzi, wiec zamknij platforme przed kopiowaniem
Copy-Item $dll $target -Force

Write-Host "copied UsHolidays.dll -> $target"
Write-Host "restart ATAS and look for 'US Holidays' in the indicator list"
