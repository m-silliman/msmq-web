using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Models.UI;

/// <summary>
/// Helper class for building tree node structures from queue data.
/// </summary>
public static class QueueTreeBuilder
{
    /// <summary>
    /// Builds a tree node structure from a queue connection.
    /// Organizes queues hierarchically: Private, Public, System, Journal.
    /// </summary>
    /// <param name="connection">The queue connection containing queues.</param>
    /// <param name="expandAll">Whether to expand all nodes by default.</param>
    /// <returns>The root tree node for the connection.</returns>
    public static TreeNodeData BuildTreeFromConnection(QueueConnection connection, bool expandAll = false)
    {
        var rootNode = new TreeNodeData
        {
            Id = connection.Id,
            Text = connection.FormattedDisplayName,
            IconClass = connection.IsLocal ? "bi bi-pc-display" : "bi bi-hdd-network",
            IsExpanded = expandAll,
            HasChildren = true,
            Level = 0,
            Data = connection,
            Children = new List<TreeNodeData>()
        };

        var filteredQueues = connection.FilteredQueues.ToList();

        // Group queues by type
        var privateQueues = filteredQueues.Where(q => q.QueueType == QueueType.Private).ToList();
        var publicQueues = filteredQueues.Where(q => q.QueueType == QueueType.Public).ToList();
        var systemQueues = filteredQueues.Where(q => q.IsSystemQueue && !q.IsJournalQueue).ToList();
        var journalQueues = filteredQueues.Where(q => q.IsJournalQueue).ToList();

        // Add Private Queues folder
        if (privateQueues.Any())
        {
            rootNode.Children.Add(CreateQueueFolder(
                "private",
                "Private Queues",
                privateQueues,
                1,
                expandAll));
        }

        // Add Public Queues folder
        if (publicQueues.Any())
        {
            rootNode.Children.Add(CreateQueueFolder(
                "public",
                "Public Queues",
                publicQueues,
                1,
                expandAll));
        }

        // Add System Queues folder
        if (systemQueues.Any())
        {
            rootNode.Children.Add(CreateQueueFolder(
                "system",
                "System Queues",
                systemQueues,
                1,
                expandAll));
        }

        // Add Journal Queues folder
        if (journalQueues.Any())
        {
            rootNode.Children.Add(CreateQueueFolder(
                "journal",
                "Journal Queues",
                journalQueues,
                1,
                expandAll));
        }

        return rootNode;
    }

    /// <summary>
    /// Creates a folder node for a group of queues.
    /// </summary>
    private static TreeNodeData CreateQueueFolder(
        string idSuffix,
        string folderName,
        List<QueueInfo> queues,
        int level,
        bool expandAll)
    {
        var totalMessages = queues.Sum(q => q.MessageCount);

        var folderNode = new TreeNodeData
        {
            Id = $"folder-{idSuffix}",
            Text = folderName,
            IconClass = "bi bi-folder",
            BadgeCount = totalMessages > 0 ? totalMessages : null,
            IsExpanded = expandAll,
            HasChildren = queues.Any(),
            Level = level,
            Data = null,
            ViewType = QueueViewType.Folder,
            Children = new List<TreeNodeData>()
        };

        // Add individual queue nodes
        foreach (var queue in queues.OrderBy(q => q.Name))
        {
            folderNode.Children.Add(CreateQueueNode(queue, level + 1));
        }

        return folderNode;
    }

    /// <summary>
    /// Creates a tree node for an individual queue with nested message/journal children.
    /// </summary>
    private static TreeNodeData CreateQueueNode(QueueInfo queue, int level)
    {
        var queueNode = new TreeNodeData
        {
            Id = queue.Id,
            Text = queue.Name,
            IconClass = "bi bi-folder", // Queue now shows as folder
            BadgeCount = queue.MessageCount > 0 ? queue.MessageCount : null,
            SecondaryBadgeCount = queue.JournalMessageCount > 0 ? queue.JournalMessageCount : null,
            IsExpanded = false,
            HasChildren = true, // Always has children (queue messages + journal)
            Level = level,
            Data = queue,
            ViewType = QueueViewType.Queue,
            Children = new List<TreeNodeData>()
        };

        // Add "Queue Messages" child node
        var messageQNode = new TreeNodeData
        {
            Id = $"{queue.Id}_messages",
            Text = "Queue Messages",
            IconClass = "bi bi-envelope-fill",
            BadgeCount = queue.MessageCount > 0 ? queue.MessageCount : null,
            IsExpanded = false,
            HasChildren = false,
            Level = level + 1,
            Data = queue,
            ViewType = QueueViewType.QueueMessages
        };
        queueNode.Children.Add(messageQNode);

        var journalQNode = new TreeNodeData
        {
            Id = $"{queue.Id}_journal",
            Text = "Journal Messages",
            IconClass = "bi bi-journal-text",
            BadgeCount = queue.JournalMessageCount > 0 ? queue.JournalMessageCount : null,
            IsExpanded = false,
            HasChildren = false,
            Level = level + 1,        
            Data = queue,
            ViewType = QueueViewType.JournalMessages
        };
        queueNode.Children.Add(journalQNode);

        return queueNode;
    }

