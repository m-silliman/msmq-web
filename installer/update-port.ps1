param([string]$InstallDir, [int]$Port)

$configPath = Join-Path $InstallDir 'appsettings.json'
if (Test-Path $configPath) {
    try {
        $config = Get-Content $configPath | ConvertFrom-Json
        $config.Service.Port = $Port
        $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
        Write-Host "Updated port configuration to $Port"
    } catch {
        Write-Host "Error updating configuration: $_"
    }
} else {
    Write-Host "Configuration file not found: $configPath"
}
