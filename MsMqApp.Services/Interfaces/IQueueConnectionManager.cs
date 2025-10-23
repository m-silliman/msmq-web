using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.Interfaces;

/// <summary>
/// Service interface for managing connections to local and remote MSMQ computers.
/// Handles connection lifecycle, state management, and automatic reconnection.
/// </summary>
/// <remarks>
/// This service maintains a collection of active connections and provides
/// automatic reconnection, health monitoring, and connection pooling capabilities.
/// All connection operations are thread-safe.
/// </remarks>
public interface IQueueConnectionManager
{
    #region Connection Management

    /// <summary>
    /// Establishes a connection to a computer's MSMQ service.
    /// </summary>
    /// <param name="computerName">
    /// The computer name or IP address. Use "." or "localhost" for local computer.
    /// </param>
    /// <param name="username">
    /// Optional username for authentication. Leave null to use current credentials.
    /// </param>
    /// <param name="displayName">
    /// Optional display name for the connection. If null, uses computerName.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the established QueueConnection,
    /// or failure result if connection cannot be established.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when computerName is null or empty</exception>
    /// <remarks>
    /// The connection is automatically added to the active connections collection.
    /// If a connection to the same computer already exists, it will be reused.
    /// </remarks>
    Task<OperationResult<QueueConnection>> ConnectAsync(
        string computerName,
        string? username = null,
        string? displayName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from a computer's MSMQ service.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection to disconnect</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure of the disconnect operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    /// <remarks>
    /// The connection is removed from the active connections collection.
    /// Any pending operations on this connection will be cancelled.
    /// </remarks>
    Task<OperationResult> DisconnectAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects all active connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// Metadata contains the number of connections disconnected.
    /// </returns>
    Task<OperationResult> DisconnectAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconnects to a previously disconnected or failed connection.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection to reconnect</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the reconnected QueueConnection,
    /// or failure result if reconnection fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    Task<OperationResult<QueueConnection>> ReconnectAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Connection Retrieval

    /// <summary>
    /// Gets all active connections.
    /// </summary>
    /// <returns>
    /// An operation result containing a collection of all QueueConnection objects.
    /// Returns empty collection if no connections exist.
    /// </returns>
    Task<OperationResult<IEnumerable<QueueConnection>>> GetAllConnectionsAsync();

    /// <summary>
    /// Gets a specific connection by its unique identifier.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection</param>
    /// <returns>
    /// An operation result containing the QueueConnection if found,
    /// or null if connection doesn't exist.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    Task<OperationResult<QueueConnection?>> GetConnectionAsync(string connectionId);

    /// <summary>
    /// Gets a connection by computer name.
    /// </summary>
    /// <param name="computerName">The computer name to search for</param>
    /// <returns>
    /// An operation result containing the QueueConnection if found,
    /// or null if no connection exists for this computer.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when computerName is null or empty</exception>
    Task<OperationResult<QueueConnection?>> GetConnectionByComputerNameAsync(string computerName);

    /// <summary>
    /// Gets the local computer connection.
    /// </summary>
    /// <returns>
    /// An operation result containing the local QueueConnection.
    /// Creates a new connection if one doesn't exist.
    /// </returns>
    Task<OperationResult<QueueConnection>> GetLocalConnectionAsync();

    /// <summary>
    /// Gets all connections that match the specified status.
    /// </summary>
    /// <param name="status">The connection status to filter by</param>
    /// <returns>
    /// An operation result containing a collection of QueueConnection objects
    /// that match the specified status.
    /// </returns>
    Task<OperationResult<IEnumerable<QueueConnection>>> GetConnectionsByStatusAsync(
        ConnectionStatus status);

    #endregion

    #region Connection State Management

    /// <summary>
    /// Refreshes the queue list for a specific connection.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection</param>
    /// <param name="includeSystemQueues">
    /// If true, includes system queues in the refresh. Default is false.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the updated QueueConnection with refreshed queue list.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    Task<OperationResult<QueueConnection>> RefreshConnectionAsync(
        string connectionId,
        bool includeSystemQueues = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the queue lists for all active connections.
    /// </summary>
    /// <param name="includeSystemQueues">
    /// If true, includes system queues in the refresh. Default is false.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// Metadata contains the number of connections refreshed.
    /// </returns>
    Task<OperationResult> RefreshAllConnectionsAsync(
        bool includeSystemQueues = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the health of a specific connection.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection to test</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing true if connection is healthy,
    /// false if unhealthy. ErrorMessage contains health check details.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    /// <remarks>
    /// A healthy connection can successfully enumerate queues and is responsive.
    /// Unhealthy connections are automatically marked for reconnection if auto-reconnect is enabled.
    /// </remarks>
    Task<OperationResult<bool>> TestConnectionHealthAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the connection settings for an existing connection.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection</param>
    /// <param name="autoRefreshEnabled">Enable or disable auto-refresh</param>
    /// <param name="autoRefreshIntervalSeconds">Auto-refresh interval in seconds</param>
    /// <param name="showSystemQueues">Show or hide system queues</param>
    /// <param name="showJournalQueues">Show or hide journal queues</param>
    /// <returns>
    /// An operation result containing the updated QueueConnection.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    Task<OperationResult<QueueConnection>> UpdateConnectionSettingsAsync(
        string connectionId,
        bool? autoRefreshEnabled = null,
        int? autoRefreshIntervalSeconds = null,
        bool? showSystemQueues = null,
        bool? showJournalQueues = null);

    #endregion

    #region Auto-Refresh Management

    /// <summary>
    /// Starts the auto-refresh service for all connections with auto-refresh enabled.
    /// </summary>
    /// <returns>
    /// An operation result indicating success or failure of starting the service.
    /// </returns>
    /// <remarks>
    /// The auto-refresh service runs in the background and periodically refreshes
    /// queue lists for connections based on their configured refresh intervals.
    /// This method is idempotent - calling it multiple times has no additional effect.
    /// </remarks>
    Task<OperationResult> StartAutoRefreshAsync();

    /// <summary>
    /// Stops the auto-refresh service for all connections.
    /// </summary>
    /// <returns>
    /// An operation result indicating success or failure of stopping the service.
    /// </returns>
    Task<OperationResult> StopAutoRefreshAsync();

    /// <summary>
    /// Gets the auto-refresh status.
    /// </summary>
    /// <returns>
    /// An operation result containing true if auto-refresh is running, false otherwise.
    /// </returns>
    Task<OperationResult<bool>> IsAutoRefreshRunningAsync();

    /// <summary>
    /// Pauses auto-refresh for a specific connection.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    Task<OperationResult> PauseAutoRefreshAsync(string connectionId);

    /// <summary>
    /// Resumes auto-refresh for a specific connection.
    /// </summary>
    /// <param name="connectionId">The unique identifier of the connection</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionId is null or empty</exception>
    Task<OperationResult> ResumeAutoRefreshAsync(string connectionId);

    #endregion

    #region Connection Persistence

    /// <summary>
    /// Saves the current connections to persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// Metadata contains the number of connections saved.
    /// </returns>
    /// <remarks>
    /// Saved connections can be restored on application restart.
    /// Credentials are not saved for security reasons.
    /// </remarks>
    Task<OperationResult> SaveConnectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads previously saved connections from persistent storage.
    /// </summary>
    /// <param name="autoConnect">
    /// If true, automatically connects to loaded connections. Default is true.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the loaded connections.
    /// Metadata contains the number of connections loaded and successfully connected.
    /// </returns>
    Task<OperationResult<IEnumerable<QueueConnection>>> LoadConnectionsAsync(
        bool autoConnect = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all saved connections from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// </returns>
    Task<OperationResult> ClearSavedConnectionsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when a connection's queue list is refreshed.
    /// </summary>
    event EventHandler<ConnectionRefreshedEventArgs>? ConnectionRefreshed;

    /// <summary>
    /// Event raised when a connection fails and auto-reconnect will be attempted.
    /// </summary>
    event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;

    #endregion
}

/// <summary>
/// Event arguments for connection state changes
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the connection ID
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the previous state
    /// </summary>
    public ConnectionStatus PreviousStatus { get; init; }

    /// <summary>
    /// Gets the new state
    /// </summary>
    public ConnectionStatus NewStatus { get; init; }

    /// <summary>
    /// Gets the timestamp of the change
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for connection refresh events
/// </summary>
public class ConnectionRefreshedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the connection ID
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of queues discovered
    /// </summary>
    public int QueueCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the refresh
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for connection failure events
/// </summary>
public class ConnectionFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the connection ID
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether auto-reconnect will be attempted
    /// </summary>
    public bool WillRetry { get; init; }

    /// <summary>
    /// Gets the retry attempt number
    /// </summary>
    public int RetryAttempt { get; init; }

    /// <summary>
    /// Gets the timestamp of the failure
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
