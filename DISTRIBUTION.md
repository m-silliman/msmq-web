# Distribution Guide

This document explains how to create and distribute the MSMQ Monitor application as a standalone executable.

## Overview

The MSMQ Monitor can be distributed as a **self-contained, single-file executable** that includes the .NET runtime. This means:

- ✅ No .NET installation required on target machines
- ✅ Single executable file (MsMqApp.exe ~47 MB)
- ✅ Works on any Windows 10/11 or Server 2016+ machine
- ✅ Can run directly or install as Windows Service

## Building the Standalone Executable

### Prerequisites

- .NET 9.0 SDK (only on build machine)
- Windows OS
- PowerShell 5.1 or later

### Build Steps

1. Open PowerShell in the project root directory
2. Run the build script:

```powershell
.\build-standalone.ps1
```

This will create a `dist` folder containing:
- `MsMqApp.exe` (~47 MB) - The self-contained executable
- `appsettings.json` - Configuration file
- `appsettings.Development.json` - Development settings
- `web.config` - IIS configuration (optional)
- `README.txt` - User instructions

### Build Options

```powershell
# Build for different Windows architectures
.\build-standalone.ps1 -Runtime win-x64    # 64-bit (default)
.\build-standalone.ps1 -Runtime win-x86    # 32-bit
.\build-standalone.ps1 -Runtime win-arm64  # ARM64

# Build in Debug configuration
.\build-standalone.ps1 -Configuration Debug

# Include debug symbols
.\build-standalone.ps1 -IncludeSymbols

# Custom output path
.\build-standalone.ps1 -OutputPath "C:\Deploy"

# Full example
.\build-standalone.ps1 `
    -Configuration Release `
    -Runtime win-x64 `
    -OutputPath ".\release" `
    -IncludeSymbols
```

## Distribution Methods

### Method 1: Direct Copy & Run

**Simplest method - no installation required**

1. Build the standalone executable
2. Copy the entire `dist` folder to target machine
3. Run `MsMqApp.exe`
4. Access web UI at `http://localhost:8080`

**Pros:**
- No installation/uninstallation needed
- Easy to test and update
- Portable - can run from USB drive

**Cons:**
- Must manually start after reboot
- Runs as current user (not a service)

### Method 2: Install as Windows Service

**Recommended for production use**

#### Installation on Target Machine:

1. Copy the `dist` folder to target machine
2. Open PowerShell as Administrator
3. Navigate to the project folder
4. Run:

```powershell
.\install-from-dist.ps1
```

This will:
- Copy files to `C:\Program Files\MSMQMonitor`
- Create Windows Service "MSMQMonitor"
- Configure auto-start on boot
- Set up automatic restart on failure

#### Custom Installation:

```powershell
# Custom port
.\install-from-dist.ps1 -Port 9090

# Custom installation path
.\install-from-dist.ps1 -InstallPath "C:\Services\MSMQ"

# Custom service name
.\install-from-dist.ps1 -ServiceName "MyMSMQ" -DisplayName "My MSMQ Monitor"

# Full custom installation
.\install-from-dist.ps1 `
    -DistPath ".\dist" `
    -ServiceName "CustomMSMQ" `
    -DisplayName "Custom MSMQ Monitor" `
    -Port 9090 `
    -InstallPath "C:\MyServices\MSMQ" `
    -StartupType Manual
```

#### Uninstallation:

```powershell
# Remove service only
.\uninstall-from-dist.ps1

# Remove service and files
.\uninstall-from-dist.ps1 -RemoveFiles

# Custom service name
.\uninstall-from-dist.ps1 -ServiceName "MyMSMQ" -RemoveFiles
```

**Pros:**
- Auto-starts with Windows
- Runs as background service
- Automatic restart on failure
- Professional deployment

**Cons:**
- Requires administrator privileges
- More complex installation

### Method 3: Create ZIP Package

For easy distribution:

```powershell
# After building standalone
Compress-Archive -Path ".\dist\*" -DestinationPath "MSMQMonitor-v1.0.0.zip"
```

Distribute the ZIP file with instructions:
1. Extract to desired location
2. Run `MsMqApp.exe` or use `install-from-dist.ps1`

## Target Machine Requirements

### Minimum Requirements

- **OS**: Windows 10 (1607+) or Windows Server 2016+
- **RAM**: 512 MB minimum (1 GB recommended)
- **Disk**: 150 MB free space
- **MSMQ**: Message Queuing feature installed
- **Network**: Port 8080 available (or custom port)

### No Additional Software Required

- ❌ No .NET Framework installation needed
- ❌ No Visual Studio needed
- ❌ No additional runtimes needed
- ✅ 100% self-contained

### Enabling MSMQ on Target Machine

MSMQ must be installed on the target machine:

**Windows 10/11:**
1. Control Panel → Programs → Turn Windows features on or off
2. Enable "Microsoft Message Queue (MSMQ) Server"
3. Reboot if prompted

**Windows Server:**
```powershell
Install-WindowsFeature MSMQ-Server
```

