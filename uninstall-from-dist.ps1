#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Uninstalls the MSMQ Monitor Windows service installed from standalone distribution.

.DESCRIPTION
    This script stops and removes the MSMQ Monitor Windows service
    and optionally removes the installation files.
    It must be run with Administrator privileges.

.PARAMETER ServiceName
    The name of the Windows service. Default: MSMQMonitor

.PARAMETER InstallPath
    Where the service files are installed. Default: C:\Program Files\MSMQMonitor

.PARAMETER RemoveFiles
    If specified, also removes the installation files.

.EXAMPLE
    .\uninstall-from-dist.ps1

.EXAMPLE
    .\uninstall-from-dist.ps1 -RemoveFiles

.EXAMPLE
    .\uninstall-from-dist.ps1 -ServiceName "CustomMSMQ" -InstallPath "C:\Services\CustomMSMQ" -RemoveFiles
#>

param(
    [string]$ServiceName = "MSMQMonitor",
    [string]$InstallPath = "C:\Program Files\MSMQMonitor",
    [switch]$RemoveFiles
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MSMQ Monitor Service Uninstaller" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow

    if ($RemoveFiles -and (Test-Path $InstallPath)) {
        Write-Host ""
        Write-Host "Removing installation files..." -ForegroundColor Green
        try {
            Remove-Item $InstallPath -Recurse -Force
            Write-Host "   Files removed from: $InstallPath" -ForegroundColor Gray
        }
        catch {
            Write-Host "ERROR: Could not remove files: $_" -ForegroundColor Red
        }
    }

    exit 0
}

# Stop the service if running
Write-Host "[1/2] Stopping service..." -ForegroundColor Green
if ($service.Status -eq "Running") {
    try {
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        Write-Host "   Service stopped" -ForegroundColor Gray
    }
    catch {
        Write-Host "WARNING: Could not stop service: $_" -ForegroundColor Yellow
    }
}
else {
    Write-Host "   Service is already stopped" -ForegroundColor Gray
}

# Remove the service
Write-Host ""
Write-Host "[2/2] Removing service..." -ForegroundColor Green
try {
    # Use sc.exe to delete the service
    $result = sc.exe delete $ServiceName
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe delete failed with exit code $LASTEXITCODE"
    }
    Write-Host "   Service removed successfully" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: Failed to remove service: $_" -ForegroundColor Red
    exit 1
}

# Remove installation files if requested
if ($RemoveFiles) {
    Write-Host ""
    Write-Host "Removing installation files..." -ForegroundColor Green
    if (Test-Path $InstallPath) {
        try {
            # Wait a moment for service to fully release files
            Start-Sleep -Seconds 2
            Remove-Item $InstallPath -Recurse -Force -ErrorAction Stop
            Write-Host "   Files removed from: $InstallPath" -ForegroundColor Gray
        }
        catch {
            Write-Host "WARNING: Could not remove all files: $_" -ForegroundColor Yellow
            Write-Host "   You may need to manually delete: $InstallPath" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "   No installation files found at: $InstallPath" -ForegroundColor Gray
    }
}

# Display completion message
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

if (-not $RemoveFiles -and (Test-Path $InstallPath)) {
    Write-Host "NOTE: Installation files are still at: $InstallPath" -ForegroundColor Yellow
    Write-Host "To remove them, run: .\uninstall-from-dist.ps1 -RemoveFiles" -ForegroundColor Yellow
    Write-Host ""
}
