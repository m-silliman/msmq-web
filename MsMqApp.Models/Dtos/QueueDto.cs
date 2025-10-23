using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Models.Dtos;

/// <summary>
/// Data transfer object representing an MSMQ queue for API/UI communication
/// </summary>
public class QueueDto
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue path (e.g., .\private$\myqueue)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message count in the queue
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the queue type
    /// </summary>
    public string QueueType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the queue is transactional
    /// </summary>
    public bool IsTransactional { get; set; }

    /// <summary>
    /// Gets or sets the computer name where the queue resides
    /// </summary>
    public string ComputerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the queue is accessible
    /// </summary>
    public bool IsAccessible { get; set; } = true;

    /// <summary>
    /// Gets or sets any error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a DTO from a domain model
    /// </summary>
    public static QueueDto FromDomain(QueueInfo queueInfo)
    {
        return new QueueDto
        {
            Id = queueInfo.Id,
            Name = queueInfo.Name,
            Path = queueInfo.Path,
            DisplayName = queueInfo.DisplayName,
            MessageCount = queueInfo.MessageCount,
            QueueType = queueInfo.QueueType.ToString(),
            IsTransactional = queueInfo.IsTransactional,
            ComputerName = queueInfo.ComputerName,
            IsAccessible = queueInfo.IsAccessible,
            ErrorMessage = queueInfo.ErrorMessage
        };
    }

    /// <summary>
    /// Converts this DTO to a domain model
    /// </summary>
    public QueueInfo ToDomain()
    {
        return new QueueInfo
        {
            Id = Id,
            Name = Name,
            Path = Path,
            MessageCount = MessageCount,
            QueueType = Enum.TryParse<QueueType>(QueueType, out var qt) ? qt : Enums.QueueType.Private,
            IsTransactional = IsTransactional,
            ComputerName = ComputerName,
            IsAccessible = IsAccessible,
            ErrorMessage = ErrorMessage
        };
    }
}
