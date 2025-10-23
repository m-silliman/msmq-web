using Microsoft.AspNetCore.Components;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying status indicators with Bootstrap color variants.
/// </summary>
public class StatusIndicatorBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the status variant (color scheme).
    /// Default is Primary.
    /// </summary>
    [Parameter]
    public StatusVariant Variant { get; set; } = StatusVariant.Primary;

    /// <summary>
    /// Gets or sets the size of the status indicator.
    /// Default is Medium.
    /// </summary>
    [Parameter]
    public StatusSize Size { get; set; } = StatusSize.Medium;

    /// <summary>
    /// Gets or sets the shape of the status indicator.
    /// Default is Circle.
    /// </summary>
    [Parameter]
    public StatusShape Shape { get; set; } = StatusShape.Circle;

    /// <summary>
    /// Gets or sets the label text to display next to the indicator.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the label.
    /// Default is true if Label is provided.
    /// </summary>
    [Parameter]
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show an icon in the indicator.
    /// Default is false.
    /// </summary>
    [Parameter]
    public bool ShowIcon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the indicator should pulse/animate.
    /// Default is false.
    /// </summary>
    [Parameter]
    public bool Pulse { get; set; }

    /// <summary>
    /// Gets or sets the tooltip text.
    /// </summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>
    /// Gets the CSS class for the status variant (color).
    /// </summary>
    /// <returns>The variant CSS class.</returns>
    protected string GetVariantClass()
    {
        return Variant switch
        {
            StatusVariant.Success => "status-success",
            StatusVariant.Primary => "status-primary",
            StatusVariant.Danger => "status-danger",
            StatusVariant.Warning => "status-warning",
            StatusVariant.Info => "status-info",
            StatusVariant.Secondary => "status-secondary",
            StatusVariant.Light => "status-light",
            StatusVariant.Dark => "status-dark",
            _ => "status-primary"
        };
    }

    /// <summary>
    /// Gets the CSS class for the size.
    /// </summary>
    /// <returns>The size CSS class.</returns>
    protected string GetSizeClass()
    {
        return Size switch
        {
            StatusSize.Small => "status-sm",
            StatusSize.Medium => "status-md",
            StatusSize.Large => "status-lg",
            _ => "status-md"
        };
    }

    /// <summary>
    /// Gets the CSS class for the shape.
    /// </summary>
    /// <returns>The shape CSS class.</returns>
    protected string GetShapeClass()
    {
        return Shape switch
        {
            StatusShape.Circle => "status-circle",
            StatusShape.Square => "status-square",
            StatusShape.Rounded => "status-rounded",
            _ => "status-circle"
        };
    }

    /// <summary>
    /// Gets the icon CSS class based on the variant.
    /// </summary>
    /// <returns>The icon CSS class or empty string.</returns>
    protected string GetIconClass()
    {
        if (!ShowIcon)
        {
            return string.Empty;
        }

        return Variant switch
        {
            StatusVariant.Success => "bi bi-check-circle-fill",
            StatusVariant.Danger => "bi bi-x-circle-fill",
            StatusVariant.Warning => "bi bi-exclamation-triangle-fill",
            StatusVariant.Info => "bi bi-info-circle-fill",
            StatusVariant.Primary => "bi bi-circle-fill",
            StatusVariant.Secondary => "bi bi-circle-fill",
            StatusVariant.Light => "bi bi-circle-fill",
            StatusVariant.Dark => "bi bi-circle-fill",
            _ => "bi bi-circle-fill"
        };
    }

    /// <summary>
    /// Gets the ARIA label for accessibility.
    /// </summary>
    /// <returns>The ARIA label text.</returns>
    protected string GetAriaLabel()
    {
        if (!string.IsNullOrWhiteSpace(Tooltip))
        {
            return Tooltip;
        }

        if (!string.IsNullOrWhiteSpace(Label))
        {
            return $"Status: {Label}";
        }

        return $"Status indicator: {Variant}";
    }
}

/// <summary>
/// Defines the available status color variants following Bootstrap standards.
/// </summary>
public enum StatusVariant
{
    /// <summary>
    /// Success status (green) - operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Primary status (blue) - primary or normal state.
    /// </summary>
    Primary,

    /// <summary>
    /// Danger status (red) - error or critical state.
    /// </summary>
    Danger,

    /// <summary>
    /// Warning status (yellow/orange) - warning or caution state.
    /// </summary>
    Warning,

    /// <summary>
    /// Info status (cyan) - informational state.
    /// </summary>
    Info,

    /// <summary>
    /// Secondary status (gray) - secondary or inactive state.
    /// </summary>
    Secondary,

    /// <summary>
    /// Light status - light variant.
    /// </summary>
    Light,

    /// <summary>
    /// Dark status - dark variant.
    /// </summary>
    Dark
}

/// <summary>
/// Defines the available status indicator sizes.
/// </summary>
public enum StatusSize
{
    /// <summary>
    /// Small size indicator.
    /// </summary>
    Small,

    /// <summary>
    /// Medium size indicator (default).
    /// </summary>
    Medium,

    /// <summary>
    /// Large size indicator.
    /// </summary>
    Large
}

/// <summary>
/// Defines the available status indicator shapes.
/// </summary>
public enum StatusShape
{
    /// <summary>
    /// Circular shape (default).
    /// </summary>
    Circle,

    /// <summary>
    /// Square shape with sharp corners.
    /// </summary>
    Square,

    /// <summary>
    /// Rounded square shape.
    /// </summary>
    Rounded
}
