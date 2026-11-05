#Requires -Version 5.1
<#
.SYNOPSIS
    Builds Coffer - portable single-file EXE and Windows installer.

.DESCRIPTION
    1. Publishes a self-contained, single-file EXE  -> dist\Coffer-Portable.exe
    2. Compiles the Inno Setup script               -> dist\Coffer-Setup.exe

.PARAMETER Configuration
    Build configuration. Default: Release

.PARAMETER SkipInstaller
    Skip the Inno Setup step (e.g. if Inno Setup is not installed).

.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -SkipInstaller
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipInstaller
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectFile  = "$PSScriptRoot\Coffer\Coffer.csproj"
$DistDir      = "$PSScriptRoot\dist"
$PublishDir   = "$PSScriptRoot\publish"
$IssScript    = "$PSScriptRoot\installer\Coffer.iss"
$PortableName = "Coffer-Portable.exe"

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host ">> $msg" -ForegroundColor Cyan
}

# -- Pre-flight ----------------------------------------------------------------

Write-Step "Checking prerequisites"

if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: dotnet not found. Install .NET 8 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $ProjectFile)) {
    Write-Host "ERROR: Project file not found: $ProjectFile" -ForegroundColor Red
    exit 1
}

Write-Host "  dotnet version: $(dotnet --version)"

# -- Clean ---------------------------------------------------------------------

Write-Step "Cleaning previous output"
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
if (Test-Path $DistDir)    { Remove-Item -Recurse -Force $DistDir }
New-Item -ItemType Directory -Path $DistDir | Out-Null

# -- Restore -------------------------------------------------------------------

Write-Step "Restoring NuGet packages"
dotnet restore $ProjectFile
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# -- Publish portable single-file EXE -----------------------------------------

Write-Step "Publishing portable single-file EXE"

dotnet publish $ProjectFile `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    --output $PublishDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$builtExe = Join-Path $PublishDir "Coffer.exe"
if (-not (Test-Path $builtExe)) {
    Write-Host "ERROR: EXE not found after publish: $builtExe" -ForegroundColor Red
    exit 1
}

$portableDest = Join-Path $DistDir $PortableName
Copy-Item $builtExe $portableDest

$sizeBytes = (Get-Item $portableDest).Length
$sizeMB    = [math]::Round($sizeBytes / 1MB, 1)
Write-Host "  OK  dist\$PortableName  [$sizeMB MB]" -ForegroundColor Green

# -- Installer -----------------------------------------------------------------

if ($SkipInstaller) {
    Write-Host ""
    Write-Host "Skipping installer (-SkipInstaller flag set)." -ForegroundColor Yellow
} else {
    Write-Step "Building Windows installer with Inno Setup"

    $isccCandidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
    )

    $isccCmd = Get-Command "iscc" -ErrorAction SilentlyContinue
    if ($isccCmd) {
        $isccCandidates = @($isccCmd.Source) + $isccCandidates
    }

    $iscc = $null
    foreach ($candidate in $isccCandidates) {
        if (Test-Path $candidate) {
            $iscc = $candidate
            break
        }
    }

    if (-not $iscc) {
        Write-Host "  WARNING: Inno Setup not found." -ForegroundColor Yellow
        Write-Host "  Download: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
        Write-Host "  Then re-run without -SkipInstaller." -ForegroundColor Yellow
    } else {
        Write-Host "  Using: $iscc"
        & $iscc $IssScript "/O$DistDir"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        $installerPath = Join-Path $DistDir "Coffer-Setup.exe"
        $instMB = [math]::Round((Get-Item $installerPath).Length / 1MB, 1)
        Write-Host "  OK  dist\Coffer-Setup.exe  [$instMB MB]" -ForegroundColor Green
    }
}

# -- Summary -------------------------------------------------------------------

Write-Host ""
Write-Host "==========================================" -ForegroundColor DarkGray
Write-Host "  Build complete -- output in .\dist\"     -ForegroundColor White
Get-ChildItem $DistDir | ForEach-Object {
    $mb = [math]::Round($_.Length / 1MB, 1)
    Write-Host "    $($_.Name)  [$mb MB]" -ForegroundColor White
}
Write-Host "==========================================" -ForegroundColor DarkGray
Write-Host ""