## Configuration

### appsettings.json

Edit before distribution or on target machine:

```json
{
  "Service": {
    "Port": 8080,
    "ServiceName": "MSMQMonitor",
    "DisplayName": "MSMQ Monitor & Management Tool"
  },
  "Application": {
    "DefaultRefreshIntervalSeconds": 5,
    "MaxMessageBodySizeBytes": 1048576,
    "MessageListPageSize": 100
  }
}
```

### Changing Port After Installation

1. Stop service: `Stop-Service MSMQMonitor`
2. Edit: `C:\Program Files\MSMQMonitor\appsettings.json`
3. Change `Service.Port` value
4. Start service: `Start-Service MSMQMonitor`

## Firewall Configuration

For remote access, open firewall port:

**PowerShell:**
```powershell
New-NetFirewallRule `
    -DisplayName "MSMQ Monitor" `
    -Direction Inbound `
    -LocalPort 8080 `
    -Protocol TCP `
    -Action Allow
```

**Command Prompt:**
```cmd
netsh advfirewall firewall add rule ^
    name="MSMQ Monitor" ^
    dir=in ^
    action=allow ^
    protocol=TCP ^
    localport=8080
```

## Verification

### Test Standalone Executable

```powershell
cd dist
.\MsMqApp.exe
```

Should see:
```
info: MSMQ Monitor & Management Tool starting...
info: Service Name: MSMQMonitor
info: Listening on port: 8080
```

Access: `http://localhost:8080`

### Test Service Installation

```powershell
# Check service status
Get-Service MSMQMonitor

# Check if port is listening
netstat -an | findstr :8080

# View service logs
Get-EventLog -LogName Application -Source MSMQMonitor -Newest 10
```

## Troubleshooting

### "This app can't run on your PC"

- Wrong architecture (e.g., ARM64 exe on x64 machine)
- Rebuild with correct `-Runtime` parameter

### Port Already in Use

- Check what's using port 8080:
  ```powershell
  Get-Process -Id (Get-NetTCPConnection -LocalPort 8080).OwningProcess
  ```
- Change port in `appsettings.json` or use `-Port` parameter

### Service Won't Start

1. Check Event Viewer: Application log, Source: MSMQMonitor
2. Verify MSMQ is installed and running
3. Ensure service account has permissions
4. Try running exe manually to see errors

### Large File Size

The executable is ~47 MB because it includes:
- .NET 9.0 runtime
- ASP.NET Core libraries
- All dependencies

This is normal for self-contained apps.

### Access Denied

- Run as Administrator for service installation
- Verify MSMQ permissions for queues
- Check Windows Firewall rules

## Deployment Checklist

### Pre-Deployment

- [ ] Build standalone executable
- [ ] Test on development machine
- [ ] Configure `appsettings.json`
- [ ] Create deployment package (ZIP)
- [ ] Write deployment notes

### On Target Machine

- [ ] Verify MSMQ is installed
- [ ] Copy distribution files
- [ ] Configure firewall (if remote access needed)
- [ ] Install as service or run directly
- [ ] Test web UI access
- [ ] Verify can see MSMQ queues

### Post-Deployment

- [ ] Check Event Log for errors
- [ ] Verify service auto-starts (if installed)
- [ ] Test queue monitoring functionality
- [ ] Document configuration for administrators

## Version Management

### Versioning the Executable

Edit `MsMqApp.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <FileVersion>1.0.0.0</FileVersion>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
</PropertyGroup>
```

Check version:
```powershell
(Get-Item .\dist\MsMqApp.exe).VersionInfo
```

### Upgrade Process

1. Stop service (if installed)
2. Backup old `appsettings.json`
3. Replace exe with new version
4. Restore custom settings
5. Start service

## Support Information

### Where to Get Help

1. Check Event Viewer for error messages
2. Review this documentation
3. Check project repository issues
4. Contact system administrator

### Collecting Diagnostic Information

```powershell
# Export recent logs
Get-EventLog -LogName Application -Source MSMQMonitor -Newest 100 |
    Export-Csv -Path "msmq-monitor-logs.csv"

# Check service status
Get-Service MSMQMonitor | Format-List *

# Check configuration
Get-Content "C:\Program Files\MSMQMonitor\appsettings.json"

# Check listening ports
netstat -an | findstr :8080
```

## Best Practices

### Development

- ✅ Test standalone build before distribution
- ✅ Verify all features work in standalone mode
- ✅ Test on clean VM without .NET installed

### Distribution

- ✅ Include README.txt in package
- ✅ Document custom configuration
- ✅ Provide contact information
- ✅ Version your releases

### Deployment

- ✅ Test on target environment first
- ✅ Install as service for production
- ✅ Configure firewall appropriately
- ✅ Monitor Event Log after deployment

### Security

- ✅ Run service with least privilege account
- ✅ Restrict firewall to necessary IPs
- ✅ Keep Windows and MSMQ updated
- ✅ Review MSMQ queue permissions
