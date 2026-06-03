# Pubblica TaxManager per Windows x64: eseguibile self-contained single-file + dati,
# impacchettati in uno ZIP pronto alla distribuzione.

param(
    [switch]$SkipClean = $false
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "src\TaxManager.App\TaxManager.App.csproj"
$outputDir   = Join-Path $PSScriptRoot "publish"
$releaseDir  = Join-Path $PSScriptRoot "release"
$zipPath     = Join-Path $releaseDir "TaxManager-win-x64.zip"

Write-Host "==== TaxManager - Build & Publish ====" -ForegroundColor Cyan

if (-not $SkipClean) {
    foreach ($d in @($outputDir, $releaseDir)) {
        if (Test-Path $d) { Remove-Item $d -Recurse -Force }
    }
}
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

Write-Host "Step 1: publish self-contained win-x64..." -ForegroundColor Cyan
& dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir
if ($LASTEXITCODE -ne 0) { throw "Publish fallito." }

$exePath = Join-Path $outputDir "TaxManager.exe"
if (-not (Test-Path $exePath)) { throw "Eseguibile non trovato: $exePath" }

Write-Host "Step 2: creazione ZIP (exe + rulesets/ + addizionali/)..." -ForegroundColor Cyan
# Il publish include gia' le cartelle dati (rulesets/, addizionali/) accanto all'exe.
Compress-Archive -Path (Join-Path $outputDir '*') -DestinationPath $zipPath -Force

$exeMb = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
$zipMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host ""
Write-Host "Build completato!" -ForegroundColor Green
Write-Host "  Eseguibile: $exePath ($exeMb MB)" -ForegroundColor Green
Write-Host "  Pacchetto:  $zipPath ($zipMb MB)" -ForegroundColor Green
