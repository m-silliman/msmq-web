using MsMqApp.Models.Enums;

namespace MsMqApp.Models.Domain;

/// <summary>
/// Represents an MSMQ message with all its properties
/// </summary>
public class QueueMessage
{
    /// <summary>
    /// Gets or sets the unique message identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message body
    /// </summary>
    public MessageBody Body { get; set; } = new MessageBody();

    /// <summary>
    /// Gets or sets the queue path this message belongs to
    /// </summary>
    public string QueuePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message priority
    /// </summary>
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;

    /// <summary>
    /// Gets or sets when the message arrived in the queue
    /// </summary>
    public DateTime ArrivedTime { get; set; }

    /// <summary>
    /// Gets or sets when the message was sent
    /// </summary>
    public DateTime SentTime { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response queue path
    /// </summary>
    public string? ResponseQueue { get; set; }

    /// <summary>
    /// Gets or sets the administration queue path
    /// </summary>
    public string? AdministrationQueue { get; set; }

    /// <summary>
    /// Gets or sets the application-specific information
    /// </summary>
    public int AppSpecific { get; set; }

    /// <summary>
    /// Gets or sets the sender identifier
    /// </summary>
    public byte[]? SenderId { get; set; }

    /// <summary>
    /// Gets or sets the sender certificate
    /// </summary>
    public byte[]? SenderCertificate { get; set; }

    /// <summary>
    /// Gets or sets the source machine GUID
    /// </summary>
    public string? SourceMachine { get; set; }

    /// <summary>
    /// Gets or sets whether the message is recoverable
    /// </summary>
    public bool Recoverable { get; set; }

    /// <summary>
    /// Gets or sets the message time to be received (timeout)
    /// </summary>
    public TimeSpan TimeToBeReceived { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Gets or sets the message time to reach queue
    /// </summary>
    public TimeSpan TimeToReachQueue { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Gets or sets whether delivery acknowledgment is requested
    /// </summary>
    public bool AcknowledgeRequired { get; set; }

    /// <summary>
    /// Gets or sets the acknowledgment type
    /// </summary>
    public int AcknowledgeType { get; set; }

    /// <summary>
    /// Gets or sets whether the message is authenticated
    /// </summary>
    public bool Authenticated { get; set; }

    /// <summary>
    /// Gets or sets the encryption algorithm
    /// </summary>
    public int EncryptionAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets whether the message uses encryption
    /// </summary>
    public bool UseEncryption { get; set; }

    /// <summary>
    /// Gets or sets whether the message is part of a transaction
    /// </summary>
    public bool IsTransactional { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets whether to use journaling
    /// </summary>
    public bool UseJournalQueue { get; set; }

    /// <summary>
    /// Gets or sets whether to use dead letter queue
    /// </summary>
    public bool UseDeadLetterQueue { get; set; }

    /// <summary>
    /// Gets or sets whether to use tracing
    /// </summary>
    public bool UseTracing { get; set; }

    /// <summary>
    /// Gets or sets the extension data
    /// </summary>
    public byte[]? Extension { get; set; }

    /// <summary>
    /// Gets or sets the lookup identifier (unique identifier assigned by queue)
    /// </summary>
    public long LookupId { get; set; }

    /// <summary>
    /// Gets or sets the message type (Normal, Acknowledgment, Report)
    /// </summary>
    public int MessageType { get; set; }

    /// <summary>
    /// Gets or sets the hash algorithm used
    /// </summary>
    public int HashAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the digital signature
    /// </summary>
    public byte[]? DigitalSignature { get; set; }

    /// <summary>
    /// Gets the age of the message (time since arrival)
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - ArrivedTime.ToUniversalTime();

    /// <summary>
    /// Gets whether the message has expired
    /// </summary>
    public bool IsExpired
    {
        get
        {
            if (TimeToBeReceived == TimeSpan.MaxValue)
                return false;

            return Age > TimeToBeReceived;
        }
    }

    /// <summary>
    /// Gets a formatted string of the message size
    /// </summary>
    public string FormattedSize
    {
        get
        {
            var bytes = Body.SizeBytes;
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }
    }

    /// <summary>
    /// Gets a display-friendly priority text
    /// </summary>
    public string PriorityText => Priority switch
    {
        MessagePriority.Lowest => "Lowest (0)",
        MessagePriority.VeryLow => "Very Low (1)",
        MessagePriority.Low => "Low (2)",
        MessagePriority.Normal => "Normal (3)",
        MessagePriority.AboveNormal => "Above Normal (4)",
        MessagePriority.High => "High (5)",
        MessagePriority.VeryHigh => "Very High (6)",
        MessagePriority.Highest => "Highest (7)",
        _ => $"Unknown ({(int)Priority})"
    };

    /// <summary>
    /// Gets whether the message has a response queue
    /// </summary>
    public bool HasResponseQueue => !string.IsNullOrEmpty(ResponseQueue);

    /// <summary>
    /// Gets whether the message has correlation ID
    /// </summary>
    public bool HasCorrelationId => !string.IsNullOrEmpty(CorrelationId);

    /// <summary>
    /// Creates a deep copy of this message (shallow copy of byte arrays)
    /// </summary>
    public QueueMessage Clone()
    {
        return new QueueMessage
        {
            Id = Id,
            Label = Label,
            Body = Body,
            QueuePath = QueuePath,
            Priority = Priority,
            ArrivedTime = ArrivedTime,
            SentTime = SentTime,
            CorrelationId = CorrelationId,
            ResponseQueue = ResponseQueue,
            AdministrationQueue = AdministrationQueue,
            AppSpecific = AppSpecific,
            SenderId = SenderId,
            SenderCertificate = SenderCertificate,
            SourceMachine = SourceMachine,
            Recoverable = Recoverable,
            TimeToBeReceived = TimeToBeReceived,
            TimeToReachQueue = TimeToReachQueue,
            AcknowledgeRequired = AcknowledgeRequired,
            AcknowledgeType = AcknowledgeType,
            Authenticated = Authenticated,
            EncryptionAlgorithm = EncryptionAlgorithm,
            UseEncryption = UseEncryption,
            IsTransactional = IsTransactional,
            TransactionId = TransactionId,
            UseJournalQueue = UseJournalQueue,
            UseDeadLetterQueue = UseDeadLetterQueue,
            UseTracing = UseTracing,
            Extension = Extension,
            LookupId = LookupId,
            MessageType = MessageType,
            HashAlgorithm = HashAlgorithm,
            DigitalSignature = DigitalSignature
        };
    }

    /// <summary>
    /// Returns a string representation of this message
    /// </summary>
    public override string ToString()
    {
        return $"{Label} (ID: {Id}, Priority: {Priority}, Size: {FormattedSize})";
    }
}
