using Microsoft.Extensions.Logging;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;
using MsMqApp.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace MsMqApp.Services.Implementations;

/// <summary>
/// Implementation of message operations service for MSMQ message management.
/// </summary>
public class MessageOperationsService : IMessageOperationsService
{
    private readonly IMsmqService _msmqService;
    private readonly ILogger<MessageOperationsService> _logger;

    /// <summary>
    /// Initializes a new instance of the MessageOperationsService class.
    /// </summary>
    /// <param name="msmqService">The MSMQ service for low-level operations</param>
    /// <param name="logger">Logger for operation tracking and diagnostics</param>
    public MessageOperationsService(
        IMsmqService msmqService,
        ILogger<MessageOperationsService> logger)
    {
        _msmqService = msmqService ?? throw new ArgumentNullException(nameof(msmqService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DeleteMessageAsync(
        string queuePath,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(queuePath))
        {
            throw new ArgumentNullException(nameof(queuePath));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentNullException(nameof(messageId));
        }

        _logger.LogInformation(
            "Deleting message {MessageId} from queue {QueuePath}",
            messageId,
            queuePath);

        try
        {
            // Delete the message directly - let MSMQ handle any queue existence issues
            var deleteResult = await _msmqService.DeleteMessageAsync(queuePath, messageId, cancellationToken);

            if (deleteResult.Success)
            {
                _logger.LogInformation(
                    "Successfully deleted message {MessageId} from queue {QueuePath}",
                    messageId,
                    queuePath);

                deleteResult.Metadata["DeletedAt"] = DateTime.UtcNow;
                deleteResult.Metadata["MessageId"] = messageId;
                deleteResult.Metadata["QueuePath"] = queuePath;
            }
            else
            {
                _logger.LogError(
                    "Failed to delete message {MessageId} from queue {QueuePath}: {Error}",
                    messageId,
                    queuePath,
                    deleteResult.ErrorMessage);
            }

            return deleteResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error deleting message {MessageId} from queue {QueuePath}",
                messageId,
                queuePath);
            return OperationResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> MoveMessageAsync(
        string sourceQueuePath,
        string destinationQueuePath,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(sourceQueuePath))
        {
            throw new ArgumentNullException(nameof(sourceQueuePath));
        }

        if (string.IsNullOrWhiteSpace(destinationQueuePath))
        {
            throw new ArgumentNullException(nameof(destinationQueuePath));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentNullException(nameof(messageId));
        }

        _logger.LogInformation(
            "Moving message {MessageId} from {SourceQueue} to {DestinationQueue}",
            messageId,
            sourceQueuePath,
            destinationQueuePath);

        try
        {
            // Validate both queues exist
            var sourceExistsResult = await _msmqService.QueueExistsAsync(sourceQueuePath, cancellationToken);
            if (!sourceExistsResult.Success || sourceExistsResult.Data == false)
            {
                _logger.LogWarning("Source queue {QueuePath} does not exist or is not accessible", sourceQueuePath);
                return OperationResult.Failure($"Source queue '{sourceQueuePath}' does not exist or is not accessible");
            }

            var destExistsResult = await _msmqService.QueueExistsAsync(destinationQueuePath, cancellationToken);
            if (!destExistsResult.Success || destExistsResult.Data == false)
            {
                _logger.LogWarning("Destination queue {QueuePath} does not exist or is not accessible", destinationQueuePath);
                return OperationResult.Failure($"Destination queue '{destinationQueuePath}' does not exist or is not accessible");
            }

            // Move the message
            var moveResult = await _msmqService.MoveMessageAsync(
                sourceQueuePath,
                destinationQueuePath,
                messageId,
                cancellationToken);

            if (moveResult.Success)
            {
                _logger.LogInformation(
                    "Successfully moved message {MessageId} from {SourceQueue} to {DestinationQueue}",
                    messageId,
                    sourceQueuePath,
                    destinationQueuePath);

                moveResult.Metadata["MovedAt"] = DateTime.UtcNow;
                moveResult.Metadata["MessageId"] = messageId;
                moveResult.Metadata["SourceQueue"] = sourceQueuePath;
                moveResult.Metadata["DestinationQueue"] = destinationQueuePath;
            }
            else
            {
                _logger.LogError(
                    "Failed to move message {MessageId}: {Error}",
                    messageId,
                    moveResult.ErrorMessage);
            }

            return moveResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error moving message {MessageId} from {SourceQueue} to {DestinationQueue}",
                messageId,
                sourceQueuePath,
                destinationQueuePath);
            return OperationResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ResendMessageAsync(
        string sourceQueuePath,
        string destinationQueuePath,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(sourceQueuePath))
        {
            throw new ArgumentNullException(nameof(sourceQueuePath));
        }

        if (string.IsNullOrWhiteSpace(destinationQueuePath))
        {
            throw new ArgumentNullException(nameof(destinationQueuePath));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentNullException(nameof(messageId));
        }

        _logger.LogInformation(
            "Resending message {MessageId} from {SourceQueue} to {DestinationQueue}",
            messageId,
            sourceQueuePath,
            destinationQueuePath);

        try
        {
            // Validate both queues exist
            var sourceExistsResult = await _msmqService.QueueExistsAsync(sourceQueuePath, cancellationToken);
            if (!sourceExistsResult.Success || sourceExistsResult.Data == false)
            {
                _logger.LogWarning("Source queue {QueuePath} does not exist or is not accessible", sourceQueuePath);
                return OperationResult.Failure($"Source queue '{sourceQueuePath}' does not exist or is not accessible");
            }

            var destExistsResult = await _msmqService.QueueExistsAsync(destinationQueuePath, cancellationToken);
            if (!destExistsResult.Success || destExistsResult.Data == false)
            {
                _logger.LogWarning("Destination queue {QueuePath} does not exist or is not accessible", destinationQueuePath);
                return OperationResult.Failure($"Destination queue '{destinationQueuePath}' does not exist or is not accessible");
            }

            // Get the message
            var messageResult = await _msmqService.GetMessageByIdAsync(
                sourceQueuePath,
                messageId,
                peekOnly: true,
                cancellationToken);

            if (!messageResult.Success)
            {
                _logger.LogError(
                    "Failed to retrieve message {MessageId} from {QueuePath}: {Error}",
                    messageId,
                    sourceQueuePath,
                    messageResult.ErrorMessage);
                return OperationResult.Failure(
                    $"Failed to retrieve message: {messageResult.ErrorMessage}",
                    messageResult.Exception);
            }

            if (messageResult.Data == null)
            {
                _logger.LogWarning("Message {MessageId} not found in queue {QueuePath}", messageId, sourceQueuePath);
                return OperationResult.Failure($"Message '{messageId}' not found in queue '{sourceQueuePath}'");
            }

            // Create a copy of the message (new ID will be assigned when sent)
            var messageCopy = messageResult.Data;

            // Send the copy to the destination queue
            var sendResult = await _msmqService.SendMessageAsync(
                destinationQueuePath,
                messageCopy,
                cancellationToken);

            if (sendResult.Success)
            {
                _logger.LogInformation(
                    "Successfully resent message {MessageId} from {SourceQueue} to {DestinationQueue}",
                    messageId,
                    sourceQueuePath,
                    destinationQueuePath);

                sendResult.Metadata["ResentAt"] = DateTime.UtcNow;
                sendResult.Metadata["OriginalMessageId"] = messageId;
                sendResult.Metadata["SourceQueue"] = sourceQueuePath;
                sendResult.Metadata["DestinationQueue"] = destinationQueuePath;
            }
            else
            {
                _logger.LogError(
                    "Failed to resend message {MessageId}: {Error}",
                    messageId,
                    sendResult.ErrorMessage);
            }

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error resending message {MessageId} from {SourceQueue} to {DestinationQueue}",
                messageId,
                sourceQueuePath,
                destinationQueuePath);
            return OperationResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ExportMessageAsync(
        string queuePath,
        string messageId,
        string filePath,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(queuePath))
        {
            throw new ArgumentNullException(nameof(queuePath));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentNullException(nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        _logger.LogInformation(
            "Exporting message {MessageId} from {QueuePath} to {FilePath} as {Format}",
            messageId,
            queuePath,
            filePath,
            format);

        try
        {
            // Validate queue exists
            var queueExistsResult = await _msmqService.QueueExistsAsync(queuePath, cancellationToken);
            if (!queueExistsResult.Success || queueExistsResult.Data == false)
            {
                _logger.LogWarning("Queue {QueuePath} does not exist or is not accessible", queuePath);
                return OperationResult.Failure($"Queue '{queuePath}' does not exist or is not accessible");
            }

            // Get the message
            var messageResult = await _msmqService.GetMessageByIdAsync(
                queuePath,
                messageId,
                peekOnly: true,
                cancellationToken);

            if (!messageResult.Success)
            {
                _logger.LogError(
                    "Failed to retrieve message {MessageId}: {Error}",
                    messageId,
                    messageResult.ErrorMessage);
                return OperationResult.Failure(
                    $"Failed to retrieve message: {messageResult.ErrorMessage}",
                    messageResult.Exception);
            }

            if (messageResult.Data == null)
            {
                _logger.LogWarning("Message {MessageId} not found in queue {QueuePath}", messageId, queuePath);
                return OperationResult.Failure($"Message '{messageId}' not found");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Export the message based on format
            string exportedContent = format switch
            {
                ExportFormat.Json => ExportAsJson(messageResult.Data),
                ExportFormat.Xml => ExportAsXml(messageResult.Data),
                ExportFormat.Csv => ExportAsCsv(new[] { messageResult.Data }),
                ExportFormat.Text => ExportAsText(messageResult.Data),
                ExportFormat.Binary => ExportAsBinary(messageResult.Data),
                _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
            };

            // Write to file
            if (format == ExportFormat.Binary && messageResult.Data.Body?.RawBytes != null)
            {
                await File.WriteAllBytesAsync(filePath, messageResult.Data.Body.RawBytes, cancellationToken);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, exportedContent, Encoding.UTF8, cancellationToken);
            }

            var fileInfo = new FileInfo(filePath);

            _logger.LogInformation(
                "Successfully exported message {MessageId} to {FilePath} ({FileSize} bytes)",
                messageId,
                filePath,
                fileInfo.Length);

            var result = OperationResult.Successful();
            result.Metadata["ExportedAt"] = DateTime.UtcNow;
            result.Metadata["MessageId"] = messageId;
            result.Metadata["FilePath"] = filePath;
            result.Metadata["FileSize"] = fileInfo.Length;
            result.Metadata["Format"] = format.ToString();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error exporting message {MessageId} to {FilePath}",
                messageId,
                filePath);
            return OperationResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ExportMessagesAsync(
        string queuePath,
        IEnumerable<string> messageIds,
        string filePath,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(queuePath))
        {
            throw new ArgumentNullException(nameof(queuePath));
        }

        if (messageIds == null || !messageIds.Any())
        {
            throw new ArgumentNullException(nameof(messageIds));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (format == ExportFormat.Binary)
        {
            return OperationResult.Failure("Binary format is not supported for bulk exports");
        }

        var messageIdList = messageIds.ToList();

        _logger.LogInformation(
            "Exporting {Count} messages from {QueuePath} to {FilePath} as {Format}",
            messageIdList.Count,
            queuePath,
            filePath,
            format);

        try
        {
            // Validate queue exists
            var queueExistsResult = await _msmqService.QueueExistsAsync(queuePath, cancellationToken);
            if (!queueExistsResult.Success || queueExistsResult.Data == false)
            {
                _logger.LogWarning("Queue {QueuePath} does not exist or is not accessible", queuePath);
                return OperationResult.Failure($"Queue '{queuePath}' does not exist or is not accessible");
            }

            // Retrieve all messages
            var messages = new List<QueueMessage>();
            var failedIds = new List<string>();

            foreach (var messageId in messageIdList)
            {
                var messageResult = await _msmqService.GetMessageByIdAsync(
                    queuePath,
                    messageId,
                    peekOnly: true,
                    cancellationToken);

                if (messageResult.Success && messageResult.Data != null)
                {
                    messages.Add(messageResult.Data);
                }
                else
                {
                    failedIds.Add(messageId);
                    _logger.LogWarning(
                        "Failed to retrieve message {MessageId} for export: {Error}",
                        messageId,
                        messageResult.ErrorMessage);
                }
            }

            if (messages.Count == 0)
            {
                _logger.LogWarning("No messages could be retrieved for export");
                return OperationResult.Failure("No messages could be retrieved for export");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Export messages based on format
            string exportedContent = format switch
            {
                ExportFormat.Json => ExportAsJsonArray(messages),
                ExportFormat.Xml => ExportAsXmlCollection(messages),
                ExportFormat.Csv => ExportAsCsv(messages),
                ExportFormat.Text => ExportAsTextMultiple(messages),
                _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
            };

            // Write to file
            await File.WriteAllTextAsync(filePath, exportedContent, Encoding.UTF8, cancellationToken);

            var fileInfo = new FileInfo(filePath);

            _logger.LogInformation(
                "Successfully exported {Count} messages to {FilePath} ({FileSize} bytes). {FailedCount} messages failed.",
                messages.Count,
                filePath,
                fileInfo.Length,
                failedIds.Count);

            var result = OperationResult.Successful();
            result.Metadata["ExportedAt"] = DateTime.UtcNow;
            result.Metadata["FilePath"] = filePath;
            result.Metadata["FileSize"] = fileInfo.Length;
            result.Metadata["Format"] = format.ToString();
            result.Metadata["MessageCount"] = messages.Count;
            result.Metadata["FailedCount"] = failedIds.Count;

            if (failedIds.Count > 0)
            {
                result.Metadata["FailedMessageIds"] = failedIds;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error exporting messages to {FilePath}",
                filePath);
            return OperationResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> PurgeQueueAsync(
        string queuePath,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(queuePath))
        {
            throw new ArgumentNullException(nameof(queuePath));
        }

        _logger.LogWarning(
            "PURGING QUEUE: {QueuePath} - This will delete all messages!",
            queuePath);

        try
        {
            // Validate queue exists
            var queueExistsResult = await _msmqService.QueueExistsAsync(queuePath, cancellationToken);
            if (!queueExistsResult.Success || queueExistsResult.Data == false)
            {
                _logger.LogWarning("Queue {QueuePath} does not exist or is not accessible", queuePath);
                return OperationResult.Failure($"Queue '{queuePath}' does not exist or is not accessible");
            }

            // Get message count before purging
            var countResult = await _msmqService.GetMessageCountAsync(queuePath, cancellationToken);
            int messageCount = countResult.Success ? countResult.Data : 0;

            // Purge the queue
            var purgeResult = await _msmqService.PurgeQueueAsync(queuePath, cancellationToken);

            if (purgeResult.Success)
            {
                _logger.LogWarning(
                    "Successfully purged {Count} messages from queue {QueuePath}",
                    messageCount,
                    queuePath);

                purgeResult.Metadata["PurgedAt"] = DateTime.UtcNow;
                purgeResult.Metadata["QueuePath"] = queuePath;
                purgeResult.Metadata["MessageCount"] = messageCount;
            }
            else
            {
                _logger.LogError(
                    "Failed to purge queue {QueuePath}: {Error}",
                    queuePath,
                    purgeResult.ErrorMessage);
            }

            return purgeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error purging queue {QueuePath}",
                queuePath);
            return OperationResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DeleteMessagesAsync(
        string queuePath,
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(queuePath))
        {
            throw new ArgumentNullException(nameof(queuePath));
        }

        if (messageIds == null || !messageIds.Any())
        {
            throw new ArgumentNullException(nameof(messageIds));
        }

        var messageIdList = messageIds.ToList();

        _logger.LogInformation(
            "Deleting {Count} messages from queue {QueuePath}",
            messageIdList.Count,
            queuePath);

        try
        {
            // Validate queue exists
            var queueExistsResult = await _msmqService.QueueExistsAsync(queuePath, cancellationToken);
            if (!queueExistsResult.Success || queueExistsResult.Data == false)
            {
                _logger.LogWarning("Queue {QueuePath} does not exist or is not accessible", queuePath);
                return OperationResult.Failure($"Queue '{queuePath}' does not exist or is not accessible");
            }

            // Delete messages
            int successCount = 0;
            var failedIds = new List<string>();

            foreach (var messageId in messageIdList)
            {
                var deleteResult = await _msmqService.DeleteMessageAsync(queuePath, messageId, cancellationToken);

                if (deleteResult.Success)
                {
                    successCount++;
                }
                else
                {
                    failedIds.Add(messageId);
                    _logger.LogWarning(
                        "Failed to delete message {MessageId}: {Error}",
                        messageId,
                        deleteResult.ErrorMessage);
                }
            }

            _logger.LogInformation(
                "Deleted {SuccessCount} of {TotalCount} messages from queue {QueuePath}. {FailedCount} failed.",
                successCount,
                messageIdList.Count,
                queuePath,
                failedIds.Count);

            var result = OperationResult.Successful();
            result.Metadata["DeletedAt"] = DateTime.UtcNow;
            result.Metadata["QueuePath"] = queuePath;
            result.Metadata["SuccessCount"] = successCount;
            result.Metadata["FailedCount"] = failedIds.Count;
            result.Metadata["TotalCount"] = messageIdList.Count;

            if (failedIds.Count > 0)
            {
                result.Metadata["FailedMessageIds"] = failedIds;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error deleting messages from queue {QueuePath}",
                queuePath);
            return OperationResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    #region Export Helpers

    private static string ExportAsJson(QueueMessage message)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(message, options);
    }

    private static string ExportAsJsonArray(IEnumerable<QueueMessage> messages)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(messages, options);
    }

    private static string ExportAsXml(QueueMessage message)
    {
        var root = new XElement("Message",
            new XElement("Id", message.Id),
            new XElement("Label", message.Label),
            new XElement("QueuePath", message.QueuePath),
            new XElement("Priority", message.Priority.ToString()),
            new XElement("ArrivedTime", message.ArrivedTime.ToString("O")),
            new XElement("SentTime", message.SentTime.ToString("O")),
            new XElement("CorrelationId", message.CorrelationId ?? string.Empty),
            new XElement("Recoverable", message.Recoverable),
            new XElement("Authenticated", message.Authenticated),
            new XElement("Body",
                new XElement("Format", message.Body?.Format.ToString() ?? "Unknown"),
                new XElement("Content", message.Body?.RawContent ?? string.Empty),
                new XElement("Encoding", message.Body?.Encoding ?? "UTF-8")
            )
        );

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string ExportAsXmlCollection(IEnumerable<QueueMessage> messages)
    {
        var root = new XElement("Messages");

        foreach (var message in messages)
        {
            root.Add(new XElement("Message",
                new XElement("Id", message.Id),
                new XElement("Label", message.Label),
                new XElement("QueuePath", message.QueuePath),
                new XElement("Priority", message.Priority.ToString()),
                new XElement("ArrivedTime", message.ArrivedTime.ToString("O")),
                new XElement("SentTime", message.SentTime.ToString("O")),
                new XElement("CorrelationId", message.CorrelationId ?? string.Empty),
                new XElement("Recoverable", message.Recoverable),
                new XElement("Authenticated", message.Authenticated),
                new XElement("Body",
                    new XElement("Format", message.Body?.Format.ToString() ?? "Unknown"),
                    new XElement("Content", message.Body?.RawContent ?? string.Empty),
                    new XElement("Encoding", message.Body?.Encoding ?? "UTF-8")
                )
            ));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string ExportAsCsv(IEnumerable<QueueMessage> messages)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Id,Label,QueuePath,Priority,ArrivedTime,SentTime,CorrelationId,Recoverable,Authenticated,BodyFormat,BodySize,BodyPreview");

        // Rows
        foreach (var message in messages)
        {
            var bodyPreview = message.Body?.RawContent?.Length > 100
                ? message.Body.RawContent.Substring(0, 100).Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ")
                : message.Body?.RawContent?.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") ?? string.Empty;

            sb.AppendLine($"\"{message.Id}\"," +
                         $"\"{message.Label}\"," +
                         $"\"{message.QueuePath}\"," +
                         $"\"{message.Priority}\"," +
                         $"\"{message.ArrivedTime:O}\"," +
                         $"\"{message.SentTime:O}\"," +
                         $"\"{message.CorrelationId ?? string.Empty}\"," +
                         $"{message.Recoverable}," +
                         $"{message.Authenticated}," +
                         $"\"{message.Body?.Format ?? MessageBodyFormat.Unknown}\"," +
                         $"{message.Body?.SizeBytes ?? 0}," +
                         $"\"{bodyPreview}\"");
        }

        return sb.ToString();
    }

    private static string ExportAsText(QueueMessage message)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Message Details ===");
        sb.AppendLine($"ID: {message.Id}");
        sb.AppendLine($"Label: {message.Label}");
        sb.AppendLine($"Queue: {message.QueuePath}");
        sb.AppendLine($"Priority: {message.Priority} ({message.PriorityText})");
        sb.AppendLine($"Arrived: {message.ArrivedTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Sent: {message.SentTime:yyyy-MM-dd HH:mm:ss.fff}");
        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            sb.AppendLine($"Correlation ID: {message.CorrelationId}");
        }
        sb.AppendLine($"Recoverable: {message.Recoverable}");
        sb.AppendLine($"Authenticated: {message.Authenticated}");
        sb.AppendLine();
        sb.AppendLine("=== Message Body ===");
        sb.AppendLine($"Format: {message.Body?.Format ?? MessageBodyFormat.Unknown}");
        sb.AppendLine($"Size: {message.FormattedSize}");
        sb.AppendLine($"Encoding: {message.Body?.Encoding ?? "UTF-8"}");
        sb.AppendLine();
        sb.AppendLine("Content:");
        sb.AppendLine(message.Body?.GetFormattedContent() ?? "(empty)");

        return sb.ToString();
    }

    private static string ExportAsTextMultiple(IEnumerable<QueueMessage> messages)
    {
        var sb = new StringBuilder();
        int count = 1;

        foreach (var message in messages)
        {
            sb.AppendLine($"--- Message {count++} ---");
            sb.AppendLine(ExportAsText(message));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string ExportAsBinary(QueueMessage message)
    {
        // For binary export, we return the raw content as-is
        // The actual file writing is handled in ExportMessageAsync
        return message.Body?.RawContent ?? string.Empty;
    }

    #endregion
}
