namespace MsMqApp.Models.Enums;

/// <summary>
/// Represents the status of a queue connection
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Connection not yet attempted
    /// </summary>
    NotConnected,

    /// <summary>
    /// Currently attempting to connect
    /// </summary>
    Connecting,

    /// <summary>
    /// Successfully connected
    /// </summary>
    Connected,

    /// <summary>
    /// Connection failed
    /// </summary>
    Failed,

    /// <summary>
    /// Connection disconnected
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connection timeout
    /// </summary>
    Timeout
}
