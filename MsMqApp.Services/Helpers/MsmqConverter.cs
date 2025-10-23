using Experimental.System.Messaging;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Services.Helpers;

/// <summary>
/// Helper class for converting between System.Messaging types and domain models
/// </summary>
internal static class MsmqConverter
{
    /// <summary>
    /// Converts a System.Messaging.MessageQueue to QueueInfo domain model
    /// </summary>
    public static QueueInfo ToQueueInfo(MessageQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        var queueInfo = new QueueInfo
        {
            Id = Guid.NewGuid().ToString(),
            Path = queue.Path,
            FormatName = queue.FormatName ?? string.Empty,
            IsLocal = IsLocalQueue(queue.MachineName)
        };

        try
        {
            queueInfo.Name = ExtractQueueName(queue.Path);
            queueInfo.ComputerName = queue.MachineName ?? ".";
            queueInfo.QueueType = DetermineQueueType(queue.Path);
            queueInfo.CanRead = queue.CanRead;
            queueInfo.CanWrite = queue.CanWrite;
            queueInfo.IsTransactional = queue.Transactional;

            // Try to get message count
            try
            {
                var messages = queue.GetAllMessages();
                queueInfo.MessageCount = messages.Length;
            }
            catch
            {
                queueInfo.MessageCount = 0;
            }

            // Try to get additional properties
            if (!string.IsNullOrEmpty(queue.Label))
            {
                queueInfo.Label = queue.Label;
            }

            queueInfo.CreateTime = queue.CreateTime;
            queueInfo.LastModifiedTime = queue.LastModifyTime;
            queueInfo.UseJournalQueue = queue.UseJournalQueue;
            queueInfo.Authenticate = queue.Authenticate;
            queueInfo.BasePriority = (int)queue.BasePriority;
            queueInfo.MaximumQueueSize = queue.MaximumQueueSize;
            queueInfo.MaximumJournalSize = queue.MaximumJournalSize;
            queueInfo.IsAccessible = true;
        }
        catch (Exception ex)
        {
            queueInfo.IsAccessible = false;
            queueInfo.ErrorMessage = ex.Message;
        }

        return queueInfo;
    }

    /// <summary>
    /// Converts a System.Messaging.Message to QueueMessage domain model
    /// </summary>
    public static QueueMessage ToQueueMessage(Message message, string queuePath)
    {
        ArgumentNullException.ThrowIfNull(message);

        var queueMessage = new QueueMessage
        {
            Id = message.Id,
            QueuePath = queuePath,
            ArrivedTime = message.ArrivedTime,
            SentTime = message.SentTime,
            Label = message.Label ?? string.Empty,
            Priority = ConvertPriority(message.Priority),
            Recoverable = message.Recoverable,
            TimeToBeReceived = message.TimeToBeReceived,
            TimeToReachQueue = message.TimeToReachQueue,
            CorrelationId = message.CorrelationId ?? string.Empty,
            UseJournalQueue = message.UseJournalQueue,
            UseDeadLetterQueue = message.UseDeadLetterQueue,
            UseTracing = message.UseTracing,
            LookupId = message.LookupId,
            AppSpecific = message.AppSpecific,
            IsTransactional = false // Property not available in Experimental.System.Messaging
        };

        // Optional properties
        if (message.ResponseQueue != null)
        {
            queueMessage.ResponseQueue = message.ResponseQueue.Path;
        }

        if (message.AdministrationQueue != null)
        {
            queueMessage.AdministrationQueue = message.AdministrationQueue.Path;
        }

        // Safe property access with error handling for potentially inaccessible properties
        try
        {
            queueMessage.SenderId = message.SenderId;
        }
        catch
        {
            queueMessage.SenderId = null;
        }

        try
        {
            queueMessage.SenderCertificate = message.SenderCertificate;
        }
        catch
        {
            queueMessage.SenderCertificate = null;
        }

        try
        {
            queueMessage.SourceMachine = message.SourceMachine;
        }
        catch
        {
            queueMessage.SourceMachine = string.Empty;
        }

        try
        {
            queueMessage.Extension = message.Extension;
        }
        catch
        {
            queueMessage.Extension = null;
        }

        try
        {
            queueMessage.DigitalSignature = message.DigitalSignature;
        }
        catch
        {
            queueMessage.DigitalSignature = null;
        }

        try
        {
            queueMessage.HashAlgorithm = (int)message.HashAlgorithm;
        }
        catch
        {
            queueMessage.HashAlgorithm = 0;
        }

        try
        {
            queueMessage.EncryptionAlgorithm = (int)message.EncryptionAlgorithm;
        }
        catch
        {
            queueMessage.EncryptionAlgorithm = 0;
        }

        queueMessage.Authenticated = false; // Property not available in Experimental.System.Messaging

        // Convert message body
        queueMessage.Body = ExtractMessageBody(message);

        return queueMessage;
    }

