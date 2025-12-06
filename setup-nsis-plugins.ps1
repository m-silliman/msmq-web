<#
.SYNOPSIS
    Checks and helps install required NSIS plugins for the installer.

.DESCRIPTION
    This script checks if NSIS and required plugins are installed and provides
    guidance for downloading and installing missing components.
#>

$ErrorActionPreference = "SilentlyContinue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "NSIS Plugin Setup for MSMQ Manager" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Find NSIS installation
$nsisPath = $null
$possiblePaths = @(
    "${env:ProgramFiles(x86)}\NSIS",
    "${env:ProgramFiles}\NSIS",
    "C:\Program Files (x86)\NSIS",
    "C:\Program Files\NSIS"
)

foreach ($path in $possiblePaths) {
    $makensisPath = Join-Path $path "makensis.exe"
    if (Test-Path $makensisPath) {
        $nsisPath = $path
        break
    }
}

if (-not $nsisPath) {
    Write-Host "ERROR: NSIS not found. Please install NSIS first." -ForegroundColor Red
    Write-Host "Download from: https://nsis.sourceforge.io/" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Found NSIS at: $nsisPath" -ForegroundColor Green
Write-Host ""

# Check for ServiceLib plugin
$pluginPath = Join-Path $nsisPath "Plugins\x86-unicode"
$serviceLibDll = Join-Path $pluginPath "ServiceLib.dll"
$serviceLibNsh = Join-Path $nsisPath "Include\ServiceLib.nsh"

$allPluginsFound = $true

if (-not (Test-Path $serviceLibDll)) {
    Write-Host "WARNING: ServiceLib plugin not found" -ForegroundColor Yellow
    Write-Host "Expected location: $serviceLibDll" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To install ServiceLib plugin:" -ForegroundColor Cyan
    Write-Host "1. Visit: https://nsis.sourceforge.io/ServiceLib_plug-in" -ForegroundColor White
    Write-Host "2. Download ServiceLib.zip" -ForegroundColor White
    Write-Host "3. Extract ServiceLib.dll to: $pluginPath" -ForegroundColor White
    Write-Host "4. Extract ServiceLib.nsh to: $(Join-Path $nsisPath "Include")" -ForegroundColor White
    Write-Host ""
    
    $response = Read-Host "Open download page in browser? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Start-Process "https://nsis.sourceforge.io/ServiceLib_plug-in"
    }
    $allPluginsFound = $false
} else {
    Write-Host "✓ ServiceLib.dll found" -ForegroundColor Green
}

if (-not (Test-Path $serviceLibNsh)) {
    Write-Host "WARNING: ServiceLib.nsh not found in Include directory" -ForegroundColor Yellow
    Write-Host "Expected location: $serviceLibNsh" -ForegroundColor Gray
    $allPluginsFound = $false
} else {
    Write-Host "✓ ServiceLib.nsh found" -ForegroundColor Green
}

Write-Host ""

if ($allPluginsFound) {
    Write-Host "✓ All required plugins are installed!" -ForegroundColor Green
    Write-Host "You can now run: .\build-installer.ps1" -ForegroundColor Cyan
} else {
    Write-Host "⚠ Missing required plugins" -ForegroundColor Yellow
    Write-Host "Install the missing plugins, then run build-installer.ps1" -ForegroundColor Cyan
}

Write-Host ""
Read-Host "Press Enter to continue"