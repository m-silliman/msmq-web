using Microsoft.AspNetCore.Components;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Reusable loading spinner component with configurable size and message.
/// </summary>
public class LoadingSpinnerBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the size of the spinner.
    /// Default is Medium.
    /// </summary>
    [Parameter]
    public SpinnerSize Size { get; set; } = SpinnerSize.Medium;

    /// <summary>
    /// Gets or sets an optional message to display below the spinner.
    /// </summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the color variant of the spinner.
    /// Default is Primary.
    /// </summary>
    [Parameter]
    public SpinnerColor Color { get; set; } = SpinnerColor.Primary;

    /// <summary>
    /// Gets the CSS class for the spinner size.
    /// </summary>
    /// <returns>The size CSS class name.</returns>
    protected string GetSizeClass()
    {
        return Size switch
        {
            SpinnerSize.Small => "spinner-small",
            SpinnerSize.Medium => "spinner-medium",
            SpinnerSize.Large => "spinner-large",
            _ => "spinner-medium"
        };
    }

    /// <summary>
    /// Gets the CSS class for the spinner color.
    /// </summary>
    /// <returns>The color CSS class name.</returns>
    protected string GetColorClass()
    {
        return Color switch
        {
            SpinnerColor.Primary => "text-primary",
            SpinnerColor.Secondary => "text-secondary",
            SpinnerColor.Success => "text-success",
            SpinnerColor.Danger => "text-danger",
            SpinnerColor.Warning => "text-warning",
            SpinnerColor.Info => "text-info",
            SpinnerColor.Light => "text-light",
            SpinnerColor.Dark => "text-dark",
            _ => "text-primary"
        };
    }
}

/// <summary>
/// Defines the available spinner sizes.
/// </summary>
public enum SpinnerSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Defines the available spinner color variants.
/// </summary>
public enum SpinnerColor
{
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info,
    Light,
    Dark
}
