#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs the MSMQ Monitor as a Windows service.

.DESCRIPTION
    This script publishes the application and installs it as a Windows service.
    It must be run with Administrator privileges.

.PARAMETER ServiceName
    The name of the Windows service. Default: MSMQMonitor

.PARAMETER DisplayName
    The display name shown in Services. Default: MSMQ Monitor & Management Tool

.PARAMETER Description
    The service description. Default: Web-based MSMQ monitoring and management service

.PARAMETER Port
    The HTTP port the service will listen on. Default: 8080

.PARAMETER StartupType
    The service startup type (Automatic, Manual, Disabled). Default: Automatic

.EXAMPLE
    .\install-service.ps1

.EXAMPLE
    .\install-service.ps1 -Port 9090 -StartupType Manual
#>

param(
    [string]$ServiceName = "MSMQMonitor",
    [string]$DisplayName = "MSMQ Monitor & Management Tool",
    [string]$Description = "Web-based MSMQ monitoring and management service",
    [int]$Port = 8080,
    [ValidateSet("Automatic", "Manual", "Disabled")]
    [string]$StartupType = "Automatic"
)

$ErrorActionPreference = "Stop"

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptPath "MsMqApp"
$publishPath = Join-Path $scriptPath "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MSMQ Monitor Service Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "ERROR: Service '$ServiceName' already exists!" -ForegroundColor Red
    Write-Host "Please uninstall the existing service first using: .\uninstall-service.ps1" -ForegroundColor Yellow
    exit 1
}

# Publish the application
Write-Host "[1/4] Publishing application..." -ForegroundColor Green
try {
    if (Test-Path $publishPath) {
        Remove-Item $publishPath -Recurse -Force
    }

    dotnet publish $projectPath `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -o $publishPath `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }

    Write-Host "   Published to: $publishPath" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: Failed to publish application: $_" -ForegroundColor Red
    exit 1
}

# Update appsettings.json with configured port
Write-Host ""
Write-Host "[2/4] Configuring service settings..." -ForegroundColor Green
$appsettingsPath = Join-Path $publishPath "appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $appsettings.Service.Port = $Port
        $appsettings.Service.ServiceName = $ServiceName
        $appsettings.Service.DisplayName = $DisplayName
        $appsettings.Service.Description = $Description
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host "   Port: $Port" -ForegroundColor Gray
        Write-Host "   Service Name: $ServiceName" -ForegroundColor Gray
    }
    catch {
        Write-Host "WARNING: Could not update appsettings.json: $_" -ForegroundColor Yellow
    }
}

# Create Windows service
Write-Host ""
Write-Host "[3/4] Creating Windows service..." -ForegroundColor Green
$exePath = Join-Path $publishPath "MsMqApp.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Executable not found at: $exePath" -ForegroundColor Red
    exit 1
}

try {
    New-Service `
        -Name $ServiceName `
        -DisplayName $DisplayName `
        -Description $Description `
        -BinaryPathName $exePath `
        -StartupType $StartupType `
        -ErrorAction Stop

    Write-Host "   Service created successfully" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: Failed to create service: $_" -ForegroundColor Red
    exit 1
}

# Configure service recovery options (restart on failure)
Write-Host ""
Write-Host "[4/4] Configuring service recovery options..." -ForegroundColor Green
try {
    # Set service to restart on failure after 1 minute, reset failure count after 1 day
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    Write-Host "   Service will restart automatically on failure" -ForegroundColor Gray
}
catch {
    Write-Host "WARNING: Could not configure service recovery: $_" -ForegroundColor Yellow
}

# Display completion message
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service Details:" -ForegroundColor Cyan
Write-Host "  Name:         $ServiceName" -ForegroundColor White
Write-Host "  Display Name: $DisplayName" -ForegroundColor White
Write-Host "  Startup Type: $StartupType" -ForegroundColor White
Write-Host "  Port:         $Port" -ForegroundColor White
Write-Host "  Install Path: $publishPath" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Start the service:  Start-Service -Name $ServiceName" -ForegroundColor Yellow
Write-Host "  2. Check status:       Get-Service -Name $ServiceName" -ForegroundColor Yellow
Write-Host "  3. Access web UI:      http://localhost:$Port" -ForegroundColor Yellow
Write-Host "  4. View logs:          Event Viewer > Windows Logs > Application (Source: $ServiceName)" -ForegroundColor Yellow
Write-Host ""
Write-Host "To uninstall: .\uninstall-service.ps1" -ForegroundColor Gray
Write-Host ""
