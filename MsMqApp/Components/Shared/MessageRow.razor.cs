using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying an individual message row in a grid.
/// Provides consistent formatting and interaction handling.
/// </summary>
public class MessageRowBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the message to display.
    /// </summary>
    [Parameter]
    public QueueMessage Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether this message is selected.
    /// </summary>
    [Parameter]
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets whether this message is checked (bulk selection).
    /// </summary>
    [Parameter]
    public bool IsChecked { get; set; }

    /// <summary>
    /// Gets or sets whether to show the message ID below the label.
    /// Default is false.
    /// </summary>
    [Parameter]
    public bool ShowId { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the row is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage> OnRowClick { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the checkbox is changed.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage> OnCheckboxChanged { get; set; }

    /// <summary>
    /// Gets the CSS class for the row based on its state.
    /// </summary>
    /// <returns>The row CSS class string.</returns>
    protected string GetRowClass()
    {
        var classes = new List<string>();

        if (IsSelected)
        {
            classes.Add("message-row-selected");
        }

        if (IsChecked)
        {
            classes.Add("message-row-checked");
        }

        if (Message.IsExpired)
        {
            classes.Add("message-row-expired");
        }

        if (Message.Priority == MessagePriority.Highest || Message.Priority == MessagePriority.VeryHigh)
        {
            classes.Add("message-row-high-priority");
        }

        return string.Join(" ", classes);
    }

    /// <summary>
    /// Gets the CSS class for the priority badge.
    /// </summary>
    /// <returns>The badge CSS class.</returns>
    protected string GetPriorityBadgeClass()
    {
        return Message.Priority switch
        {
            MessagePriority.Lowest => "bg-secondary",
            MessagePriority.VeryLow => "bg-secondary",
            MessagePriority.Low => "bg-info",
            MessagePriority.Normal => "bg-primary",
            MessagePriority.AboveNormal => "bg-success",
            MessagePriority.High => "bg-warning text-dark",
            MessagePriority.VeryHigh => "bg-danger",
            MessagePriority.Highest => "bg-danger",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Gets the priority text for display.
    /// </summary>
    /// <returns>The priority text.</returns>
    protected string GetPriorityText()
    {
        // Show shorter text to save space
        return Message.Priority switch
        {
            MessagePriority.Lowest => "Lowest",
            MessagePriority.VeryLow => "V.Low",
            MessagePriority.Low => "Low",
            MessagePriority.Normal => "Normal",
            MessagePriority.AboveNormal => "Above",
            MessagePriority.High => "High",
            MessagePriority.VeryHigh => "V.High",
            MessagePriority.Highest => "Highest",
            _ => ((int)Message.Priority).ToString()
        };
    }

    /// <summary>
    /// Gets the formatted time string using relative or absolute formatting.
    /// </summary>
    /// <returns>The formatted time string.</returns>
    protected string GetFormattedTime()
    {
        var now = DateTime.Now;
        var diff = now - Message.ArrivedTime;

        // Use relative time for recent messages
        if (diff.TotalSeconds < 60)
        {
            return "Just now";
        }

        if (diff.TotalMinutes < 60)
        {
            var minutes = (int)diff.TotalMinutes;
            return $"{minutes}m ago";
        }

        if (diff.TotalHours < 24)
        {
            var hours = (int)diff.TotalHours;
            return $"{hours}h ago";
        }

        if (diff.TotalDays < 7)
        {
            var days = (int)diff.TotalDays;
            return $"{days}d ago";
        }

        // Use absolute time for older messages
        if (diff.TotalDays < 365)
        {
            return Message.ArrivedTime.ToString("MMM d, h:mm tt");
        }

        return Message.ArrivedTime.ToString("MMM d yyyy");
    }

    /// <summary>
    /// Gets the ARIA label for the checkbox.
    /// </summary>
    /// <returns>The ARIA label text.</returns>
    protected string GetCheckboxAriaLabel()
    {
        return $"Select message: {Message.Label}";
    }

    /// <summary>
    /// Handles row click events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnRowClickAsync()
    {
        if (OnRowClick.HasDelegate)
        {
            await OnRowClick.InvokeAsync(Message);
        }
    }

    /// <summary>
    /// Handles checkbox change events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnCheckboxChangedAsync()
    {
        if (OnCheckboxChanged.HasDelegate)
        {
            await OnCheckboxChanged.InvokeAsync(Message);
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Message == null)
        {
            throw new InvalidOperationException(
                $"{nameof(Message)} parameter is required for MessageRow component.");
        }
    }
}
