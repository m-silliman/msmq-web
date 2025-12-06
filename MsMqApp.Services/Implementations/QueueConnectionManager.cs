using Microsoft.Extensions.Logging;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Services.Implementations;

/// <summary>
/// Implementation of queue connection manager - manages connections to local and remote MSMQ computers
/// </summary>
public class QueueConnectionManager : IQueueConnectionManager
{
    private readonly Dictionary<string, QueueConnection> _connections = new();
    private readonly IMsmqService _msmqService;
    private readonly ILogger<QueueConnectionManager> _logger;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<ConnectionRefreshedEventArgs>? ConnectionRefreshed;
    public event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;

    public QueueConnectionManager(IMsmqService msmqService, ILogger<QueueConnectionManager> logger)
    {
        _msmqService = msmqService ?? throw new ArgumentNullException(nameof(msmqService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResult<QueueConnection>> ConnectAsync(
        string computerName,
        string? username = null,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(computerName);

        // Normalize computer name
        var normalizedName = NormalizeComputerName(computerName);

        _logger.LogInformation("Attempting to connect to {ComputerName} (normalized: {NormalizedName})",
            computerName, normalizedName);

        // Check if already connected
        var existing = _connections.Values.FirstOrDefault(c =>
            c.ComputerName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _logger.LogInformation("Already connected to {ComputerName}, returning existing connection", normalizedName);
            return OperationResult<QueueConnection>.Successful(existing);
        }

        var connection = new QueueConnection
        {
            Id = Guid.NewGuid().ToString(),
            ComputerName = normalizedName,
            DisplayName = displayName ?? GetDisplayName(normalizedName),
            Username = username,
            IsLocal = IsLocalComputer(normalizedName),
            Status = ConnectionStatus.Connecting
        };

        try
        {
            // Test connection first
            var testResult = await _msmqService.TestConnectionAsync(normalizedName, 30, cancellationToken);
            if (!testResult.Success)
            {
                connection.MarkFailed(GetFriendlyErrorMessage(testResult.ErrorMessage));
                _logger.LogError("Connection test failed for {ComputerName}: {Error}", normalizedName, testResult.ErrorMessage);

                ConnectionFailed?.Invoke(this, new ConnectionFailedEventArgs
                {
                    ConnectionId = connection.Id,
                    ErrorMessage = testResult.ErrorMessage ?? "Unknown error",
                    WillRetry = false,
                    RetryAttempt = 0
                });

                return OperationResult<QueueConnection>.Failure(
                    GetFriendlyErrorMessage(testResult.ErrorMessage),
                    testResult.Exception);
            }

            // Get queues
            var queuesResult = await _msmqService.GetQueuesAsync(normalizedName, false, cancellationToken);
            if (!queuesResult.Success)
            {
                connection.MarkFailed(GetFriendlyErrorMessage(queuesResult.ErrorMessage));
                _logger.LogError("Failed to get queues for {ComputerName}: {Error}", normalizedName, queuesResult.ErrorMessage);

                ConnectionFailed?.Invoke(this, new ConnectionFailedEventArgs
                {
                    ConnectionId = connection.Id,
                    ErrorMessage = queuesResult.ErrorMessage ?? "Unknown error",
                    WillRetry = false,
                    RetryAttempt = 0
                });

                return OperationResult<QueueConnection>.Failure(
                    GetFriendlyErrorMessage(queuesResult.ErrorMessage),
                    queuesResult.Exception);
            }

            // Populate queue journal counts
            var queues = queuesResult.Data?.ToList() ?? new List<QueueInfo>();
            foreach (var queue in queues)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var journalCountResult = await _msmqService.GetJournalMessageCountAsync(queue.Path, cancellationToken);
                    if (journalCountResult.Success)
                    {
                        queue.JournalMessageCount = journalCountResult.Data;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get journal count for queue {QueuePath}", queue.Path);
                    // Continue even if journal count fails
                }
            }

            connection.RefreshQueues(queues);
            connection.MarkConnected();

            // Add to connections dictionary
            _connections[connection.Id] = connection;

            _logger.LogInformation("Successfully connected to {ComputerName} with {QueueCount} queues",
                normalizedName, queues.Count);

            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                ConnectionId = connection.Id,
                PreviousStatus = ConnectionStatus.Connecting,
                NewStatus = ConnectionStatus.Connected
            });

            return OperationResult<QueueConnection>.Successful(connection);
        }
        catch (Exception ex)
        {
            var errorMsg = GetFriendlyErrorMessage(ex.Message);
            connection.MarkFailed(errorMsg);
            _logger.LogError(ex, "Failed to connect to {ComputerName}", normalizedName);

            ConnectionFailed?.Invoke(this, new ConnectionFailedEventArgs
            {
                ConnectionId = connection.Id,
                ErrorMessage = ex.Message,
                WillRetry = false,
                RetryAttempt = 0
            });

            return OperationResult<QueueConnection>.Failure(errorMsg, ex);
        }
    }

