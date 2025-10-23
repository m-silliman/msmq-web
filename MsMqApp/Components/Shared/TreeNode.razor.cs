using Microsoft.AspNetCore.Components;
using MsMqApp.Models.UI;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying a hierarchical tree node with expand/collapse functionality.
/// Supports selection, badges, icons, and recursive child rendering.
/// </summary>
public class TreeNodeBase : ComponentBase
{
    private const int IndentationPerLevel = 20;
    private const int MaxBadgeDisplay = 9999;

    /// <summary>
    /// Gets or sets the tree node data to display.
    /// </summary>
    [Parameter]
    public TreeNodeData NodeData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback invoked when this node is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<TreeNodeData> OnNodeClick { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when this node's expand/collapse state changes.
    /// </summary>
    [Parameter]
    public EventCallback<TreeNodeData> OnNodeToggle { get; set; }

    /// <summary>
    /// Gets the CSS class for the selected state.
    /// </summary>
    /// <returns>The selected CSS class if node is selected, empty otherwise.</returns>
    protected string GetSelectedClass()
    {
        return NodeData.IsSelected ? "tree-node-selected" : string.Empty;
    }

    /// <summary>
    /// Gets the indentation style based on the node's depth level.
    /// </summary>
    /// <returns>The inline CSS padding-left style.</returns>
    protected string GetIndentationStyle()
    {
        var paddingLeft = NodeData.Level * IndentationPerLevel;
        return $"{paddingLeft}px";
    }

    /// <summary>
    /// Gets the CSS class for the expand/collapse icon.
    /// </summary>
    /// <returns>The Bootstrap icon CSS class.</returns>
    protected string GetExpandIconClass()
    {
        return NodeData.IsExpanded ? "bi bi-chevron-down" : "bi bi-chevron-right";
    }

    /// <summary>
    /// Gets the ARIA label for the expand/collapse toggle button.
    /// </summary>
    /// <returns>The ARIA label text.</returns>
    protected string GetToggleAriaLabel()
    {
        var action = NodeData.IsExpanded ? "Collapse" : "Expand";
        return $"{action} {NodeData.Text}";
    }

    /// <summary>
    /// Gets the tooltip text for the badge.
    /// </summary>
    /// <returns>The tooltip text describing the badge count.</returns>
    protected string GetBadgeTooltip()
    {
        if (!NodeData.BadgeCount.HasValue)
        {
            return string.Empty;
        }

        var count = NodeData.BadgeCount.Value;
        return count == 1 ? "1 message" : $"{count:N0} messages";
    }

    /// <summary>
    /// Gets the ARIA label for the badge for accessibility.
    /// </summary>
    /// <returns>The ARIA label text.</returns>
    protected string GetBadgeAriaLabel()
    {
        if (!NodeData.BadgeCount.HasValue)
        {
            return string.Empty;
        }

        var count = NodeData.BadgeCount.Value;
        return count == 1 ? "1 message" : $"{count} messages";
    }

    /// <summary>
    /// Formats the badge count for display.
    /// Shows "9999+" if count exceeds maximum display value.
    /// </summary>
    /// <param name="count">The count to format.</param>
    /// <returns>The formatted count string.</returns>
    protected string FormatBadgeCount(int count)
    {
        return count > MaxBadgeDisplay ? $"{MaxBadgeDisplay:N0}+" : count.ToString("N0");
    }

    /// <summary>
    /// Gets the tooltip text for the secondary badge (journal count).
    /// </summary>
    /// <returns>The tooltip text describing the secondary badge count.</returns>
    protected string GetSecondaryBadgeTooltip()
    {
        if (!NodeData.SecondaryBadgeCount.HasValue)
        {
            return string.Empty;
        }

        var count = NodeData.SecondaryBadgeCount.Value;
        return count == 1 ? "1 journal message" : $"{count:N0} journal messages";
    }

    /// <summary>
    /// Gets the ARIA label for the secondary badge for accessibility.
    /// </summary>
    /// <returns>The ARIA label text.</returns>
    protected string GetSecondaryBadgeAriaLabel()
    {
        if (!NodeData.SecondaryBadgeCount.HasValue)
        {
            return string.Empty;
        }

        var count = NodeData.SecondaryBadgeCount.Value;
        return count == 1 ? "1 journal message" : $"{count} journal messages";
    }

    /// <summary>
    /// Handles node click events.
    /// Invokes the OnNodeClick callback with the current node data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnNodeClickAsync()
    {
        if (OnNodeClick.HasDelegate)
        {
            await OnNodeClick.InvokeAsync(NodeData);
        }
    }

    /// <summary>
    /// Handles expand/collapse toggle button click events.
    /// Toggles the expanded state and invokes the OnNodeToggle callback.
    /// The parent component is responsible for triggering re-render of the entire tree.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnToggleClickAsync()
    {
        NodeData.IsExpanded = !NodeData.IsExpanded;

        // Force this component to re-render immediately
        StateHasChanged();

        if (OnNodeToggle.HasDelegate)
        {
            await OnNodeToggle.InvokeAsync(NodeData);
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (NodeData == null)
        {
            throw new InvalidOperationException(
                $"{nameof(NodeData)} parameter is required for TreeNode component.");
        }
    }
}
