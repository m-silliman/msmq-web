using Microsoft.AspNetCore.Components;
using MsMqApp.Services;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for toggling between light and dark theme modes.
/// </summary>
public class ThemeToggleBase : ComponentBase, IDisposable
{
    private bool _isLoading;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the theme service.
    /// </summary>
    [Inject]
    protected IThemeService ThemeService { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the toggle button is disabled.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the theme label text.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the button is in a loading state.
    /// </summary>
    protected bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                StateHasChanged();
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    /// <summary>
    /// Handles the toggle button click event.
    /// </summary>
    protected async Task HandleToggleClickAsync()
    {
        if (IsDisabled || IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            await ThemeService.ToggleThemeAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets the CSS class for the current theme icon.
    /// </summary>
    /// <returns>The icon CSS class.</returns>
    protected string GetIconClass()
    {
        return ThemeService.IsDarkMode ? "bi bi-moon-fill" : "bi bi-sun-fill";
    }

    /// <summary>
    /// Gets the label text for the current theme.
    /// </summary>
    /// <returns>The label text.</returns>
    protected string GetLabelText()
    {
        return ThemeService.IsDarkMode ? "Dark" : "Light";
    }

    /// <summary>
    /// Gets the tooltip text for the toggle button.
    /// </summary>
    /// <returns>The tooltip text.</returns>
    protected string GetTooltipText()
    {
        return ThemeService.IsDarkMode
            ? "Switch to light mode"
            : "Switch to dark mode";
    }

    /// <summary>
    /// Gets the ARIA label for accessibility.
    /// </summary>
    /// <returns>The ARIA label text.</returns>
    protected string GetAriaLabel()
    {
        return $"Toggle theme. Current theme: {GetLabelText()} mode";
    }

    /// <summary>
    /// Gets the active CSS class when button is active.
    /// </summary>
    /// <returns>The active CSS class or empty string.</returns>
    protected string GetActiveClass()
    {
        return ThemeService.IsDarkMode ? "active" : string.Empty;
    }

    /// <summary>
    /// Handles theme change events from the service.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources used by this component.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
        }

        _disposed = true;
    }
}
