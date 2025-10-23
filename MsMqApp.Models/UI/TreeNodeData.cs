using MsMqApp.Models.Enums;

namespace MsMqApp.Models.UI;

/// <summary>
/// Represents a node in the queue tree view hierarchy.
/// </summary>
public class TreeNodeData
{
    /// <summary>
    /// Gets or sets the unique identifier for this node.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display text for this node.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of queue view this node represents.
    /// </summary>
    public QueueViewType ViewType { get; set; } = QueueViewType.Queue;

    /// <summary>
    /// Gets or sets the badge count (e.g., message count).
    /// Null indicates no badge should be shown.
    /// </summary>
    public int? BadgeCount { get; set; }

    /// <summary>
    /// Gets or sets the secondary badge count (e.g., journal message count for queues).
    /// Null indicates no secondary badge should be shown.
    /// </summary>
    public int? SecondaryBadgeCount { get; set; }

    /// <summary>
    /// Gets or sets the icon CSS class for this node (e.g., "bi bi-folder").
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node is expanded.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node is selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node has children.
    /// Used to determine if expand/collapse icon should be shown.
    /// </summary>
    public bool HasChildren { get; set; }

    /// <summary>
    /// Gets or sets the child nodes of this node.
    /// </summary>
    public List<TreeNodeData> Children { get; set; } = new();

    /// <summary>
    /// Gets or sets the depth level in the tree (0 for root level).
    /// Used for indentation calculation.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets optional data associated with this node.
    /// Can be used to store queue info, connection details, etc.
    /// </summary>
    public object? Data { get; set; }
}
