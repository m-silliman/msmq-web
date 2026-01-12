using MsMqApp.Models.Enums;

namespace MsMqApp.Models.Domain;

/// <summary>
/// Represents an MSMQ queue with its properties and metadata
/// </summary>
public class QueueInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for this queue
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue name (e.g., "MyQueue")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full queue path (e.g., ".\private$\MyQueue" or "MACHINE\private$\MyQueue")
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the format name of the queue
    /// </summary>
    public string FormatName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computer name where the queue resides
    /// </summary>
    public string ComputerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue type (Private, Public, System, etc.)
    /// </summary>
    public QueueType QueueType { get; set; }

    /// <summary>
    /// Gets or sets the current message count in the queue
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Gets or sets whether the queue is transactional
    /// </summary>
    public bool IsTransactional { get; set; }

    /// <summary>
    /// Gets or sets the queue label/description
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue creation time
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// Gets or sets the last modified time
    /// </summary>
    public DateTime? LastModifiedTime { get; set; }

    /// <summary>
    /// Gets or sets whether the queue can be read
    /// </summary>
    public bool CanRead { get; set; }

    /// <summary>
    /// Gets or sets whether the queue can be written to
    /// </summary>
    public bool CanWrite { get; set; }

    /// <summary>
    /// Gets or sets whether this is a local queue
    /// </summary>
    public bool IsLocal { get; set; }

    /// <summary>
    /// Gets or sets whether journaling is enabled
    /// </summary>
    public bool UseJournalQueue { get; set; }

    /// <summary>
    /// Gets or sets the journal queue path (typically queuePath + ";journal")
    /// </summary>
    // public string JournalPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets the journal queue path computed from the main queue path
    /// </summary>
    public string JournalPath
    {
        get
        {
            if (string.IsNullOrEmpty(Path))
                return string.Empty;

            // If it's a format name, append ;JOURNAL
            if (!string.IsNullOrEmpty(FormatName) && 
                (FormatName.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase) ||
                FormatName.StartsWith("PUBLIC=", StringComparison.OrdinalIgnoreCase) ||
                FormatName.StartsWith("PRIVATE=", StringComparison.OrdinalIgnoreCase)))
            {
                return FormatName + ";journal";
            }

            // For path-based names
            if (Path.Contains("\\private$\\"))
            {
                // Replace \private$\ with \private$\journal$\
                return Path.Replace("\\private$\\", "\\private$\\journal$\\");
            }
            else if (Path.EndsWith("\\private$"))
            {
                return Path + "\\journal$";
            }
            else
            {
                // Fallback - append journal$ 
                return Path.TrimEnd('\\') + "\\journal$";
            }
        }
    }

    /// <summary>
    /// Gets or sets the current message count in the journal queue
    /// </summary>
    public int JournalMessageCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether this queue has journaling enabled and accessible
    /// </summary>
    public bool HasJournaling => UseJournalQueue && !string.IsNullOrEmpty(JournalPath);

    /// <summary>
    /// Gets or sets the authentication level required
    /// </summary>
    public bool Authenticate { get; set; }

    /// <summary>
    /// Gets or sets the queue type ID (GUID)
    /// </summary>
    public Guid? TypeId { get; set; }

    /// <summary>
    /// Gets or sets the privacy level (None, Optional, Body)
    /// </summary>
    public int PrivacyLevel { get; set; }

    /// <summary>
    /// Gets or sets whether journal is enabled
    /// </summary>
    public bool JournalEnabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum journal storage limit in KB (0 = unlimited)
    /// </summary>
    public long JournalQuota { get; set; }

    /// <summary>
    /// Gets or sets the base priority for messages in this queue
    /// </summary>
    public int BasePriority { get; set; }

    /// <summary>
    /// Gets or sets the maximum queue size in KB (0 = unlimited)
    /// </summary>
    public long MaximumQueueSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum journal size in KB (0 = unlimited)
    /// </summary>
    public long MaximumJournalSize { get; set; }

    /// <summary>
    /// Gets or sets whether the queue is currently accessible
    /// </summary>
    public bool IsAccessible { get; set; } = true;

    /// <summary>
    /// Gets or sets any error message if the queue is not accessible
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the display name for the UI (combines name and computer)
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(ComputerName) || ComputerName == "." || ComputerName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
        ? Name
        : $"{ComputerName}\\{Name}";

    /// <summary>
    /// Gets a value indicating whether this queue is a system queue
    /// </summary>
    public bool IsSystemQueue => QueueType == QueueType.System ||
                                  QueueType == QueueType.DeadLetter ||
                                  QueueType == QueueType.TransactionalDeadLetter;

    /// <summary>
    /// Gets a value indicating whether this queue is a journal queue
    /// </summary>
    public bool IsJournalQueue => QueueType == QueueType.Journal;

    /// <summary>
    /// Determines if the queue is empty
    /// </summary>
    public bool IsEmpty => MessageCount == 0;

    /// <summary>
    /// Creates a deep copy of this QueueInfo
    /// </summary>
    public QueueInfo Clone()
    {
        return new QueueInfo
        {
            Id = Id,
            Name = Name,
            Path = Path,
            FormatName = FormatName,
            ComputerName = ComputerName,
            QueueType = QueueType,
            MessageCount = MessageCount,
            IsTransactional = IsTransactional,
            Label = Label,
            CreateTime = CreateTime,
            LastModifiedTime = LastModifiedTime,
            CanRead = CanRead,
            CanWrite = CanWrite,
            IsLocal = IsLocal,
            UseJournalQueue = UseJournalQueue,
            JournalMessageCount = JournalMessageCount,
            Authenticate = Authenticate,
            TypeId = TypeId,
            PrivacyLevel = PrivacyLevel,
            JournalEnabled = JournalEnabled,
            JournalQuota = JournalQuota,
            BasePriority = BasePriority,
            MaximumQueueSize = MaximumQueueSize,
            MaximumJournalSize = MaximumJournalSize,
            IsAccessible = IsAccessible,
            ErrorMessage = ErrorMessage
        };
    }

    /// <summary>
    /// Returns a string representation of this queue
    /// </summary>
    public override string ToString()
    {
        return $"{DisplayName} ({MessageCount} messages)";
    }
}
