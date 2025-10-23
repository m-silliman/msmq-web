<#
.SYNOPSIS
    Builds a self-contained distribution for the MSMQ Monitor application.

.DESCRIPTION
    This script publishes the application as a self-contained distribution
    that includes the .NET runtime and all dependencies. The resulting folder
    contains all files needed to run the application on any Windows machine
    without requiring .NET to be pre-installed.

.PARAMETER OutputPath
    The directory where the executable will be created. Default: .\dist

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release

.PARAMETER Runtime
    Target runtime identifier. Default: win-x64

.PARAMETER IncludeSymbols
    If specified, includes debug symbols (.pdb files) in the output.

.EXAMPLE
    .\build-standalone.ps1

.EXAMPLE
    .\build-standalone.ps1 -OutputPath "C:\Deploy" -Configuration Debug

.EXAMPLE
    .\build-standalone.ps1 -Runtime win-x86
#>

param(
    [string]$OutputPath = ".\dist",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$IncludeSymbols
)

$ErrorActionPreference = "Stop"

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptPath "MsMqApp"
$projectFile = Join-Path $projectPath "MsMqApp.csproj"
$outputFullPath = Join-Path $scriptPath $OutputPath

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MSMQ Monitor - Standalone Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate project exists
if (-not (Test-Path $projectFile)) {
    Write-Host "ERROR: Project file not found at: $projectFile" -ForegroundColor Red
    exit 1
}

# Display build settings
Write-Host "Build Settings:" -ForegroundColor Green
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host "  Runtime:       $Runtime" -ForegroundColor White
Write-Host "  Output Path:   $outputFullPath" -ForegroundColor White
Write-Host "  Project:       $projectFile" -ForegroundColor White
Write-Host ""

# Clean output directory
if (Test-Path $outputFullPath) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item $outputFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

# Build the application
Write-Host "Building self-contained distribution..." -ForegroundColor Green
Write-Host ""

try {
    $publishArgs = @(
        "publish"
        $projectFile
        "-c", $Configuration
        "-r", $Runtime
        "--self-contained", "true"
        "-o", $outputFullPath
        "/p:PublishSingleFile=false"
        "/p:IncludeNativeLibrariesInSingleFile=false"
        "/p:DebugType=embedded"
    )

    # Add symbol options
    if (-not $IncludeSymbols) {
        $publishArgs += "/p:DebugSymbols=false"
    }

    Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    Write-Host ""

    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: Build failed: $_" -ForegroundColor Red
    exit 1
}

# Get the executable name
$exeName = "MsMqApp.exe"
$exePath = Join-Path $outputFullPath $exeName

if (-not (Test-Path $exePath)) {
    Write-Host ""
    Write-Host "ERROR: Executable not found at: $exePath" -ForegroundColor Red
    exit 1
}

# Get total distribution size
$allFiles = Get-ChildItem $outputFullPath -File -Recurse
$totalSizeBytes = ($allFiles | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSizeBytes / 1MB, 2)
$fileCount = $allFiles.Count

# Clean up unnecessary build artifacts (but keep runtime files)
Write-Host ""
Write-Host "Cleaning up unnecessary build artifacts..." -ForegroundColor Green

# Files to remove (build artifacts that aren't needed for runtime)
$removeFiles = @(
    "*.deps.json.backup",
    "*.runtimeconfig.dev.json",
    "createdump.exe"  # Debug tool not needed for production
)

# Remove specified unnecessary files
foreach ($pattern in $removeFiles) {
    Get-ChildItem $outputFullPath -Filter $pattern -ErrorAction SilentlyContinue | Remove-Item -Force
}

# Optionally remove debug symbols if not requested
if (-not $IncludeSymbols) {
    Write-Host "Removing debug symbols (.pdb files)..." -ForegroundColor Gray
    Get-ChildItem $outputFullPath -Filter "*.pdb" -Recurse | Remove-Item -Force
}

# Display completion message
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Distribution Details:" -ForegroundColor Cyan
Write-Host "  Location: $outputFullPath" -ForegroundColor White
Write-Host "  Total Size: $totalSizeMB MB ($fileCount files)" -ForegroundColor White
Write-Host "  Runtime:  $Runtime (.NET runtime included)" -ForegroundColor White
Write-Host "  Main Executable: $exeName" -ForegroundColor White
Write-Host ""

