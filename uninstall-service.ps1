#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Uninstalls the MSMQ Monitor Windows service.

.DESCRIPTION
    This script stops and removes the MSMQ Monitor Windows service.
    It must be run with Administrator privileges.

.PARAMETER ServiceName
    The name of the Windows service. Default: MSMQMonitor

.PARAMETER RemoveFiles
    If specified, also removes the published application files.

.EXAMPLE
    .\uninstall-service.ps1

.EXAMPLE
    .\uninstall-service.ps1 -RemoveFiles
#>

param(
    [string]$ServiceName = "MSMQMonitor",
    [switch]$RemoveFiles
)

$ErrorActionPreference = "Stop"

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishPath = Join-Path $scriptPath "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MSMQ Monitor Service Uninstaller" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow

    if ($RemoveFiles -and (Test-Path $publishPath)) {
        Write-Host ""
        Write-Host "Removing published files..." -ForegroundColor Green
        Remove-Item $publishPath -Recurse -Force
        Write-Host "   Files removed from: $publishPath" -ForegroundColor Gray
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

# Remove published files if requested
if ($RemoveFiles) {
    Write-Host ""
    Write-Host "Removing published files..." -ForegroundColor Green
    if (Test-Path $publishPath) {
        try {
            # Wait a moment for service to fully release files
            Start-Sleep -Seconds 2
            Remove-Item $publishPath -Recurse -Force -ErrorAction Stop
            Write-Host "   Files removed from: $publishPath" -ForegroundColor Gray
        }
        catch {
            Write-Host "WARNING: Could not remove all files: $_" -ForegroundColor Yellow
            Write-Host "   You may need to manually delete: $publishPath" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "   No published files found at: $publishPath" -ForegroundColor Gray
    }
}

# Display completion message
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

if (-not $RemoveFiles -and (Test-Path $publishPath)) {
    Write-Host "NOTE: Published files are still at: $publishPath" -ForegroundColor Yellow
    Write-Host "To remove them, run: .\uninstall-service.ps1 -RemoveFiles" -ForegroundColor Yellow
    Write-Host ""
}
