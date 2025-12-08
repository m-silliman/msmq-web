using Microsoft.AspNetCore.Components;
using System.Timers;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for refresh controls with pause/resume and countdown timer.
/// </summary>
public class RefreshControlBase : ComponentBase, IDisposable
{
    private System.Timers.Timer? _countdownTimer;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the callback invoked when refresh is requested.
    /// </summary>
    [Parameter]
    public EventCallback OnRefresh { get; set; }

    /// <summary>
    /// Gets or sets whether auto-refresh is enabled.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool AutoRefreshEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the auto-refresh interval in seconds.
    /// Default is 10.
    /// </summary>
    [Parameter]
    public int RefreshIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether the refresh is currently in progress.
    /// </summary>
    [Parameter]
    public bool IsRefreshing { get; set; }

    /// <summary>
    /// Gets or sets whether auto-refresh is paused.
    /// </summary>
    [Parameter]
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets the callback when pause state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsPausedChanged { get; set; }

    /// <summary>
    /// Gets or sets whether to show the countdown timer.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool ShowCountdown { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the "Refresh" label on the button.
    /// Default is false (icon only).
    /// </summary>
    [Parameter]
    public bool ShowRefreshLabel { get; set; }

    /// <summary>
    /// Gets or sets whether to show the "Pause/Resume" label on the button.
    /// Default is false (icon only).
    /// </summary>
    [Parameter]
    public bool ShowPauseLabel { get; set; }

    /// <summary>
    /// Gets or sets whether to allow editing the refresh interval.
    /// Default is true. When true, shows an input field when refresh is stopped.
    /// </summary>
    [Parameter]
    public bool AllowIntervalEditing { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback when refresh interval changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> RefreshIntervalSecondsChanged { get; set; }

    /// <summary>
    /// Gets the remaining seconds until next refresh.
    /// </summary>
    protected int RemainingSeconds { get; private set; }

    /// <summary>
    /// Gets or sets the temporary interval value for editing.
    /// </summary>
    protected int EditingIntervalSeconds { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RemainingSeconds = RefreshIntervalSeconds;
        EditingIntervalSeconds = RefreshIntervalSeconds;
        StartCountdownTimer();
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync editing interval with parameter changes
        if (EditingIntervalSeconds != RefreshIntervalSeconds)
        {
            EditingIntervalSeconds = RefreshIntervalSeconds;
        }

        // Reset countdown when refresh completes
        if (!IsRefreshing && RemainingSeconds <= 0)
        {
            RemainingSeconds = RefreshIntervalSeconds;
        }
    }

    /// <summary>
    /// Gets the CSS class for the refresh icon.
    /// </summary>
    /// <returns>The icon CSS class.</returns>
    protected string GetRefreshIconClass()
    {
        return IsRefreshing ? "bi-arrow-clockwise spin" : "bi-arrow-clockwise";
    }

    /// <summary>
    /// Gets the CSS class for the pause button.
    /// </summary>
    /// <returns>The button CSS class.</returns>
    protected string GetPauseButtonClass()
    {
        return IsPaused ? "btn-outline-success" : "btn-outline-warning";
    }

    /// <summary>
    /// Gets the CSS class for the pause icon.
    /// </summary>
    /// <returns>The icon CSS class.</returns>
    protected string GetPauseIconClass()
    {
        return IsPaused ? "bi-play-fill" : "bi-pause-fill";
    }

    /// <summary>
    /// Gets the label text for the pause button.
    /// </summary>
    /// <returns>The label text.</returns>
    protected string GetPauseLabel()
    {
        return IsPaused ? "Resume" : "Pause";
    }

    /// <summary>
    /// Gets the tooltip text for the pause button.
    /// </summary>
    /// <returns>The tooltip text.</returns>
    protected string GetPauseTooltip()
    {
        return IsPaused ? "Resume auto-refresh" : "Pause auto-refresh";
    }

    /// <summary>
    /// Handles refresh button click events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnRefreshClickedAsync()
    {
        RemainingSeconds = RefreshIntervalSeconds;

        if (OnRefresh.HasDelegate)
        {
            await OnRefresh.InvokeAsync();
        }
    }

    /// <summary>
    /// Handles pause/resume button click events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnPauseToggledAsync()
    {
        IsPaused = !IsPaused;

        if (IsPausedChanged.HasDelegate)
        {
            await IsPausedChanged.InvokeAsync(IsPaused);
        }

        if (!IsPaused)
        {
            RemainingSeconds = RefreshIntervalSeconds;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Handles when the refresh interval is changed by the user.
    /// </summary>
    /// <param name="newInterval">The new interval in seconds.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnIntervalChangedAsync(int newInterval)
    {
        if (newInterval > 0 && newInterval != RefreshIntervalSeconds)
        {
            EditingIntervalSeconds = newInterval;
            
            if (RefreshIntervalSecondsChanged.HasDelegate)
            {
                await RefreshIntervalSecondsChanged.InvokeAsync(newInterval);
            }
        }
    }

    /// <summary>
    /// Gets whether the interval input should be shown (when refresh is stopped).
    /// </summary>
    protected bool ShowIntervalInput => AllowIntervalEditing && (!AutoRefreshEnabled || IsPaused);

    /// <summary>
    /// Starts the countdown timer for auto-refresh.
    /// </summary>
    private void StartCountdownTimer()
    {
        _countdownTimer = new System.Timers.Timer(1000); // 1 second
        _countdownTimer.Elapsed += OnCountdownTick;
        _countdownTimer.AutoReset = true;
        _countdownTimer.Start();
    }

    /// <summary>
    /// Handles countdown timer ticks.
    /// </summary>
    private async void OnCountdownTick(object? sender, ElapsedEventArgs e)
    {
        if (IsRefreshing || IsPaused || !AutoRefreshEnabled)
        {
            return;
        }

        RemainingSeconds--;

        if (RemainingSeconds <= 0)
        {
            RemainingSeconds = RefreshIntervalSeconds;

            if (OnRefresh.HasDelegate)
            {
                await InvokeAsync(async () =>
                {
                    await OnRefresh.InvokeAsync();
                });
            }
        }

        await InvokeAsync(StateHasChanged);
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
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Elapsed -= OnCountdownTick;
                _countdownTimer.Dispose();
                _countdownTimer = null;
            }
        }

        _disposed = true;
    }
}
