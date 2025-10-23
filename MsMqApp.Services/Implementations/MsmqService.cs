using Experimental.System.Messaging;
using Microsoft.Extensions.Logging;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Results;
using MsMqApp.Services.Helpers;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Services.Implementations;

/// <summary>
/// MSMQ service implementation using System.Messaging
/// </summary>
public class MsmqService : IMsmqService
{
    private readonly ILogger<MsmqService> _logger;

    public MsmqService(ILogger<MsmqService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Queue Discovery

    public Task<OperationResult<IEnumerable<QueueInfo>>> GetQueuesAsync(
        string computerName,
        bool includeSystemQueues = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(computerName);

        try
        {
            _logger.LogInformation("Discovering queues on computer: {ComputerName}", computerName);

            var queues = new List<QueueInfo>();

            // Get private queues
            var privateQueues = MessageQueue.GetPrivateQueuesByMachine(computerName);
            foreach (var queue in privateQueues)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var queueInfo = MsmqConverter.ToQueueInfo(queue);
                queues.Add(queueInfo);
                queue.Dispose();
            }

            _logger.LogInformation("Found {Count} private queues on {ComputerName}", queues.Count, computerName);

            // Filter out system queues if requested
            var result = includeSystemQueues
                ? queues
                : queues.Where(q => !q.IsSystemQueue).ToList();

            return Task.FromResult(OperationResult<IEnumerable<QueueInfo>>.Successful(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover queues on {ComputerName}", computerName);
            return Task.FromResult(OperationResult<IEnumerable<QueueInfo>>.Failure(
                $"Failed to discover queues: {ex.Message}", ex));
        }
    }

    public Task<OperationResult<QueueInfo>> GetQueueInfoAsync(
        string queuePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);

        try
        {
            using var queue = new MessageQueue(queuePath);
            var queueInfo = MsmqConverter.ToQueueInfo(queue);
            return Task.FromResult(OperationResult<QueueInfo>.Successful(queueInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue info for {QueuePath}", queuePath);
            return Task.FromResult(OperationResult<QueueInfo>.Failure(
                $"Failed to get queue info: {ex.Message}", ex));
        }
    }

    public Task<OperationResult<int>> GetMessageCountAsync(
        string queuePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);

        try
        {
            using var queue = new MessageQueue(queuePath);
            var messages = queue.GetAllMessages();
            return Task.FromResult(OperationResult<int>.Successful(messages.Length));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message count for {QueuePath}", queuePath);
            return Task.FromResult(OperationResult<int>.Failure(
                $"Failed to get message count: {ex.Message}", ex));
        }
    }

    public Task<OperationResult<int>> GetJournalMessageCountAsync(
        string queuePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);
        string journalPath = string.Empty;
        try
        {
            // Construct journal queue path
            // FormatNames need ;JOURNAL (uppercase), regular paths use ;journal (lowercase)
            journalPath = queuePath.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase)
                ? $"{queuePath};journal"
                : $"FormatName:{queuePath};journal";

            _logger.LogInformation("Getting journal message count for {QueuePath}, journal path: {JournalPath}", queuePath, journalPath);

            // Skip existence check for FormatNames as MessageQueue.Exists() doesn't support them
            // Instead, just try to access and handle exceptions
            using var journalQueue = new MessageQueue(journalPath);
            var messages = journalQueue.GetAllMessages();

            _logger.LogInformation("Found {Count} messages in journal for {QueuePath}", messages.Length, queuePath);
            return Task.FromResult(OperationResult<int>.Successful(messages.Length));
        }
        catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound ||
                                                ex.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
        {
            // Queue or journal doesn't exist or not accessible, return 0
            _logger.LogWarning(ex, "Journal queue not accessible for {QueuePath} => {JournalPath}", queuePath, journalPath);
            return Task.FromResult(OperationResult<int>.Successful(0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get journal message count for {QueuePath} => {JournalPath}", queuePath, journalPath);
            return Task.FromResult(OperationResult<int>.Failure(
                $"Failed to get journal message count: {ex.Message}", ex));
        }
    }

    #endregion

    #region Message Retrieval

    public Task<OperationResult<IEnumerable<QueueMessage>>> GetMessagesAsync(
        string queuePath,
        bool peekOnly = true,
        int maxMessages = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);

        try
        {
            if (queuePath.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase) == false)
            {
                _logger.LogWarning("Using FormatName paths may limit certain operations. QueuePath: {QueuePath}", queuePath);
                queuePath = $"FormatName:{queuePath}";
            }
            
            _logger.LogInformation("Retrieving messages from {QueuePath} (peek: {PeekOnly})", queuePath, peekOnly);

            using var queue = new MessageQueue(queuePath);
            queue.MessageReadPropertyFilter.SetAll();

            var msmqMessages = peekOnly ? queue.GetAllMessages() : queue.GetAllMessages();
            var messages = new List<QueueMessage>();

            var limit = maxMessages > 0 ? Math.Min(maxMessages, msmqMessages.Length) : msmqMessages.Length;

            for (int i = 0; i < limit; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                messages.Add(MsmqConverter.ToQueueMessage(msmqMessages[i], queuePath));
            }

            _logger.LogInformation("Retrieved {Count} messages from {QueuePath}", messages.Count, queuePath);

            return Task.FromResult(OperationResult<IEnumerable<QueueMessage>>.Successful(messages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve messages from {QueuePath}", queuePath);
            return Task.FromResult(OperationResult<IEnumerable<QueueMessage>>.Failure(
                $"Failed to retrieve messages: {ex.Message}", ex));
        }
    }

    public Task<OperationResult<IEnumerable<QueueMessage>>> GetJournalMessagesAsync(
        string journalQueuePath,
        int maxMessages = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journalQueuePath);

        try
        {
            _logger.LogInformation("Retrieving journal messages from {JournalQueuePath}", journalQueuePath);

            // The journalQueuePath should already have ;journal appended by the caller
            // Skip existence check for FormatNames as MessageQueue.Exists() doesn't support them
            // Instead, just try to access and handle exceptions
            using var journalQueue = new MessageQueue(journalQueuePath);
            journalQueue.MessageReadPropertyFilter.SetAll();

            var msmqMessages = journalQueue.GetAllMessages();
            var messages = new List<QueueMessage>();

            var limit = maxMessages > 0 ? Math.Min(maxMessages, msmqMessages.Length) : msmqMessages.Length;

            for (int i = 0; i < limit; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                messages.Add(MsmqConverter.ToQueueMessage(msmqMessages[i], journalQueuePath));
            }

            _logger.LogInformation("Retrieved {Count} journal messages from {JournalQueuePath}", messages.Count, journalQueuePath);

            return Task.FromResult(OperationResult<IEnumerable<QueueMessage>>.Successful(messages));
        }
        catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound ||
                                                ex.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
        {
            // Queue or journal doesn't exist or not accessible, return failure with details
            _logger.LogWarning(ex, "Journal queue not accessible: {JournalQueuePath}", journalQueuePath);
            return Task.FromResult(OperationResult<IEnumerable<QueueMessage>>.Failure(
                $"Journal queue not accessible: {ex.Message} (ErrorCode: {ex.MessageQueueErrorCode})", ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve journal messages from {JournalQueuePath}", journalQueuePath);
            return Task.FromResult(OperationResult<IEnumerable<QueueMessage>>.Failure(
                $"Failed to retrieve journal messages: {ex.Message}", ex));
        }
    }

    public Task<OperationResult<QueueMessage?>> GetMessageByIdAsync(
        string queuePath,
        string messageId,
        bool peekOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);
        ArgumentNullException.ThrowIfNull(messageId);

        try
        {
            using var queue = new MessageQueue(queuePath);
            queue.MessageReadPropertyFilter.SetAll();

            var msmqMessage = peekOnly ? queue.PeekById(messageId) : queue.ReceiveById(messageId);
            var message = MsmqConverter.ToQueueMessage(msmqMessage, queuePath);

            return Task.FromResult(OperationResult<QueueMessage?>.Successful(message));
        }
        catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
        {
            return Task.FromResult(OperationResult<QueueMessage?>.Successful(null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message {MessageId} from {QueuePath}", messageId, queuePath);
            return Task.FromResult(OperationResult<QueueMessage?>.Failure(
                $"Failed to get message: {ex.Message}", ex));
        }
    }

    public Task<OperationResult<QueueMessage?>> GetMessageByLookupIdAsync(
        string queuePath,
        long lookupId,
        bool peekOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);

        try
        {
            using var queue = new MessageQueue(queuePath);
            queue.MessageReadPropertyFilter.SetAll();

            var msmqMessage = peekOnly
                ? queue.PeekByLookupId(lookupId)
                : queue.ReceiveByLookupId(lookupId);

            var message = MsmqConverter.ToQueueMessage(msmqMessage, queuePath);
            return Task.FromResult(OperationResult<QueueMessage?>.Successful(message));
        }
        catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
        {
            return Task.FromResult(OperationResult<QueueMessage?>.Successful(null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message by lookup ID {LookupId} from {QueuePath}", lookupId, queuePath);
            return Task.FromResult(OperationResult<QueueMessage?>.Failure(
                $"Failed to get message: {ex.Message}", ex));
        }
    }

    #endregion

    #region Message Operations

    public Task<OperationResult> DeleteMessageAsync(
        string queuePath,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);
        ArgumentNullException.ThrowIfNull(messageId);

        try
        {
            using var queue = new MessageQueue(queuePath);
            queue.ReceiveById(messageId);

            _logger.LogInformation("Deleted message {MessageId} from {QueuePath}", messageId, queuePath);
            return Task.FromResult(OperationResult.Successful());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId} from {QueuePath}", messageId, queuePath);
            return Task.FromResult(OperationResult.Failure($"Failed to delete message: {ex.Message}", ex));
        }
    }

    public Task<OperationResult> MoveMessageAsync(
        string sourceQueuePath,
        string destinationQueuePath,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceQueuePath);
        ArgumentNullException.ThrowIfNull(destinationQueuePath);
        ArgumentNullException.ThrowIfNull(messageId);

        return Task.FromResult(OperationResult.Failure("Move operation not yet implemented"));
    }

    public Task<OperationResult> PurgeQueueAsync(
        string queuePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);

        try
        {
            using var queue = new MessageQueue(queuePath);
            var count = queue.GetAllMessages().Length;
            queue.Purge();

            _logger.LogWarning("Purged {Count} messages from {QueuePath}", count, queuePath);

            var result = OperationResult.Successful();
            result.Metadata["MessagesPurged"] = count;
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge {QueuePath}", queuePath);
            return Task.FromResult(OperationResult.Failure($"Failed to purge queue: {ex.Message}", ex));
        }
    }

    public Task<OperationResult> SendMessageAsync(
        string queuePath,
        QueueMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);
        ArgumentNullException.ThrowIfNull(message);

        return Task.FromResult(OperationResult.Failure("Send operation not yet implemented"));
    }

    #endregion

    #region Connection Testing

    public Task<OperationResult> TestConnectionAsync(
        string computerName,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(computerName);

        try
        {
            _logger.LogInformation("Testing connection to {ComputerName}", computerName);
            MessageQueue.GetPrivateQueuesByMachine(computerName);

            return Task.FromResult(OperationResult.Successful());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for {ComputerName}", computerName);
            return Task.FromResult(OperationResult.Failure(
                $"Connection test failed: {ex.Message}", ex));
        }
    }

    public Task<OperationResult<bool>> QueueExistsAsync(
        string queuePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queuePath);

        try
        {
            var exists = MessageQueue.Exists(queuePath);
            return Task.FromResult(OperationResult<bool>.Successful(exists));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if queue exists: {QueuePath}", queuePath);
            return Task.FromResult(OperationResult<bool>.Failure(
                $"Failed to check queue existence: {ex.Message}", ex));
        }
    }

    #endregion
}
