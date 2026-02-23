using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.UI;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying a hierarchical tree view of MSMQ queues.
/// Organizes queues by type (Private, Public, Outgoing, System) and provides
/// selection and refresh capabilities.
/// </summary>
public class QueueTreeViewBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the queue connection to display.
    /// </summary>
    [Parameter]
    public QueueConnection? Connection { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when a queue is selected.
    /// Passes the tree node data containing queue info and view type.
    /// </summary>
    [Parameter]
    public EventCallback<TreeNodeData?> OnNodeSelected { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when a queue is selected (legacy).
    /// For backward compatibility - use OnNodeSelected for new code.
    /// </summary>
    [Parameter]
    public EventCallback<QueueInfo?> OnQueueSelected { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the refresh button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnRefreshRequested { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the purge button is clicked.
    /// Passes the selected queue and current view type for purging.
    /// </summary>
    [Parameter]
    public EventCallback<(QueueInfo Queue, Models.Enums.QueueViewType ViewType)> OnPurgeRequested { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the send message button is clicked.
    /// Passes the selected queue path for pre-selecting in the send dialog.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSendMessageRequested { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the filter changes.
    /// Passes the filtered tree root so parent can update derived views.
    /// </summary>
    [Parameter]
    public EventCallback<TreeNodeData?> OnFilterChanged { get; set; }

    /// <summary>
    /// Gets or sets whether to show the refresh button.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool ShowRefreshButton { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the "Refresh" label on the button.
    /// Default is false (icon only).
    /// </summary>
    [Parameter]
    public bool ShowRefreshLabel { get; set; }

    /// <summary>
    /// Gets or sets whether to show connection details in the header.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool ShowConnectionDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to expand all nodes by default.
    /// Default is false.
    /// </summary>
    [Parameter]
    public bool ExpandAll { get; set; }

    /// <summary>
    /// Gets or sets the currently selected queue.
    /// </summary>
    [Parameter]
    public QueueInfo? SelectedQueue { get; set; }

    /// <summary>
    /// Gets or sets the callback when SelectedQueue changes.
    /// </summary>
    [Parameter]
    public EventCallback<QueueInfo?> SelectedQueueChanged { get; set; }

    /// <summary>
    /// Gets or sets the current view type (Queue, QueueMessages, JournalMessages).
    /// Used to maintain selection state when tree rebuilds.
    /// </summary>
    [Parameter]
    public Models.Enums.QueueViewType CurrentViewType { get; set; } = Models.Enums.QueueViewType.QueueMessages;

    /// <summary>
    /// Gets or sets whether the component is currently refreshing.
    /// </summary>
    protected bool IsRefreshing { get; set; }

    /// <summary>
    /// Gets or sets the error message if tree building fails.
    /// </summary>
    protected string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the root tree node for the connection.
    /// </summary>
    protected TreeNodeData? TreeRoot { get; private set; }

    /// <summary>
    /// Gets the filtered tree root based on current filter text.
    /// </summary>
    protected TreeNodeData? FilteredTreeRoot { get; private set; }

    /// <summary>
    /// Gets or sets the current filter text for queue names.
    /// </summary>
    protected string FilterText { get; set; } = string.Empty;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Connection != null)
        {
            try
            {
                // Store current expansion and selection state before rebuilding
                var expandedNodes = new HashSet<string>();
                var selectedNodeId = string.Empty;
                
                if (TreeRoot != null)
                {
                    CollectExpandedNodes(TreeRoot, expandedNodes);
                    selectedNodeId = FindSelectedNodeId(TreeRoot);
                }

                // Rebuild tree
                TreeRoot = QueueTreeBuilder.BuildTreeFromConnection(Connection, ExpandAll);
                ErrorMessage = null;

                // Restore expansion and selection state
                if (TreeRoot != null)
                {
                    RestoreExpandedNodes(TreeRoot, expandedNodes);

                    // Determine the target node ID based on SelectedQueue and CurrentViewType
                    string? targetSelectedId = null;
                    if (SelectedQueue != null)
                    {
                        // Reconstruct the node ID based on view type
                        targetSelectedId = CurrentViewType switch
                        {
                            Models.Enums.QueueViewType.QueueMessages => SelectedQueue.QueueType == QueueType.Outgoing
                                ? SelectedQueue.Id
                                : $"{SelectedQueue.Id}_messages",
                            Models.Enums.QueueViewType.JournalMessages => $"{SelectedQueue.Id}_journal",
                            Models.Enums.QueueViewType.Queue => SelectedQueue.Id,
                            _ => SelectedQueue.Id
                        };
                    }
                    else
                    {
                        // Fall back to previous selection
                        targetSelectedId = selectedNodeId;
                    }

                    if (!string.IsNullOrEmpty(targetSelectedId))
                    {
                        UpdateSelectionState(TreeRoot, targetSelectedId);
                    }
                }

                // Apply filter if present
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error building queue tree: {ex.Message}";
                TreeRoot = null;
                FilteredTreeRoot = null;
            }
        }
        else
        {
            TreeRoot = null;
            FilteredTreeRoot = null;
            ErrorMessage = null;
        }
    }

    /// <summary>
    /// Gets the connection details string for display.
    /// </summary>
    /// <returns>The connection details string.</returns>
    protected string GetConnectionDetails()
    {
        if (Connection == null)
        {
            return string.Empty;
        }

        if (Connection.Status == Models.Enums.ConnectionStatus.Connected)
        {
            var queueCount = Connection.TotalQueues;
            var messageCount = Connection.TotalMessages;
            return $"{queueCount} queue{(queueCount != 1 ? "s" : "")}, {messageCount:N0} message{(messageCount != 1 ? "s" : "")}";
        }

        if (!string.IsNullOrWhiteSpace(Connection.ErrorMessage))
        {
            return Connection.ErrorMessage;
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the CSS class for the refresh button icon.
    /// Shows spinning animation when refreshing.
    /// </summary>
    /// <returns>The icon CSS class.</returns>
    protected string GetRefreshIconClass()
    {
        return (Connection?.IsRefreshing ?? false) || IsRefreshing
            ? "bi-arrow-clockwise spin"
            : "bi-arrow-clockwise";
    }

    /// <summary>
    /// Handles node click events.
    /// </summary>
    /// <param name="nodeData">The clicked node data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleNodeClickAsync(TreeNodeData nodeData)
    {
        if (TreeRoot == null)
        {
            return;
        }

        // Clear previous selection
        ClearSelection(TreeRoot);

        // Set new selection
        nodeData.IsSelected = true;

        // Extract queue info if this is a queue node
        var queueInfo = nodeData.Data as QueueInfo;
        SelectedQueue = queueInfo;

        // Notify parent component with node data (includes ViewType)
        if (OnNodeSelected.HasDelegate)
        {
            await OnNodeSelected.InvokeAsync(nodeData);
        }

        // Notify parent component with queue info (legacy/backward compatibility)
        if (SelectedQueueChanged.HasDelegate)
        {
            await SelectedQueueChanged.InvokeAsync(queueInfo);
        }

        if (OnQueueSelected.HasDelegate)
        {
            await OnQueueSelected.InvokeAsync(queueInfo);
        }

        StateHasChanged();
    }

    /// <summary>
    /// Handles node toggle (expand/collapse) events.
    /// </summary>
    /// <param name="nodeData">The toggled node data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task HandleNodeToggleAsync(TreeNodeData nodeData)
    {
        // The tree node already updated its state, just trigger re-render
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles refresh button click events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnRefreshClickedAsync()
    {
        IsRefreshing = true;
        StateHasChanged();

        try
        {
            if (OnRefreshRequested.HasDelegate)
            {
                await OnRefreshRequested.InvokeAsync();
            }
        }
        finally
        {
            IsRefreshing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles purge button click events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnPurgeClickedAsync()
    {
        if (SelectedQueue == null || !OnPurgeRequested.HasDelegate)
        {
            return;
        }

        await OnPurgeRequested.InvokeAsync((SelectedQueue, CurrentViewType));
    }

    /// <summary>
    /// Handles send message button click events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnSendMessageClickedAsync()
    {
        if (SelectedQueue == null || !OnSendMessageRequested.HasDelegate)
        {
            return;
        }

        // Determine the queue path to pre-select based on current view type
        var queuePath = CurrentViewType switch
        {
            Models.Enums.QueueViewType.JournalMessages => SelectedQueue.JournalPath ?? SelectedQueue.Path,
            _ => SelectedQueue.Path
        };

        await OnSendMessageRequested.InvokeAsync(queuePath);
    }

    /// <summary>
    /// Determines if the purge button should be visible and enabled.
    /// </summary>
    /// <returns>True if purge button should be shown and enabled.</returns>
    protected bool ShouldShowPurgeButton()
    {
        if (SelectedQueue == null || Connection?.IsRefreshing == true || IsRefreshing)
        {
            return false;
        }

        // Show purge button if there are messages in the selected queue/view
        return CurrentViewType switch
        {
            Models.Enums.QueueViewType.QueueMessages => SelectedQueue.MessageCount > 0,
            Models.Enums.QueueViewType.JournalMessages => SelectedQueue.JournalMessageCount > 0,
            _ => false
        };
    }

    /// <summary>
    /// Determines if the send message button should be visible and enabled.
    /// </summary>
    /// <returns>True if send message button should be shown and enabled.</returns>
    protected bool ShouldShowSendMessageButton()
    {
        if (SelectedQueue == null || Connection?.IsRefreshing == true || IsRefreshing)
        {
            return false;
        }

        // Show send button for queue messages only (not for journal queues since you typically don't send to journals)
        return CurrentViewType == Models.Enums.QueueViewType.QueueMessages;
    }

    /// <summary>
    /// Recursively clears selection from all nodes.
    /// </summary>
    /// <param name="node">The node to process.</param>
    private void ClearSelection(TreeNodeData node)
    {
        node.IsSelected = false;

        foreach (var child in node.Children)
        {
            ClearSelection(child);
        }
    }

    /// <summary>
    /// Recursively updates selection state based on node ID.
    /// </summary>
    /// <param name="node">The node to process.</param>
    /// <param name="selectedNodeId">The ID of the node to select.</param>
    private void UpdateSelectionState(TreeNodeData node, string selectedNodeId)
    {
        // Match by node ID, not queue ID, to avoid selecting multiple nodes
        node.IsSelected = node.Id == selectedNodeId;

        foreach (var child in node.Children)
        {
            UpdateSelectionState(child, selectedNodeId);
        }
    }

    /// <summary>
    /// Updates the tree with refreshed connection data.
    /// Call this method when connection data changes.
    /// </summary>
    public void RefreshTree()
    {
        if (Connection != null && TreeRoot != null)
        {
            try
            {
                // Update badge counts without rebuilding entire tree
                QueueTreeBuilder.UpdateBadgeCounts(TreeRoot, Connection);
                
                // Reapply filter to update filtered view
                ApplyFilter();
                
                StateHasChanged();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error refreshing tree: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Handles filter input changes.
    /// </summary>
    /// <param name="e">The change event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnFilterInputAsync(ChangeEventArgs e)
    {
        FilterText = e.Value?.ToString() ?? string.Empty;
        ApplyFilter();
        
        // Notify parent that filter has changed
        if (OnFilterChanged.HasDelegate)
        {
            await OnFilterChanged.InvokeAsync(FilteredTreeRoot);
        }
        
        StateHasChanged();
    }

    /// <summary>
    /// Handles clearing the filter.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnClearFilterAsync()
    {
        FilterText = string.Empty;
        ApplyFilter();
        
        // Notify parent that filter has been cleared
        if (OnFilterChanged.HasDelegate)
        {
            await OnFilterChanged.InvokeAsync(FilteredTreeRoot);
        }
        
        StateHasChanged();
    }

    /// <summary>
    /// Applies the current filter to the tree.
    /// </summary>
    private void ApplyFilter()
    {
        if (TreeRoot == null)
        {
            FilteredTreeRoot = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(FilterText))
        {
            // No filter - show entire tree
            FilteredTreeRoot = TreeRoot;
        }
        else
        {
            // Collect current expansion state from filtered tree before refiltering
            var expansionState = new Dictionary<string, bool>();
            if (FilteredTreeRoot != null)
            {
                CollectExpansionState(FilteredTreeRoot, expansionState);
            }

            // Apply filter and create filtered tree
            FilteredTreeRoot = FilterTreeNode(TreeRoot, expansionState);
        }
    }

    /// <summary>
    /// Recursively filters a tree node and its children based on the filter text.
    /// </summary>
    /// <param name="node">The node to filter.</param>
    /// <param name="expansionState">Dictionary of node IDs to their expansion states.</param>
    /// <returns>A filtered copy of the node, or null if it doesn't match the filter.</returns>
    private TreeNodeData? FilterTreeNode(TreeNodeData node, Dictionary<string, bool> expansionState)
    {
        // Check if this is a queue node (has QueueInfo as Data)
        var isQueueNode = node.Data is QueueInfo;
        
        // Only apply filter matching to queue nodes (leaf queue names)
        var nodeMatches = isQueueNode && node.Text.Contains(FilterText, StringComparison.OrdinalIgnoreCase);

        // Determine expansion state: use user's current state if available, otherwise use original
        var isExpanded = expansionState.ContainsKey(node.Id) ? expansionState[node.Id] : node.IsExpanded;

        // If this queue node matches, include it with ALL its children (Queue Messages, Journal Messages)
        if (nodeMatches)
        {
            // Clone the entire node with all children intact, preserving expansion state
            var filteredNode = new TreeNodeData
            {
                Id = node.Id,
                Text = node.Text,
                IconClass = node.IconClass,
                Data = node.Data,
                IsExpanded = isExpanded, // Use preserved expansion state
                IsSelected = node.IsSelected,
                Level = node.Level,
                ViewType = node.ViewType,
                BadgeCount = node.BadgeCount,
                SecondaryBadgeCount = node.SecondaryBadgeCount,
                HasChildren = node.HasChildren,
                Children = node.Children.Select(child => {
                    var childExpanded = expansionState.ContainsKey(child.Id) ? expansionState[child.Id] : child.IsExpanded;
                    return new TreeNodeData
                    {
                        Id = child.Id,
                        Text = child.Text,
                        IconClass = child.IconClass,
                        Data = child.Data,
                        IsExpanded = childExpanded, // Use preserved expansion state
                        IsSelected = child.IsSelected,
                        Level = child.Level,
                        ViewType = child.ViewType,
                        BadgeCount = child.BadgeCount,
                        SecondaryBadgeCount = child.SecondaryBadgeCount,
                        HasChildren = child.HasChildren,
                        Children = child.Children.ToList()
                    };
                }).ToList()
            };

            return filteredNode;
        }

        // For non-queue nodes (categories, etc.), recursively filter children
        if (!isQueueNode)
        {
            var filteredChildren = node.Children
                .Select(child => FilterTreeNode(child, expansionState))
                .Where(child => child != null)
                .Cast<TreeNodeData>()
                .ToList();

            // Include this parent node if any of its children match
            if (filteredChildren.Any())
            {
                var filteredNode = new TreeNodeData
                {
                    Id = node.Id,
                    Text = node.Text,
                    IconClass = node.IconClass,
                    Data = node.Data,
                    IsExpanded = true, // Expand parent categories to show matching queues
                    IsSelected = node.IsSelected,
                    Level = node.Level,
                    ViewType = node.ViewType,
                    BadgeCount = node.BadgeCount,
                    SecondaryBadgeCount = node.SecondaryBadgeCount,
                    HasChildren = filteredChildren.Any(),
                    Children = filteredChildren
                };

                return filteredNode;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively collects expansion state from all nodes.
    /// </summary>
    /// <param name="node">The node to process.</param>
    /// <param name="expansionState">Dictionary to store node IDs and their expansion states.</param>
    private void CollectExpansionState(TreeNodeData node, Dictionary<string, bool> expansionState)
    {
        expansionState[node.Id] = node.IsExpanded;

        foreach (var child in node.Children)
        {
            CollectExpansionState(child, expansionState);
        }
    }

    /// <summary>
    /// Recursively collects IDs of all expanded nodes.
    /// </summary>
    /// <param name="node">The node to process.</param>
    /// <param name="expandedNodes">Collection to store expanded node IDs.</param>
    private void CollectExpandedNodes(TreeNodeData node, HashSet<string> expandedNodes)
    {
        if (node.IsExpanded)
        {
            expandedNodes.Add(node.Id);
        }

        foreach (var child in node.Children)
        {
            CollectExpandedNodes(child, expandedNodes);
        }
    }

    /// <summary>
    /// Finds the ID of the currently selected node.
    /// </summary>
    /// <param name="node">The node to search.</param>
    /// <returns>The ID of the selected node, or empty string if none found.</returns>
    private string FindSelectedNodeId(TreeNodeData node)
    {
        if (node.IsSelected)
        {
            return node.Id;
        }

        foreach (var child in node.Children)
        {
            var result = FindSelectedNodeId(child);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Recursively restores expansion state for nodes.
    /// </summary>
    /// <param name="node">The node to process.</param>
    /// <param name="expandedNodes">Collection of node IDs that should be expanded.</param>
    private void RestoreExpandedNodes(TreeNodeData node, HashSet<string> expandedNodes)
    {
        node.IsExpanded = expandedNodes.Contains(node.Id);

        foreach (var child in node.Children)
        {
            RestoreExpandedNodes(child, expandedNodes);
        }
    }
}