    public Task<OperationResult> DisconnectAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

        if (!_connections.TryGetValue(connectionId, out var connection))
        {
            return Task.FromResult(OperationResult.Failure($"Connection {connectionId} not found"));
        }

        var oldStatus = connection.Status;
        connection.MarkDisconnected();
        _connections.Remove(connectionId);

        _logger.LogInformation("Disconnected from {ComputerName}", connection.ComputerName);

        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            ConnectionId = connection.Id,
            PreviousStatus = oldStatus,
            NewStatus = ConnectionStatus.Disconnected
        });

        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult> DisconnectAllAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement disconnect all logic
        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult<QueueConnection>> ReconnectAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        // TODO: Implement reconnect logic
        return Task.FromResult(OperationResult<QueueConnection>.Failure("Not yet implemented"));
    }

    public Task<OperationResult<IEnumerable<QueueConnection>>> GetAllConnectionsAsync()
    {
        return Task.FromResult(OperationResult<IEnumerable<QueueConnection>>.Successful(_connections.Values));
    }

    public Task<OperationResult<QueueConnection?>> GetConnectionAsync(string connectionId)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        _connections.TryGetValue(connectionId, out var connection);
        return Task.FromResult(OperationResult<QueueConnection?>.Successful(connection));
    }

    public Task<OperationResult<QueueConnection?>> GetConnectionByComputerNameAsync(string computerName)
    {
        ArgumentNullException.ThrowIfNull(computerName);
        var connection = _connections.Values.FirstOrDefault(c => c.ComputerName.Equals(computerName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(OperationResult<QueueConnection?>.Successful(connection));
    }

    public Task<OperationResult<QueueConnection>> GetLocalConnectionAsync()
    {
        // TODO: Implement local connection logic
        var connection = new QueueConnection
        {
            ComputerName = ".",
            DisplayName = "Local Computer",
            IsLocal = true,
            Status = ConnectionStatus.NotConnected
        };
        return Task.FromResult(OperationResult<QueueConnection>.Successful(connection));
    }

    public Task<OperationResult<IEnumerable<QueueConnection>>> GetConnectionsByStatusAsync(ConnectionStatus status)
    {
        var connections = _connections.Values.Where(c => c.Status == status);
        return Task.FromResult(OperationResult<IEnumerable<QueueConnection>>.Successful(connections));
    }

    public async Task<OperationResult<QueueConnection>> RefreshConnectionAsync(
        string connectionId,
        bool includeSystemQueues = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

        if (!_connections.TryGetValue(connectionId, out var connection))
        {
            return OperationResult<QueueConnection>.Failure($"Connection {connectionId} not found");
        }

        _logger.LogInformation("Refreshing connection {ConnectionId} for {ComputerName}",
            connectionId, connection.ComputerName);

        try
        {
            // Get queues
            var queuesResult = await _msmqService.GetQueuesAsync(
                connection.ComputerName, includeSystemQueues, cancellationToken);

            if (!queuesResult.Success)
            {
                _logger.LogError("Failed to refresh queues for {ComputerName}: {Error}",
                    connection.ComputerName, queuesResult.ErrorMessage);
                return OperationResult<QueueConnection>.Failure(
                    GetFriendlyErrorMessage(queuesResult.ErrorMessage),
                    queuesResult.Exception);
            }

            // Populate queue journal counts
            var queues = queuesResult.Data?.ToList() ?? new List<QueueInfo>();
            foreach (var queue in queues)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var journalCountResult = await _msmqService.GetJournalMessageCountAsync(
                        queue.Path, cancellationToken);
                    if (journalCountResult.Success)
                    {
                        queue.JournalMessageCount = journalCountResult.Data;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get journal count for queue {QueuePath}", queue.Path);
                }
            }

            connection.RefreshQueues(queues);

            _logger.LogInformation("Refreshed connection {ConnectionId} with {QueueCount} queues",
                connectionId, queues.Count);

            ConnectionRefreshed?.Invoke(this, new ConnectionRefreshedEventArgs
            {
                ConnectionId = connection.Id,
                QueueCount = queues.Count
            });

            return OperationResult<QueueConnection>.Successful(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh connection {ConnectionId}", connectionId);
            return OperationResult<QueueConnection>.Failure(
                GetFriendlyErrorMessage(ex.Message), ex);
        }
    }

    public Task<OperationResult> RefreshAllConnectionsAsync(
        bool includeSystemQueues = false,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement refresh all logic
        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult<bool>> TestConnectionHealthAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        // TODO: Implement health check logic
        return Task.FromResult(OperationResult<bool>.Successful(false));
    }

    public Task<OperationResult<QueueConnection>> UpdateConnectionSettingsAsync(
        string connectionId,
        bool? autoRefreshEnabled = null,
        int? autoRefreshIntervalSeconds = null,
        bool? showSystemQueues = null,
        bool? showJournalQueues = null)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        // TODO: Implement settings update logic
        return Task.FromResult(OperationResult<QueueConnection>.Failure("Not yet implemented"));
    }

    public Task<OperationResult> StartAutoRefreshAsync()
    {
        // TODO: Implement auto-refresh start logic
        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult> StopAutoRefreshAsync()
    {
        // TODO: Implement auto-refresh stop logic
        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult<bool>> IsAutoRefreshRunningAsync()
    {
        // TODO: Implement auto-refresh status check
        return Task.FromResult(OperationResult<bool>.Successful(false));
    }

    public Task<OperationResult> PauseAutoRefreshAsync(string connectionId)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        // TODO: Implement pause logic
        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult> ResumeAutoRefreshAsync(string connectionId)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        // TODO: Implement resume logic
        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult> SaveConnectionsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement save logic
        return Task.FromResult(OperationResult.Successful());
    }

    public Task<OperationResult<IEnumerable<QueueConnection>>> LoadConnectionsAsync(
        bool autoConnect = true,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement load logic
        return Task.FromResult(OperationResult<IEnumerable<QueueConnection>>.Successful(Enumerable.Empty<QueueConnection>()));
    }

    public Task<OperationResult> ClearSavedConnectionsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement clear logic
        return Task.FromResult(OperationResult.Successful());
    }

    #region Helper Methods

    private static string NormalizeComputerName(string computerName)
    {
        var normalized = computerName.Trim();

        // Convert localhost and . to actual machine name for display consistency
        if (normalized.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return ".";
        }

        return normalized;
    }

    private static bool IsLocalComputer(string computerName)
    {
        return computerName == "." ||
               computerName.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               computerName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDisplayName(string computerName)
    {
        if (IsLocalComputer(computerName))
        {
            return $"{Environment.MachineName} (Local)";
        }

        return computerName;
    }

    private static string GetFriendlyErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "An unknown error occurred";

        // Map common MSMQ errors to friendly messages
        if (errorMessage.Contains("RPC server", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("network path", StringComparison.OrdinalIgnoreCase))
        {
            return "Unable to reach the remote computer. Please verify the computer name and ensure the computer is online and accessible on the network.";
        }

        if (errorMessage.Contains("access", StringComparison.OrdinalIgnoreCase) &&
            errorMessage.Contains("denied", StringComparison.OrdinalIgnoreCase))
        {
            return "Access denied. You do not have permission to access MSMQ on this computer. Please verify your credentials and permissions.";
        }

        if (errorMessage.Contains("MSMQ", StringComparison.OrdinalIgnoreCase) &&
            (errorMessage.Contains("not installed", StringComparison.OrdinalIgnoreCase) ||
             errorMessage.Contains("not available", StringComparison.OrdinalIgnoreCase)))
        {
            return "MSMQ is not installed or not running on the target computer. Please ensure MSMQ is installed and the Message Queuing service is started.";
        }

        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "Connection timed out. The remote computer did not respond within the expected time. Please check network connectivity and firewall settings.";
        }

        if (errorMessage.Contains("DNS", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("host not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Unable to resolve the computer name. Please verify the computer name is correct and that DNS is functioning properly.";
        }

        // Return original message if no specific mapping found
        return errorMessage;
    }

    #endregion
}
