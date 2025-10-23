using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MsMqApp.Services;

namespace MsMqApp.Components.Layout;

/// <summary>
/// Main layout component with theme support.
/// </summary>
public class MainLayoutBase : LayoutComponentBase, IAsyncDisposable
{
    private bool _disposed;
    private bool _themeInitialized;
    protected bool _drawerOpen = false;

    /// <summary>
    /// Gets or sets the theme service.
    /// </summary>
    [Inject]
    protected IThemeService ThemeService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        // Subscribe to theme changes
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // Initialize theme
            if (!_themeInitialized)
            {
                await ThemeService.InitializeAsync();
                _themeInitialized = true;
            }

            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles theme changes.
    /// </summary>
    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Toggles the navigation drawer.
    /// </summary>
    protected void ToggleDrawerAsync()
    {
        _drawerOpen = !_drawerOpen;
        Console.WriteLine($"ToggleDrawerAsync: _drawerOpen = {_drawerOpen}");
        
        // Handle body scroll prevention with JavaScript
        InvokeAsync(async () =>
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", _drawerOpen 
                    ? "document.body.style.overflow = 'hidden'"
                    : "document.body.style.overflow = ''");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting body overflow: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Closes the navigation drawer.
    /// </summary>
    protected void CloseDrawerAsync()
    {
        if (_drawerOpen)
        {
            _drawerOpen = false;
            Console.WriteLine("CloseDrawerAsync: closing drawer");
            
            // Restore body scroll
            InvokeAsync(async () =>
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("eval", "document.body.style.overflow = ''");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restoring body overflow: {ex.Message}");
                }
            });
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unsubscribe from events
        ThemeService.ThemeChanged -= OnThemeChanged;

        // Dispose services
        if (ThemeService is IAsyncDisposable themeDisposable)
        {
            await themeDisposable.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
