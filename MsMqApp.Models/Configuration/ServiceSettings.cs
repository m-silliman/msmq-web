namespace MsMqApp.Models.Configuration;

/// <summary>
/// Windows Service configuration settings
/// </summary>
public class ServiceSettings
{
    /// <summary>
    /// Gets or sets the HTTP port the service will listen on
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Gets or sets the Windows service name
    /// </summary>
    public string ServiceName { get; set; } = "MSMQMonitor";

    /// <summary>
    /// Gets or sets the display name shown in Windows Services
    /// </summary>
    public string DisplayName { get; set; } = "MSMQ Monitor & Management Tool";

    /// <summary>
    /// Gets or sets the service description
    /// </summary>
    public string Description { get; set; } = "Web-based MSMQ monitoring and management service";

    /// <summary>
    /// Gets or sets whether the service should auto-start with Windows
    /// </summary>
    public bool AutoStart { get; set; } = true;
}
