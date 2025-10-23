namespace MsMqApp.Models.Configuration;

/// <summary>
/// Application configuration settings
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Gets or sets the default refresh interval in seconds
    /// </summary>
    public int DefaultRefreshIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum message body size in bytes
    /// </summary>
    public int MaxMessageBodySizeBytes { get; set; } = 1048576; // 1 MB

    /// <summary>
    /// Gets or sets the message list page size
    /// </summary>
    public int MessageListPageSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the remote connection timeout in seconds
    /// </summary>
    public int RemoteConnectionTimeoutSeconds { get; set; } = 30;
}
