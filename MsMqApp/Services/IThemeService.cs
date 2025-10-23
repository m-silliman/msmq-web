namespace MsMqApp.Services;

/// <summary>
/// Service for managing application theme (dark/light mode).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Gets the current theme mode.
    /// </summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets a value indicating whether dark mode is currently active.
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="theme">The theme mode to apply.</param>
    Task SetThemeAsync(ThemeMode theme);

    /// <summary>
    /// Toggles between light and dark mode.
    /// </summary>
    Task ToggleThemeAsync();

    /// <summary>
    /// Initializes the theme from stored preferences or system default.
    /// </summary>
    Task InitializeAsync();
}

/// <summary>
/// Defines the available theme modes.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Light theme mode.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme mode.
    /// </summary>
    Dark
}

/// <summary>
/// Event arguments for theme change events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new theme mode.
    /// </summary>
    public ThemeMode NewTheme { get; }

    /// <summary>
    /// Gets the previous theme mode.
    /// </summary>
    public ThemeMode PreviousTheme { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
    /// </summary>
    /// <param name="newTheme">The new theme mode.</param>
    /// <param name="previousTheme">The previous theme mode.</param>
    public ThemeChangedEventArgs(ThemeMode newTheme, ThemeMode previousTheme)
    {
        NewTheme = newTheme;
        PreviousTheme = previousTheme;
    }
}