    /// <summary>
    /// Gets the appropriate icon class for a queue based on its type and status.
    /// </summary>
    private static string GetQueueIcon(QueueInfo queue)
    {
        if (!queue.IsAccessible)
        {
            return "bi bi-exclamation-triangle";
        }

        return queue.QueueType switch
        {
            QueueType.Private => "bi bi-inbox",
            QueueType.Public => "bi bi-envelope",
            QueueType.Journal => "bi bi-journal-text",
            QueueType.DeadLetter => "bi bi-x-circle",
            QueueType.TransactionalDeadLetter => "bi bi-x-octagon",
            QueueType.System => "bi bi-gear",
            _ => "bi bi-inbox"
        };
    }

    /// <summary>
    /// Finds a queue node in the tree by queue ID.
    /// </summary>
    /// <param name="rootNode">The root node to search from.</param>
    /// <param name="queueId">The queue ID to find.</param>
    /// <returns>The tree node containing the queue, or null if not found.</returns>
    public static TreeNodeData? FindQueueNode(TreeNodeData rootNode, string queueId)
    {
        if (rootNode.Id == queueId)
        {
            return rootNode;
        }

        foreach (var child in rootNode.Children)
        {
            var found = FindQueueNode(child, queueId);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Updates the badge counts in the tree based on current queue data.
    /// </summary>
    /// <param name="rootNode">The root node to update.</param>
    /// <param name="connection">The connection with updated queue data.</param>
    public static void UpdateBadgeCounts(TreeNodeData rootNode, QueueConnection connection)
    {
        UpdateNodeBadgeCounts(rootNode, connection.FilteredQueues.ToList());
    }

    /// <summary>
    /// Recursively updates badge counts for nodes.
    /// </summary>
    private static int UpdateNodeBadgeCounts(TreeNodeData node, List<QueueInfo> queues)
    {
        // If this is a queue node, update from queue data
        if (node.Data is QueueInfo queueInfo)
        {
            var currentQueue = queues.FirstOrDefault(q => q.Id == queueInfo.Id);
            if (currentQueue != null)
            {
                // Update based on node type
                switch (node.ViewType)
                {
                    case QueueViewType.Queue:
                        // Queue folder node shows both queue and journal counts
                        node.BadgeCount = currentQueue.MessageCount > 0 ? currentQueue.MessageCount : null;
                        node.SecondaryBadgeCount = currentQueue.JournalMessageCount > 0 ? currentQueue.JournalMessageCount : null;
                        return currentQueue.MessageCount;

                    case QueueViewType.QueueMessages:
                        // Queue messages node shows only queue count
                        node.BadgeCount = currentQueue.MessageCount > 0 ? currentQueue.MessageCount : null;
                        return currentQueue.MessageCount;

                    case QueueViewType.JournalMessages:
                        // Journal messages node shows only journal count
                        node.BadgeCount = currentQueue.JournalMessageCount > 0 ? currentQueue.JournalMessageCount : null;
                        return currentQueue.JournalMessageCount;

                    default:
                        node.BadgeCount = currentQueue.MessageCount > 0 ? currentQueue.MessageCount : null;
                        return currentQueue.MessageCount;
                }
            }
            return 0;
        }

        // If this is a folder node, sum up children
        if (node.Children.Any())
        {
            var total = node.Children.Sum(child => UpdateNodeBadgeCounts(child, queues));
            node.BadgeCount = total > 0 ? total : null;
            return total;
        }

        return 0;
    }
}
