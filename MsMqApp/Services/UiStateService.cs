using Microsoft.JSInterop;

namespace MsMqApp.Services;

/// <summary>
/// Service for managing UI state with local storage persistence.
/// </summary>
public class UiStateService : IUiStateService, IAsyncDisposable
{
    private const string SidebarCollapsedKey = "msmq-app-sidebar-collapsed";
    private const string SidebarWidthKey = "msmq-app-sidebar-width";
    private const int DefaultSidebarWidth = 250;
    private const int DefaultCollapsedWidth = 48; // Reduced from 60 to 48 for tighter icon-only mode
    private const int DefaultMinWidth = 200;
    private const int DefaultMaxWidth = 500;

    private readonly IJSRuntime _jsRuntime;
    private bool _initialized;
    private bool _isSidebarCollapsed;
    private int _sidebarWidth = DefaultSidebarWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiStateService"/> class.
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime for interop operations.</param>
    public UiStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <inheritdoc/>
    public event EventHandler<bool>? SidebarCollapsedChanged;

    /// <inheritdoc/>
    public event EventHandler<int>? SidebarWidthChanged;

    /// <inheritdoc/>
    public bool IsSidebarCollapsed => _isSidebarCollapsed;

    /// <inheritdoc/>
    public int SidebarWidth => _sidebarWidth;

    /// <inheritdoc/>
    public int MinSidebarWidth => DefaultMinWidth;

    /// <inheritdoc/>
    public int MaxSidebarWidth => DefaultMaxWidth;

    /// <inheritdoc/>
    public int CollapsedSidebarWidth => DefaultCollapsedWidth;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            // Load sidebar collapsed state from local storage
            var storedCollapsed = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SidebarCollapsedKey);
            if (!string.IsNullOrEmpty(storedCollapsed) && bool.TryParse(storedCollapsed, out var collapsed))
            {
                _isSidebarCollapsed = collapsed;
            }

            // Load sidebar width from local storage
            var storedWidth = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SidebarWidthKey);
            if (!string.IsNullOrEmpty(storedWidth) && int.TryParse(storedWidth, out var width))
            {
                _sidebarWidth = Math.Clamp(width, MinSidebarWidth, MaxSidebarWidth);
            }

            _initialized = true;
        }
        catch (InvalidOperationException)
        {
            // JS interop not available yet (prerendering), will be initialized after first render
            _initialized = false;
        }
    }

    /// <inheritdoc/>
    public async Task ToggleSidebarAsync()
    {
        Console.WriteLine($"[DEBUG] UiStateService.ToggleSidebarAsync called - Current: {_isSidebarCollapsed}");
        await SetSidebarCollapsedAsync(!_isSidebarCollapsed);
        Console.WriteLine($"[DEBUG] UiStateService.ToggleSidebarAsync completed - New: {_isSidebarCollapsed}");
    }

    /// <inheritdoc/>
    public async Task SetSidebarCollapsedAsync(bool collapsed)
    {
        if (_isSidebarCollapsed == collapsed)
        {
            return;
        }

        _isSidebarCollapsed = collapsed;

        // Persist to local storage
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SidebarCollapsedKey, collapsed.ToString());
        }
        catch (InvalidOperationException)
        {
            // JS interop not available yet
        }

        // Raise event
        SidebarCollapsedChanged?.Invoke(this, collapsed);
    }

    /// <inheritdoc/>
    public async Task SetSidebarWidthAsync(int width)
    {
        // Clamp width to valid range
        var newWidth = Math.Clamp(width, MinSidebarWidth, MaxSidebarWidth);

        if (_sidebarWidth == newWidth)
        {
            return;
        }

        _sidebarWidth = newWidth;

        // Persist to local storage
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SidebarWidthKey, newWidth.ToString());
        }
        catch (InvalidOperationException)
        {
            // JS interop not available yet
        }

        // Raise event
        SidebarWidthChanged?.Invoke(this, newWidth);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