    private static MessageBody ExtractMessageBody(Message message)
    {
        var messageBody = new MessageBody();

        try
        {
            // Try to get the body as bytes first
            if (message.BodyStream != null && message.BodyStream.Length > 0)
            {
                message.BodyStream.Position = 0;
                var bytes = new byte[message.BodyStream.Length];
                var totalBytesRead = 0;
                int bytesRead;
                
                // Read all bytes with proper handling of partial reads
                while (totalBytesRead < bytes.Length && 
                       (bytesRead = message.BodyStream.Read(bytes, totalBytesRead, bytes.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                }
                
                // Trim array if we didn't read the expected amount
                if (totalBytesRead < bytes.Length)
                {
                    Array.Resize(ref bytes, totalBytesRead);
                }
                
                messageBody.RawBytes = bytes;

                // Try to convert to string
                try
                {
                    messageBody.RawContent = System.Text.Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    // If conversion fails, it's likely binary
                    messageBody.Format = MessageBodyFormat.Binary;
                }
            }
            else if (message.Body != null)
            {
                messageBody.RawContent = message.Body.ToString() ?? string.Empty;
            }
        }
        catch
        {
            messageBody.RawContent = string.Empty;
        }

        return messageBody;
    }

    private static string ExtractQueueName(string queuePath)
    {
        if (string.IsNullOrEmpty(queuePath))
            return string.Empty;

        // Extract name from path like ".\private$\MyQueue"
        var lastSlash = queuePath.LastIndexOf('\\');
        return lastSlash >= 0 ? queuePath.Substring(lastSlash + 1) : queuePath;
    }

    private static QueueType DetermineQueueType(string queuePath)
    {
        if (string.IsNullOrEmpty(queuePath))
            return QueueType.Private;

        var lowerPath = queuePath.ToLowerInvariant();

        if (lowerPath.Contains("private$"))
            return QueueType.Private;

        if (lowerPath.Contains("deadletter") || lowerPath.Contains("xactdeadletter"))
            return lowerPath.Contains("xact") ? QueueType.TransactionalDeadLetter : QueueType.DeadLetter;

        if (lowerPath.Contains("journal"))
            return QueueType.Journal;

        if (lowerPath.Contains("system"))
            return QueueType.System;

        return QueueType.Public;
    }

    private static Models.Enums.MessagePriority ConvertPriority(Experimental.System.Messaging.MessagePriority msmqPriority)
    {
        return msmqPriority switch
        {
            Experimental.System.Messaging.MessagePriority.Lowest => Models.Enums.MessagePriority.Lowest,
            Experimental.System.Messaging.MessagePriority.VeryLow => Models.Enums.MessagePriority.VeryLow,
            Experimental.System.Messaging.MessagePriority.Low => Models.Enums.MessagePriority.Low,
            Experimental.System.Messaging.MessagePriority.Normal => Models.Enums.MessagePriority.Normal,
            Experimental.System.Messaging.MessagePriority.AboveNormal => Models.Enums.MessagePriority.AboveNormal,
            Experimental.System.Messaging.MessagePriority.High => Models.Enums.MessagePriority.High,
            Experimental.System.Messaging.MessagePriority.VeryHigh => Models.Enums.MessagePriority.VeryHigh,
            Experimental.System.Messaging.MessagePriority.Highest => Models.Enums.MessagePriority.Highest,
            _ => Models.Enums.MessagePriority.Normal
        };
    }

    private static bool IsLocalQueue(string machineName)
    {
        if (string.IsNullOrEmpty(machineName))
            return true;

        return machineName == "." ||
               machineName.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               machineName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }
}
