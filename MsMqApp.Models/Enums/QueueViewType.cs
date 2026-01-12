namespace MsMqApp.Models.Enums;

/// <summary>
/// Represents the type of queue view node in the tree hierarchy.
/// Used to distinguish between folder nodes, queue nodes, and message container nodes.
/// </summary>
public enum QueueViewType
{
    /// <summary>
    /// A folder node (e.g., "Private Queues", "Public Queues")
    /// </summary>
    Folder,

    /// <summary>
    /// A queue node representing an actual MSMQ queue
    /// </summary>
    Queue,

    /// <summary>
    /// A node representing the queue's messages container
    /// </summary>
    QueueMessages,

    /// <summary>
    /// A node representing the queue's journal messages container
    /// </summary>
    JournalMessages,

    /// <summary>
    /// View for displaying queue properties (configuration, permissions, etc.)
    /// </summary>
    QueueProperties
}
