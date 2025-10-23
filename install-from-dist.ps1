#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs MSMQ Monitor from a standalone distribution package as a Windows service.

.DESCRIPTION
    This script installs the pre-built standalone executable as a Windows service.
    Use this script when you have already built the standalone distribution using build-standalone.ps1
    It must be run with Administrator privileges.

.PARAMETER DistPath
    Path to the standalone distribution folder. Default: .\dist

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

.PARAMETER InstallPath
    Where to install the service files. Default: C:\Program Files\MSMQMonitor

.EXAMPLE
    .\install-from-dist.ps1

.EXAMPLE
    .\install-from-dist.ps1 -DistPath ".\dist" -Port 9090

.EXAMPLE
    .\install-from-dist.ps1 -InstallPath "C:\Services\MSMQMonitor"
#>

param(
    [string]$DistPath = ".\dist",
    [string]$ServiceName = "MSMQMonitor",
    [string]$DisplayName = "MSMQ Monitor & Management Tool",
    [string]$Description = "Web-based MSMQ monitoring and management service",
    [int]$Port = 8080,
    [ValidateSet("Automatic", "Manual", "Disabled")]
    [string]$StartupType = "Automatic",
    [string]$InstallPath = "C:\Program Files\MSMQMonitor"
)

$ErrorActionPreference = "Stop"

# Get absolute paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$distFullPath = Join-Path $scriptPath $DistPath
$distFullPath = [System.IO.Path]::GetFullPath($distFullPath)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MSMQ Monitor Service Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify distribution exists
if (-not (Test-Path $distFullPath)) {
    Write-Host "ERROR: Distribution folder not found: $distFullPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the standalone distribution first:" -ForegroundColor Yellow
    Write-Host "  .\build-standalone.ps1" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

$exePath = Join-Path $distFullPath "MsMqApp.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: MsMqApp.exe not found in: $distFullPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the standalone distribution first:" -ForegroundColor Yellow
    Write-Host "  .\build-standalone.ps1" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "ERROR: Service '$ServiceName' already exists!" -ForegroundColor Red
    Write-Host "Please uninstall the existing service first:" -ForegroundColor Yellow
    Write-Host "  .\uninstall-from-dist.ps1" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Copy files to installation directory
Write-Host "[1/4] Copying files to installation directory..." -ForegroundColor Green
try {
    if (Test-Path $InstallPath) {
        Write-Host "   Removing existing installation directory..." -ForegroundColor Gray
        Remove-Item $InstallPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

    # Copy all files from dist
    Copy-Item -Path "$distFullPath\*" -Destination $InstallPath -Recurse -Force

    Write-Host "   Files installed to: $InstallPath" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: Failed to copy files: $_" -ForegroundColor Red
    exit 1
}

# Update appsettings.json with configured port
Write-Host ""
Write-Host "[2/4] Configuring service settings..." -ForegroundColor Green
$appsettingsPath = Join-Path $InstallPath "appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

        # Ensure Service section exists
        if (-not $appsettings.Service) {
            $appsettings | Add-Member -MemberType NoteProperty -Name Service -Value ([PSCustomObject]@{})
        }

        $appsettings.Service.Port = $Port
        $appsettings.Service.ServiceName = $ServiceName
        $appsettings.Service.DisplayName = $DisplayName
        $appsettings.Service.Description = $Description

        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding UTF8
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
$serviceExePath = Join-Path $InstallPath "MsMqApp.exe"

try {
    New-Service `
        -Name $ServiceName `
        -DisplayName $DisplayName `
        -Description $Description `
        -BinaryPathName $serviceExePath `
        -StartupType $StartupType `
        -ErrorAction Stop

    Write-Host "   Service created successfully" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: Failed to create service: $_" -ForegroundColor Red

    # Clean up installation directory
    Write-Host "   Cleaning up installation directory..." -ForegroundColor Gray
    Remove-Item $InstallPath -Recurse -Force -ErrorAction SilentlyContinue

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
Write-Host "  Install Path: $InstallPath" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Start the service:  Start-Service -Name $ServiceName" -ForegroundColor Yellow
Write-Host "  2. Check status:       Get-Service -Name $ServiceName" -ForegroundColor Yellow
Write-Host "  3. Access web UI:      http://localhost:$Port" -ForegroundColor Yellow
Write-Host "  4. View logs:          Event Viewer > Windows Logs > Application (Source: $ServiceName)" -ForegroundColor Yellow
Write-Host ""
Write-Host "To uninstall: .\uninstall-from-dist.ps1" -ForegroundColor Gray
Write-Host ""
