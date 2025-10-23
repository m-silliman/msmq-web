using Microsoft.JSInterop;

namespace MsMqApp.Services;

/// <summary>
/// Implementation of theme management service with localStorage persistence.
/// </summary>
public class ThemeService : IThemeService, IAsyncDisposable
{
    private const string ThemeStorageKey = "msmq-app-theme";
    private readonly IJSRuntime _jsRuntime;
    private ThemeMode _currentTheme = ThemeMode.Dark; // Default to Dark mode
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime for interop operations.</param>
    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <inheritdoc/>
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc/>
    public ThemeMode CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public bool IsDarkMode => _currentTheme == ThemeMode.Dark;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            // Get the current theme from DOM (set by our pre-init script)
            var currentDomTheme = await GetCurrentDomThemeAsync();
            
            var storedTheme = await GetStoredThemeAsync();
            if (storedTheme.HasValue)
            {
                _currentTheme = storedTheme.Value;
            }
            else
            {
                // Default to Dark mode
                _currentTheme = ThemeMode.Dark;
                await StoreThemeAsync(_currentTheme);
            }

            // Only apply theme if DOM doesn't match our current theme
            var expectedDomTheme = _currentTheme == ThemeMode.Dark ? "dark" : "light";
            if (currentDomTheme != expectedDomTheme)
            {
                await ApplyThemeAsync(_currentTheme);
            }
            
            _initialized = true;
        }
        catch (InvalidOperationException)
        {
            // JS interop not available yet (prerendering), use default theme
            // Will be initialized properly after first render
            _currentTheme = ThemeMode.Dark;
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected, use default theme
            _currentTheme = ThemeMode.Dark;
        }
        catch (JSException)
        {
            // JS error, use default theme
            _currentTheme = ThemeMode.Dark;
        }
    }

    /// <inheritdoc/>
    public async Task SetThemeAsync(ThemeMode theme)
    {
        if (_currentTheme == theme)
        {
            return;
        }

        var previousTheme = _currentTheme;
        _currentTheme = theme;

        await ApplyThemeAsync(theme);
        await StoreThemeAsync(theme);

        OnThemeChanged(new ThemeChangedEventArgs(theme, previousTheme));
    }

    /// <inheritdoc/>
    public async Task ToggleThemeAsync()
    {
        var newTheme = _currentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
        await SetThemeAsync(newTheme);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Clear event handlers
        ThemeChanged = null;
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Applies the theme to the document by setting the data-bs-theme attribute.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    private async Task ApplyThemeAsync(ThemeMode theme)
    {
        var themeValue = theme == ThemeMode.Dark ? "dark" : "light";

        try
        {
            // Use the client-side theme application function for consistency
            await _jsRuntime.InvokeVoidAsync("eval", $@"
                if (typeof window.applyTheme === 'function') {{
                    console.log('[ThemeService] Using client-side applyTheme function');
                    window.applyTheme('{themeValue}');
                }} else {{
                    console.log('[ThemeService] Direct DOM manipulation fallback');
                    document.documentElement.setAttribute('data-bs-theme', '{themeValue}');
                    document.body.className = document.body.className.replace(/theme-(light|dark)/g, '').trim();
                    document.body.classList.add('theme-{themeValue}');
                    localStorage.setItem('msmq-app-theme', '{themeValue}');
                }}
            ");
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected, ignore
        }
        catch (JSException)
        {
            // JS error, ignore
        }
    }

    /// <summary>
    /// Stores the theme preference in localStorage.
    /// </summary>
    /// <param name="theme">The theme to store.</param>
    private async Task StoreThemeAsync(ThemeMode theme)
    {
        try
        {
            var themeValue = theme == ThemeMode.Dark ? "dark" : "light";
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeStorageKey, themeValue);
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected, ignore
        }
        catch (JSException)
        {
            // JS error, ignore
        }
    }

    /// <summary>
    /// Gets the current theme from the DOM.
    /// </summary>
    /// <returns>The current theme value from the DOM, or "dark" as default.</returns>
    private async Task<string> GetCurrentDomThemeAsync()
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("eval", 
                "document.documentElement.getAttribute('data-bs-theme') || 'dark'");
            return result ?? "dark";
        }
        catch (JSDisconnectedException)
        {
            return "dark";
        }
        catch (JSException)
        {
            return "dark";
        }
    }

    /// <summary>
    /// Retrieves the stored theme preference from localStorage.
    /// </summary>
    /// <returns>The stored theme mode, or null if not found.</returns>
    private async Task<ThemeMode?> GetStoredThemeAsync()
    {
        try
        {
            var storedValue = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeStorageKey);

            if (string.IsNullOrWhiteSpace(storedValue))
            {
                return null;
            }

            return storedValue.ToLowerInvariant() == "dark" ? ThemeMode.Dark : ThemeMode.Light;
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected, return null
            return null;
        }
        catch (JSException)
        {
            // JS error, return null
            return null;
        }
    }

    /// <summary>
    /// Raises the ThemeChanged event.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    protected virtual void OnThemeChanged(ThemeChangedEventArgs e)
    {
        ThemeChanged?.Invoke(this, e);
    }
}