# List key files and summary
Write-Host "Key Distribution Files:" -ForegroundColor Cyan
$keyFiles = @("MsMqApp.exe", "MsMqApp.dll", "appsettings.json", "System.*.dll", "Microsoft.*.dll")
foreach ($pattern in $keyFiles) {
    $matchingFiles = Get-ChildItem $outputFullPath -Filter $pattern -ErrorAction SilentlyContinue
    if ($matchingFiles) {
        if ($matchingFiles.Count -eq 1) {
            $size = [math]::Round($matchingFiles.Length / 1KB, 2)
            Write-Host "  $($matchingFiles.Name) ($size KB)" -ForegroundColor White
        } else {
            Write-Host "  $($matchingFiles.Count) files matching '$pattern'" -ForegroundColor White
        }
    }
}

Write-Host ""
Write-Host "File Summary:" -ForegroundColor Cyan
$exeFiles = Get-ChildItem $outputFullPath -Filter "*.exe"
$dllFiles = Get-ChildItem $outputFullPath -Filter "*.dll"
$configFiles = Get-ChildItem $outputFullPath -Filter "*.json"
Write-Host "  Executables: $($exeFiles.Count)" -ForegroundColor White
Write-Host "  Libraries: $($dllFiles.Count)" -ForegroundColor White
Write-Host "  Config Files: $($configFiles.Count)" -ForegroundColor White
Write-Host "  Total Files: $fileCount" -ForegroundColor White

Write-Host ""
Write-Host "Distribution Package:" -ForegroundColor Cyan
Write-Host "  The files in '$OutputPath' can be copied to any Windows machine" -ForegroundColor White
Write-Host "  and run without installing .NET Framework." -ForegroundColor White
Write-Host ""
Write-Host "Usage:" -ForegroundColor Cyan
Write-Host "  Run directly:      .\$exeName" -ForegroundColor Yellow
Write-Host "  Install service:   sc create MSMQMonitor binPath= \`"C:\path\to\$exeName\`"" -ForegroundColor Yellow
Write-Host "  Or use:            .\install-service.ps1 (from published folder)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Copy the entire '$OutputPath' folder to your target machine" -ForegroundColor Yellow
Write-Host "  2. Run MsMqApp.exe directly, or install as a service" -ForegroundColor Yellow
Write-Host "  3. Access the web UI at http://localhost:8080" -ForegroundColor Yellow
Write-Host ""

# Create a README in the output folder
$readmePath = Join-Path $outputFullPath "README.txt"
$readmeContent = @"
MSMQ Monitor & Management Tool
===============================

This is a self-contained distribution that includes the .NET runtime.
No installation of .NET Framework is required.

SYSTEM REQUIREMENTS:
- Windows 10/11 or Windows Server 2016+
- MSMQ feature installed
- Administrator privileges (for service installation)

QUICK START:
1. Run directly: Double-click MsMqApp.exe
2. Access web UI: http://localhost:8080

INSTALL AS WINDOWS SERVICE:
1. Open PowerShell or Command Prompt as Administrator
2. Run:
   sc create MSMQMonitor binPath= "C:\full\path\to\MsMqApp.exe" start= auto
3. Start the service:
   sc start MSMQMonitor

UNINSTALL SERVICE:
1. Stop the service:
   sc stop MSMQMonitor
2. Delete the service:
   sc delete MSMQMonitor

CONFIGURATION:
- Edit appsettings.json to change settings
- Default port: 8080
- To change port: Edit "Service" -> "Port" in appsettings.json

LOGS:
- When running directly: Console output
- When running as service: Windows Event Viewer (Application log, Source: MSMQMonitor)

FIREWALL:
For remote access, add firewall rule:
  netsh advfirewall firewall add rule name="MSMQ Monitor" dir=in action=allow protocol=TCP localport=8080

BUILD INFO:
- Configuration: $Configuration
- Runtime: $Runtime
- Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
- Self-Contained: Yes (includes .NET runtime - $fileCount files)
- Distribution Type: Full self-contained deployment
- Total Size: $totalSizeMB MB

For more information, see SERVICE-INSTALLATION.md in the source repository.
"@

Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8

Write-Host "Created README.txt with usage instructions" -ForegroundColor Gray
Write-Host ""
