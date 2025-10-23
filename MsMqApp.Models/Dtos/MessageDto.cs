using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Models.Dtos;

/// <summary>
/// Data transfer object representing an MSMQ message for API/UI communication
/// </summary>
public class MessageDto
{
    /// <summary>
    /// Gets or sets the message ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message body as a string
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message body format (XML, JSON, Text, Binary)
    /// </summary>
    public string BodyFormat { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message priority (0-7)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the priority text
    /// </summary>
    public string PriorityText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message arrival time
    /// </summary>
    public DateTime ArrivedTime { get; set; }

    /// <summary>
    /// Gets or sets the message sent time
    /// </summary>
    public DateTime SentTime { get; set; }

    /// <summary>
    /// Gets or sets the source queue path
    /// </summary>
    public string QueuePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message size in bytes
    /// </summary>
    public long BodySize { get; set; }

    /// <summary>
    /// Gets or sets the formatted size (e.g., "1.5 KB")
    /// </summary>
    public string FormattedSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets whether the message is recoverable
    /// </summary>
    public bool Recoverable { get; set; }

    /// <summary>
    /// Gets or sets whether the message is expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Creates a DTO from a domain model
    /// </summary>
    public static MessageDto FromDomain(QueueMessage message)
    {
        return new MessageDto
        {
            Id = message.Id,
            Label = message.Label,
            Body = message.Body.RawContent,
            BodyFormat = message.Body.Format.ToString(),
            Priority = (int)message.Priority,
            PriorityText = message.PriorityText,
            ArrivedTime = message.ArrivedTime,
            SentTime = message.SentTime,
            QueuePath = message.QueuePath,
            BodySize = message.Body.SizeBytes,
            FormattedSize = message.FormattedSize,
            CorrelationId = message.CorrelationId,
            Recoverable = message.Recoverable,
            IsExpired = message.IsExpired
        };
    }

    /// <summary>
    /// Converts this DTO to a domain model
    /// </summary>
    public QueueMessage ToDomain()
    {
        return new QueueMessage
        {
            Id = Id,
            Label = Label,
            Body = new MessageBody(Body)
            {
                Format = Enum.TryParse<MessageBodyFormat>(BodyFormat, out var bf) ? bf : MessageBodyFormat.Unknown
            },
            Priority = Enum.IsDefined(typeof(MessagePriority), Priority)
                ? (MessagePriority)Priority
                : MessagePriority.Normal,
            ArrivedTime = ArrivedTime,
            SentTime = SentTime,
            QueuePath = QueuePath,
            CorrelationId = CorrelationId ?? string.Empty,
            Recoverable = Recoverable
        };
    }
}
