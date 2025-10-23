# Windows Service Installation Guide

This document describes how to install, configure, and manage the MSMQ Monitor application as a Windows service.

## Prerequisites

- Windows OS with MSMQ feature enabled
- .NET 9.0 SDK or later
- Administrator privileges
- PowerShell 5.1 or later

## Quick Start

### Installation

1. Open PowerShell as Administrator
2. Navigate to the project directory
3. Run the installation script:

```powershell
.\install-service.ps1
```

4. Start the service:

```powershell
Start-Service -Name MSMQMonitor
```

5. Access the web UI at: `http://localhost:8080`

### Uninstallation

```powershell
.\uninstall-service.ps1
```

To also remove published files:

```powershell
.\uninstall-service.ps1 -RemoveFiles
```

## Custom Installation Options

### Change Port

```powershell
.\install-service.ps1 -Port 9090
```

### Custom Service Name

```powershell
.\install-service.ps1 -ServiceName "MyMSMQMonitor" -DisplayName "My MSMQ Monitor"
```

### Manual Startup

```powershell
.\install-service.ps1 -StartupType Manual
```

### Full Custom Installation

```powershell
.\install-service.ps1 `
    -ServiceName "CustomMSMQ" `
    -DisplayName "Custom MSMQ Monitor" `
    -Description "My custom MSMQ monitoring service" `
    -Port 9090 `
    -StartupType Manual
```

## Managing the Service

### Start/Stop/Restart

```powershell
# Start
Start-Service -Name MSMQMonitor

# Stop
Stop-Service -Name MSMQMonitor

# Restart
Restart-Service -Name MSMQMonitor

# Check status
Get-Service -Name MSMQMonitor
```

### View Logs

The service logs to the Windows Event Log (Application log):

**Using Event Viewer:**
1. Open Event Viewer (eventvwr.msc)
2. Navigate to: Windows Logs > Application
3. Filter by Source: "MSMQMonitor"

**Using PowerShell:**
```powershell
# View recent service events
Get-EventLog -LogName Application -Source MSMQMonitor -Newest 50

# View all service events
Get-EventLog -LogName Application -Source MSMQMonitor

# View errors only
Get-EventLog -LogName Application -Source MSMQMonitor -EntryType Error -Newest 20
```

### Change Service Configuration

#### Change Port After Installation

1. Stop the service
2. Edit the appsettings.json in the publish folder:
   ```
   C:\Users\[YourUser]\Source\msmqmgr\publish\appsettings.json
   ```
3. Update the `Service.Port` value
4. Start the service

#### Change Startup Type

```powershell
Set-Service -Name MSMQMonitor -StartupType Automatic
```

## Configuration

### appsettings.json

The service configuration is located in `appsettings.json`:

```json
{
  "Service": {
    "Port": 8080,
    "ServiceName": "MSMQMonitor",
    "DisplayName": "MSMQ Monitor & Management Tool",
    "Description": "Web-based MSMQ monitoring and management service",
    "AutoStart": true
  },
  "Application": {
    "DefaultRefreshIntervalSeconds": 5,
    "MaxMessageBodySizeBytes": 1048576,
    "MessageListPageSize": 100,
    "RemoteConnectionTimeoutSeconds": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Service Port

Default: `8080`

The service listens on HTTP only (no HTTPS). Access via:
- Local: `http://localhost:8080`
- Remote: `http://[server-name]:8080`

**Note:** Ensure Windows Firewall allows inbound traffic on the configured port for remote access.

### Logging Levels

When running as a service (Production mode), logs are written to:
- **Windows Event Log** (Application log, Source: MSMQMonitor)
- **Console** (for manual runs or debugging)

When running in Development mode (via `dotnet run`):
- **Console**
- **Debug output**

## Troubleshooting

### Service Won't Start

1. Check Event Log for error messages:
   ```powershell
   Get-EventLog -LogName Application -Source MSMQMonitor -EntryType Error -Newest 5
   ```

2. Verify MSMQ is installed and running
3. Check that the port is not already in use
4. Ensure the service account has necessary permissions

### Port Already in Use

If port 8080 is already in use, either:
1. Uninstall and reinstall with a different port
2. Or manually update appsettings.json and restart the service

### Cannot Access from Remote Machine

1. Check Windows Firewall:
   ```powershell
   New-NetFirewallRule -DisplayName "MSMQ Monitor" -Direction Inbound -LocalPort 8080 -Protocol TCP -Action Allow
   ```

2. Verify the service is listening:
   ```powershell
   netstat -an | findstr ":8080"
   ```

### Service Crashes on Startup

1. Check Event Log for stack traces
2. Try running manually to see console output:
   ```powershell
   cd publish
   .\MsMqApp.exe
   ```
3. Verify all dependencies are present in the publish folder

## Automatic Recovery

The service is configured to automatically restart on failure:
- Restart delay: 1 minute
- Restart attempts: 3 consecutive failures
- Reset failure count: After 24 hours

## Manual Service Management

If you prefer to use `sc.exe` directly:

```cmd
:: Create service
sc create MSMQMonitor binPath= "C:\path\to\publish\MsMqApp.exe" start= auto

:: Start service
sc start MSMQMonitor

:: Stop service
sc stop MSMQMonitor

:: Query service status
sc query MSMQMonitor

:: Delete service
sc delete MSMQMonitor
```

## Upgrading the Service

To upgrade to a new version:

1. Stop the service:
   ```powershell
   Stop-Service -Name MSMQMonitor
   ```

2. Back up your appsettings.json (optional but recommended)

3. Run the installation script again (it will publish new files)

4. Restore your custom appsettings.json if needed

5. Start the service:
   ```powershell
   Start-Service -Name MSMQMonitor
   ```

## Development vs Production

### Development (dotnet run)
- Uses console and debug logging
- Runs on ports 5000 (HTTP) or 7000 (HTTPS) by default
- Hot reload enabled
- Developer exception pages

### Production (Windows Service)
- Uses Windows Event Log
- Runs on configured port (default 8080)
- HTTP only
- Production error pages
- Automatic restart on failure

## Security Considerations

1. **Service Account**: The service runs as Local System by default. For production, consider using a dedicated service account with minimal permissions.

2. **Network Access**: The service listens on all network interfaces (0.0.0.0). Restrict access via:
   - Windows Firewall rules
   - Network ACLs
   - Application-level authentication (to be implemented)

3. **MSMQ Permissions**: Ensure the service account has appropriate MSMQ permissions for the queues you want to monitor.

## Support

For issues or questions:
1. Check the Event Log for error messages
2. Review this documentation
3. Check the project repository for known issues
