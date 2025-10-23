namespace MsMqApp.Models.Enums;

/// <summary>
/// Represents the type of MSMQ queue
/// </summary>
public enum QueueType
{
    /// <summary>
    /// Application-created private queue
    /// </summary>
    Private,

    /// <summary>
    /// Public queue registered in Active Directory
    /// </summary>
    Public,

    /// <summary>
    /// System queue (e.g., Dead Letter, Journal)
    /// </summary>
    System,

    /// <summary>
    /// Journal queue for a specific queue
    /// </summary>
    Journal,

    /// <summary>
    /// Dead letter queue
    /// </summary>
    DeadLetter,

    /// <summary>
    /// Transactional dead letter queue
    /// </summary>
    TransactionalDeadLetter
}
