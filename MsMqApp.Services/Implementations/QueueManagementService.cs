using Experimental.System.Messaging;
using MsMqApp.Models.Results;
using MsMqApp.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MsMqApp.Services.Implementations;

/// <summary>
/// Service for managing MSMQ queue configuration and properties.
/// Uses System.Messaging to update queue settings.
/// </summary>
public class QueueManagementService : IQueueManagementService
{
    private readonly ILogger<QueueManagementService> _logger;

    public QueueManagementService(ILogger<QueueManagementService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OperationResult<bool>> UpdateQueuePropertiesAsync(
        string queuePath,
        string label,
        bool authenticate,
        long maximumQueueSize,
        int privacyLevel,
        bool useJournalQueue,
        long maximumJournalSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(queuePath))
            {
                return OperationResult<bool>.Failure("Queue path cannot be empty");
            }

            _logger.LogInformation("Updating properties for queue: {QueuePath}", queuePath);

            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string actualQueuePath = queuePath;
            if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                actualQueuePath = $"FormatName:{queuePath}";
            }

            _logger.LogDebug("Using actual queue path: {ActualQueuePath}", actualQueuePath);

            // Run on background thread since MessageQueue operations are synchronous
            await Task.Run(() =>
            {
                using var queue = new MessageQueue(actualQueuePath);

                // Update queue properties
                queue.Label = label ?? string.Empty;
                queue.Authenticate = authenticate;
                queue.UseJournalQueue = useJournalQueue;

                // Set privacy level
                queue.EncryptionRequired = privacyLevel switch
                {
                    0 => EncryptionRequired.None,
                    1 => EncryptionRequired.Optional,
                    2 => EncryptionRequired.Body,
                    _ => EncryptionRequired.Optional
                };

                // Set storage limits (convert KB to bytes, but MessageQueue uses KB)
                // Note: MaximumQueueSize and MaximumJournalSize are in KB
                if (maximumQueueSize > 0)
                {
                    queue.MaximumQueueSize = maximumQueueSize;
                }
                else
                {
                    // Set to max value for unlimited (MSMQ uses max long value)
                    queue.MaximumQueueSize = long.MaxValue / 1024; // Convert to KB
                }

                if (maximumJournalSize > 0)
                {
                    queue.MaximumJournalSize = maximumJournalSize;
                }
                else
                {
                    queue.MaximumJournalSize = long.MaxValue / 1024;
                }

            }, cancellationToken);

            _logger.LogInformation("Successfully updated properties for queue: {QueuePath}", queuePath);

            return OperationResult<bool>.Successful(true);
        }
        catch (MessageQueueException ex)
        {
            var errorMessage = ex.MessageQueueErrorCode switch
            {
                MessageQueueErrorCode.QueueNotFound => "Queue not found",
                MessageQueueErrorCode.AccessDenied => "Access denied. You may not have permissions to modify this queue",
                MessageQueueErrorCode.InvalidParameter => "Invalid parameter value provided",
                MessageQueueErrorCode.UnsupportedOperation => "Operation not supported on this queue",
                _ => $"Message queue error: {ex.Message}"
            };

            _logger.LogError(ex, "Failed to update queue properties for {QueuePath}: {Error}", queuePath, errorMessage);
            return OperationResult<bool>.Failure(errorMessage);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access when updating queue properties for {QueuePath}", queuePath);
            return OperationResult<bool>.Failure("Access denied. You do not have permission to modify this queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating queue properties for {QueuePath}", queuePath);
            return OperationResult<bool>.Failure($"Failed to update queue properties: {ex.Message}");
        }
    }
}
