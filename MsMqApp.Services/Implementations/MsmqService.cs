using Experimental.System.Messaging;
using Microsoft.Extensions.Logging;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
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
            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string actualQueuePath = queuePath;
            if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                actualQueuePath = $"FormatName:{queuePath}";
            }

            using var queue = new MessageQueue(actualQueuePath);
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
            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string actualQueuePath = queuePath;
            if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                actualQueuePath = $"FormatName:{queuePath}";
            }

            using var queue = new MessageQueue(actualQueuePath);
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
            _logger.LogWarning("===> Retrieving journal messages from {JournalQueuePath}", journalQueuePath);

            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string queuePath = journalQueuePath;
            if (journalQueuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                queuePath = $"FormatName:{journalQueuePath}";
            }

            using var journalQueue = new MessageQueue(queuePath);

            // The journalQueuePath should already have ;journal appended by the caller
            // Skip existence check for FormatNames as MessageQueue.Exists() doesn't support them
            // Instead, just try to access and handle exceptions
            // using var journalQueue = new MessageQueue(journalQueuePath);
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
            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string actualQueuePath = queuePath;
            if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                actualQueuePath = $"FormatName:{queuePath}";
            }

            using var queue = new MessageQueue(actualQueuePath);
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
            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string actualQueuePath = queuePath;
            if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                actualQueuePath = $"FormatName:{queuePath}";
            }

            using var queue = new MessageQueue(actualQueuePath);
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
            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string actualQueuePath = queuePath;
            if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                actualQueuePath = $"FormatName:{queuePath}";
            }

            using var queue = new MessageQueue(actualQueuePath);
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
            // Convert DIRECT format to FormatName format for the MessageQueue constructor
            string actualQueuePath = queuePath;
            if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                actualQueuePath = $"FormatName:{queuePath}";
            }

            using var queue = new MessageQueue(actualQueuePath);
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

        try
        {
            _logger.LogInformation("Sending message to queue: {QueuePath}", queuePath);

            // Ensure the queue path has FormatName prefix for proper handling
            var actualQueuePath = queuePath.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase)
                ? queuePath
                : $"FormatName:{queuePath}";

            _logger.LogInformation("Using queue path: {ActualQueuePath}", actualQueuePath);

            using var queue = new MessageQueue(actualQueuePath);
            
            // Log queue properties for debugging
            try
            {
                _logger.LogInformation("Queue properties - CanWrite: {CanWrite}, MachineName: {MachineName}, Path: {Path}", 
                    queue.CanWrite, queue.MachineName ?? "Unknown", queue.Path ?? "Unknown");
                
                if (!queue.CanWrite)
                {
                    _logger.LogWarning("Queue {QueuePath} is not writable", queuePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read queue properties for {QueuePath}", queuePath);
            }
            
            // Convert QueueMessage to System.Messaging.Message
            var systemMessage = ConvertToSystemMessage(message);
            _logger.LogInformation("Created system message with Label: '{Label}' for encoding: '{Encoding}'", 
                systemMessage.Label, message.Body?.Encoding ?? "UTF-8");

            // Use the transaction type specified by the user in the message
            var transactionType = message.IsTransactional 
                ? MessageQueueTransactionType.Single 
                : MessageQueueTransactionType.None;
            _logger.LogInformation("Using transaction type: {TransactionType} (user specified: {IsTransactional}) for queue: {QueuePath}", 
                transactionType, message.IsTransactional, queuePath);

            // Send the message with appropriate transaction handling
            bool messageSent = false;
            MessageQueueTransactionType actualTransactionType = transactionType;

            try
            {
                if (transactionType == MessageQueueTransactionType.Single)
                {
                    // For transactional queues, use a single internal transaction
                    queue.Send(systemMessage, MessageQueueTransactionType.Single);
                    messageSent = true;
                }
                else
                {
                    // For non-transactional queues or when transactions are not supported
                    queue.Send(systemMessage, MessageQueueTransactionType.None);
                    messageSent = true;
                }
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.TransactionUsage)
            {
                // Transaction usage mismatch - try the opposite transaction type
                _logger.LogWarning("Transaction type mismatch for {QueuePath}, trying alternative transaction type. Error: {Error}", 
                    queuePath, ex.Message);

                try
                {
                    if (transactionType == MessageQueueTransactionType.Single)
                    {
                        // Try without transaction
                        queue.Send(systemMessage, MessageQueueTransactionType.None);
                        actualTransactionType = MessageQueueTransactionType.None;
                        _logger.LogInformation("Successfully sent message using None transaction type after Single failed");
                    }
                    else
                    {
                        // Try with transaction
                        queue.Send(systemMessage, MessageQueueTransactionType.Single);
                        actualTransactionType = MessageQueueTransactionType.Single;
                        _logger.LogInformation("Successfully sent message using Single transaction type after None failed");
                    }
                    messageSent = true;
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "Both transaction types failed for queue {QueuePath}", queuePath);
                    throw; // Re-throw the retry exception
                }
            }

            if (!messageSent)
            {
                throw new InvalidOperationException("Message was not sent successfully");
            }

            _logger.LogInformation("Successfully sent message to {QueuePath} using transaction type: {TransactionType}", 
                queuePath, actualTransactionType);
            
            // Return success with message ID in metadata
            var result = OperationResult.Successful();
            result.Metadata["MessageId"] = systemMessage.Id;
            result.Metadata["QueuePath"] = queuePath;
            result.Metadata["Label"] = message.Label;
            result.Metadata["TransactionType"] = actualTransactionType.ToString();
            
            return Task.FromResult(result);
        }
        catch (MessageQueueException ex)
        {
            var errorMessage = $"MSMQ error sending message to {queuePath}: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            return Task.FromResult(OperationResult.Failure(errorMessage, ex));
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to send message to {queuePath}: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            return Task.FromResult(OperationResult.Failure(errorMessage, ex));
        }
    }

    /// <summary>
    /// Converts a QueueMessage domain model to a System.Messaging.Message.
    /// </summary>
    /// <param name="queueMessage">The domain message to convert</param>
    /// <returns>A System.Messaging.Message ready to be sent</returns>
    private static Message ConvertToSystemMessage(QueueMessage queueMessage)
    {
        var systemMessage = new Message();

        // Set basic properties
        if (!string.IsNullOrEmpty(queueMessage.Label))
        {
            systemMessage.Label = queueMessage.Label;
        }

        // Set message body based on format
        if (queueMessage.Body != null)
        {
            SetMessageBody(systemMessage, queueMessage.Body);
        }

        // Set priority
        systemMessage.Priority = (Experimental.System.Messaging.MessagePriority)queueMessage.Priority;

        // Set recoverable flag
        systemMessage.Recoverable = queueMessage.Recoverable;

        // Set timeouts
        if (queueMessage.TimeToReachQueue != TimeSpan.MaxValue && queueMessage.TimeToReachQueue > TimeSpan.Zero)
        {
            systemMessage.TimeToReachQueue = queueMessage.TimeToReachQueue;
        }

        if (queueMessage.TimeToBeReceived != TimeSpan.MaxValue && queueMessage.TimeToBeReceived > TimeSpan.Zero)
        {
            systemMessage.TimeToBeReceived = queueMessage.TimeToBeReceived;
        }

        // Set correlation ID
        if (!string.IsNullOrEmpty(queueMessage.CorrelationId))
        {
            systemMessage.CorrelationId = queueMessage.CorrelationId;
        }

        // Set response queue if specified
        if (!string.IsNullOrEmpty(queueMessage.ResponseQueue))
        {
            try
            {
                systemMessage.ResponseQueue = new MessageQueue(queueMessage.ResponseQueue);
            }
            catch
            {
                // Ignore invalid response queue paths
            }
        }

        return systemMessage;
    }

    /// <summary>
    /// Sets the body of a System.Messaging.Message based on the MessageBody format.
    /// </summary>
    /// <param name="systemMessage">The system message to set the body on</param>
    /// <param name="messageBody">The domain message body</param>
    private static void SetMessageBody(Message systemMessage, MessageBody messageBody)
    {
        var content = messageBody.RawContent ?? string.Empty;
        var encoding = GetTextEncoding(messageBody.Encoding);

        // For all text-based formats and non-UTF8 encodings, convert to bytes with specified encoding
        if (messageBody.Format == MessageBodyFormat.Binary)
        {
            // Handle binary content
            if (messageBody.RawBytes != null)
            {
                systemMessage.BodyStream = new MemoryStream(messageBody.RawBytes);
            }
            else if (!string.IsNullOrEmpty(content))
            {
                // Try hex string first, fallback to encoded bytes
                try
                {
                    var bytes = ConvertHexStringToBytes(content);
                    systemMessage.BodyStream = new MemoryStream(bytes);
                }
                catch
                {
                    var bytes = encoding.GetBytes(content);
                    systemMessage.BodyStream = new MemoryStream(bytes);
                }
            }
            else
            {
                systemMessage.Body = string.Empty;
            }
        }
        else if (encoding.WebName.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
        {
            // UTF-8: Use simple string body for maximum compatibility
            systemMessage.Body = content;
        }
        else
        {
            // Non-UTF8 encodings: Convert to bytes and use BodyStream
            var bytes = encoding.GetBytes(content);
            systemMessage.BodyStream = new MemoryStream(bytes);
        }

        // Set appropriate formatter for all cases
        systemMessage.Formatter = new ActiveXMessageFormatter();
    }



    /// <summary>
    /// Gets the System.Text.Encoding instance for the specified encoding name.
    /// </summary>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The corresponding Encoding instance, defaulting to UTF-8 if invalid</returns>
    private static System.Text.Encoding GetTextEncoding(string encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
        {
            return System.Text.Encoding.UTF8;
        }

        try
        {
            return encodingName.ToUpperInvariant() switch
            {
                "UTF-8" => System.Text.Encoding.UTF8,
                "UTF-16" => System.Text.Encoding.Unicode,
                "ASCII" => System.Text.Encoding.ASCII,
                "UTF-32" => System.Text.Encoding.UTF32,
                "ISO-8859-1" => System.Text.Encoding.Latin1,
                "WINDOWS-1252" => System.Text.Encoding.GetEncoding("Windows-1252"),
                _ => System.Text.Encoding.GetEncoding(encodingName)
            };
        }
        catch (ArgumentException)
        {
            // Fall back to UTF-8 if the encoding name is invalid
            return System.Text.Encoding.UTF8;
        }
        catch (NotSupportedException)
        {
            // Fall back to UTF-8 if the encoding is not supported
            return System.Text.Encoding.UTF8;
        }
        catch
        {
            // Fall back to UTF-8 for any other encoding-related exceptions
            return System.Text.Encoding.UTF8;
        }
    }

    /// <summary>
    /// Converts a hex string to byte array.
    /// </summary>
    /// <param name="hexString">The hex string to convert</param>
    /// <returns>Byte array</returns>
    private static byte[] ConvertHexStringToBytes(string hexString)
    {
        // Remove any spaces or formatting
        hexString = hexString.Replace(" ", "").Replace("-", "").Replace(":", "");
        
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have an even number of characters");
        }

        var bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return bytes;
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
            // MessageQueue.Exists() doesn't work with FormatName or DIRECT= paths
            // For these paths, try to create a MessageQueue and see if it works
            if (queuePath.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase) || 
                queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Convert DIRECT= format to FormatName format if needed
                    string actualQueuePath = queuePath;
                    if (queuePath.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
                    {
                        actualQueuePath = $"FormatName:{queuePath}";
                    }

                    using var testQueue = new MessageQueue(actualQueuePath);
                    // Try to access a property to verify the queue exists
                    _ = testQueue.CanRead;
                    return Task.FromResult(OperationResult<bool>.Successful(true));
                }
                catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                {
                    return Task.FromResult(OperationResult<bool>.Successful(false));
                }
                catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    // Queue exists but we don't have access - still counts as exists
                    return Task.FromResult(OperationResult<bool>.Successful(true));
                }
            }
            else
            {
                // Use standard Exists for regular paths
                var exists = MessageQueue.Exists(queuePath);
                return Task.FromResult(OperationResult<bool>.Successful(exists));
            }
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
