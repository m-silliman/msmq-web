namespace MsMqApp.Services;

/// <summary>
/// Service interface for managing UI state across the application.
/// </summary>
public interface IUiStateService
{
    /// <summary>
    /// Event raised when the sidebar collapsed state changes.
    /// </summary>
    event EventHandler<bool>? SidebarCollapsedChanged;

    /// <summary>
    /// Event raised when the sidebar width changes.
    /// </summary>
    event EventHandler<int>? SidebarWidthChanged;

    /// <summary>
    /// Gets a value indicating whether the sidebar is collapsed.
    /// </summary>
    bool IsSidebarCollapsed { get; }

    /// <summary>
    /// Gets the current sidebar width in pixels.
    /// </summary>
    int SidebarWidth { get; }

    /// <summary>
    /// Gets the minimum sidebar width in pixels.
    /// </summary>
    int MinSidebarWidth { get; }

    /// <summary>
    /// Gets the maximum sidebar width in pixels.
    /// </summary>
    int MaxSidebarWidth { get; }

    /// <summary>
    /// Gets the collapsed sidebar width in pixels (icons only).
    /// </summary>
    int CollapsedSidebarWidth { get; }

    /// <summary>
    /// Initializes the UI state from local storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Toggles the sidebar between collapsed and expanded states.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ToggleSidebarAsync();

    /// <summary>
    /// Sets the sidebar collapsed state.
    /// </summary>
    /// <param name="collapsed">True to collapse the sidebar, false to expand it.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetSidebarCollapsedAsync(bool collapsed);

    /// <summary>
    /// Sets the sidebar width.
    /// </summary>
    /// <param name="width">The new width in pixels.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetSidebarWidthAsync(int width);
}
