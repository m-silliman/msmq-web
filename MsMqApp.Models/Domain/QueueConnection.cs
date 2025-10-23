using MsMqApp.Models.Enums;

namespace MsMqApp.Models.Domain;

/// <summary>
/// Represents a connection to a local or remote computer for MSMQ access
/// </summary>
public class QueueConnection
{
    /// <summary>
    /// Gets or sets the unique identifier for this connection
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the computer name or IP address
    /// </summary>
    public string ComputerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for this connection
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection status
    /// </summary>
    public ConnectionStatus Status { get; set; } = ConnectionStatus.NotConnected;

    /// <summary>
    /// Gets or sets whether this is a local connection
    /// </summary>
    public bool IsLocal { get; set; }

    /// <summary>
    /// Gets or sets the username for authentication (if required)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets when the connection was established
    /// </summary>
    public DateTime? ConnectedAt { get; set; }

    /// <summary>
    /// Gets or sets when the connection was last used
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if connection failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to automatically reconnect on failure
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the list of queues discovered on this connection
    /// </summary>
    public List<QueueInfo> Queues { get; set; } = new List<QueueInfo>();

    /// <summary>
    /// Gets or sets whether the connection is currently being refreshed
    /// </summary>
    public bool IsRefreshing { get; set; }

    /// <summary>
    /// Gets or sets when the queues were last refreshed
    /// </summary>
    public DateTime? LastRefreshedAt { get; set; }

    /// <summary>
    /// Gets or sets the auto-refresh interval in seconds (0 = disabled)
    /// </summary>
    public int AutoRefreshIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether auto-refresh is enabled
    /// </summary>
    public bool AutoRefreshEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this connection is pinned (always shown)
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets whether to show system queues
    /// </summary>
    public bool ShowSystemQueues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show journal queues
    /// </summary>
    public bool ShowJournalQueues { get; set; } = true;

    /// <summary>
    /// Gets or sets custom metadata for this connection
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the total number of queues
    /// </summary>
    public int TotalQueues => Queues.Count;

    /// <summary>
    /// Gets the total number of messages across all queues
    /// </summary>
    public int TotalMessages => Queues.Sum(q => q.MessageCount);

    /// <summary>
    /// Gets whether the connection is currently connected
    /// </summary>
    public bool IsConnected => Status == ConnectionStatus.Connected;

    /// <summary>
    /// Gets whether the connection has failed
    /// </summary>
    public bool HasFailed => Status == ConnectionStatus.Failed || Status == ConnectionStatus.Timeout;

    /// <summary>
    /// Gets whether the connection can retry
    /// </summary>
    public bool CanRetry => RetryAttempts < MaxRetryAttempts && HasFailed && AutoReconnect;

    /// <summary>
    /// Gets the connection uptime (if connected)
    /// </summary>
    public TimeSpan? Uptime => ConnectedAt.HasValue && IsConnected
        ? DateTime.UtcNow - ConnectedAt.Value.ToUniversalTime()
        : null;

    /// <summary>
    /// Gets a formatted display name for the connection
    /// </summary>
    public string FormattedDisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(DisplayName))
                return DisplayName;

            if (IsLocal)
                return "Local Computer";

            return ComputerName;
        }
    }

    /// <summary>
    /// Gets a status description
    /// </summary>
    public string StatusDescription => Status switch
    {
        ConnectionStatus.NotConnected => "Not connected",
        ConnectionStatus.Connecting => "Connecting...",
        ConnectionStatus.Connected => $"Connected ({TotalQueues} queues, {TotalMessages} messages)",
        ConnectionStatus.Failed => $"Failed: {ErrorMessage}",
        ConnectionStatus.Disconnected => "Disconnected",
        ConnectionStatus.Timeout => "Connection timeout",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the queues filtered by current settings
    /// </summary>
    public IEnumerable<QueueInfo> FilteredQueues
    {
        get
        {
            var filtered = Queues.AsEnumerable();

            if (!ShowSystemQueues)
                filtered = filtered.Where(q => !q.IsSystemQueue);

            if (!ShowJournalQueues)
                filtered = filtered.Where(q => !q.IsJournalQueue);

            return filtered;
        }
    }

    /// <summary>
    /// Marks the connection as connected
    /// </summary>
    public void MarkConnected()
    {
        Status = ConnectionStatus.Connected;
        ConnectedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
        RetryAttempts = 0;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marks the connection as failed
    /// </summary>
    public void MarkFailed(string errorMessage)
    {
        Status = ConnectionStatus.Failed;
        ErrorMessage = errorMessage;
        RetryAttempts++;
    }

    /// <summary>
    /// Marks the connection as disconnected
    /// </summary>
    public void MarkDisconnected()
    {
        Status = ConnectionStatus.Disconnected;
        ConnectedAt = null;
    }

    /// <summary>
    /// Updates the last accessed time
    /// </summary>
    public void UpdateLastAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Refreshes the queue list with new data
    /// </summary>
    public void RefreshQueues(IEnumerable<QueueInfo> queues)
    {
        Queues.Clear();
        Queues.AddRange(queues);
        LastRefreshedAt = DateTime.UtcNow;
        UpdateLastAccessed();
    }

    /// <summary>
    /// Creates a deep copy of this connection (without queues)
    /// </summary>
    public QueueConnection Clone()
    {
        return new QueueConnection
        {
            Id = Id,
            ComputerName = ComputerName,
            DisplayName = DisplayName,
            Status = Status,
            IsLocal = IsLocal,
            Username = Username,
            ConnectedAt = ConnectedAt,
            LastAccessedAt = LastAccessedAt,
            ErrorMessage = ErrorMessage,
            TimeoutSeconds = TimeoutSeconds,
            AutoReconnect = AutoReconnect,
            RetryAttempts = RetryAttempts,
            MaxRetryAttempts = MaxRetryAttempts,
            IsRefreshing = IsRefreshing,
            LastRefreshedAt = LastRefreshedAt,
            AutoRefreshIntervalSeconds = AutoRefreshIntervalSeconds,
            AutoRefreshEnabled = AutoRefreshEnabled,
            IsPinned = IsPinned,
            ShowSystemQueues = ShowSystemQueues,
            ShowJournalQueues = ShowJournalQueues,
            Metadata = new Dictionary<string, string>(Metadata)
        };
    }

    /// <summary>
    /// Returns a string representation of this connection
    /// </summary>
    public override string ToString()
    {
        return $"{FormattedDisplayName} - {StatusDescription}";
    }
}
