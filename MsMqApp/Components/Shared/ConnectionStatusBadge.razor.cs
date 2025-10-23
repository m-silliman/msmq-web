using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Enums;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying connection status with appropriate visual indicators.
/// </summary>
public class ConnectionStatusBadgeBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the connection status to display.
    /// </summary>
    [Parameter]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.NotConnected;

    /// <summary>
    /// Gets or sets whether to show the status label text.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show additional details.
    /// Default is false.
    /// </summary>
    [Parameter]
    public bool ShowDetails { get; set; }

    /// <summary>
    /// Gets or sets additional details to display (e.g., queue count, error message).
    /// </summary>
    [Parameter]
    public string? Details { get; set; }

    /// <summary>
    /// Gets the status variant based on connection status.
    /// </summary>
    /// <returns>The appropriate StatusVariant for the connection status.</returns>
    protected StatusVariant GetStatusVariant()
    {
        return Status switch
        {
            ConnectionStatus.Connected => StatusVariant.Success,
            ConnectionStatus.Connecting => StatusVariant.Info,
            ConnectionStatus.NotConnected => StatusVariant.Secondary,
            ConnectionStatus.Failed => StatusVariant.Danger,
            ConnectionStatus.Timeout => StatusVariant.Warning,
            ConnectionStatus.Disconnected => StatusVariant.Secondary,
            _ => StatusVariant.Secondary
        };
    }

    /// <summary>
    /// Gets the status label text.
    /// </summary>
    /// <returns>The status label string.</returns>
    protected string GetStatusLabel()
    {
        return Status switch
        {
            ConnectionStatus.Connected => "Connected",
            ConnectionStatus.Connecting => "Connecting",
            ConnectionStatus.NotConnected => "Not Connected",
            ConnectionStatus.Failed => "Failed",
            ConnectionStatus.Timeout => "Timeout",
            ConnectionStatus.Disconnected => "Disconnected",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the tooltip text for the status indicator.
    /// </summary>
    /// <returns>The tooltip text.</returns>
    protected string GetTooltip()
    {
        var baseTooltip = GetStatusLabel();

        if (!string.IsNullOrWhiteSpace(Details))
        {
            return $"{baseTooltip}: {Details}";
        }

        return baseTooltip;
    }

    /// <summary>
    /// Determines whether the indicator should pulse (animate).
    /// </summary>
    /// <returns>True if the indicator should pulse, false otherwise.</returns>
    protected bool ShouldPulse()
    {
        return Status == ConnectionStatus.Connecting;
    }
}
