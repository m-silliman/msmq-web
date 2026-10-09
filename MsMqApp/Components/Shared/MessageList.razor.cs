using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying a virtualized list of MSMQ messages with sorting, filtering, and selection.
/// </summary>
public class MessageListBase : ComponentBase, IAsyncDisposable
{
    private string _searchQuery = string.Empty;
    private string _currentSortColumn = "ArrivedTime";
    private bool _sortAscending = false;
    private int _lastListVersion = -1;
    private bool? _lastReadFromTail;
    private bool _loadingNextPage;
    private bool _isLoadingBodySearch;
    private bool _disposed;
    private IJSObjectReference? _scrollModule;
    private DotNetObjectReference<MessageListBase>? _scrollReference;
    protected ElementReference ListElement;

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public IMsmqService MsmqService { get; set; } = default!;
    [Parameter] public bool ReadFromTail { get; set; } = true;
    [Parameter] public EventCallback<bool> ReadFromTailChanged { get; set; }
    [Parameter] public int BatchSize { get; set; } = 100;
    [Parameter] public EventCallback<int> BatchSizeChanged { get; set; }
    [Parameter] public EventCallback OnLoadMore { get; set; }
    [Parameter] public bool HasMore { get; set; }
    [Parameter] public bool IsLoadingMore { get; set; }
    [Parameter] public string? PagingError { get; set; }
    [Parameter] public int ListVersion { get; set; }

    protected bool CanAutoLoad => HasMore && !IsLoading && !IsLoadingMore
        && PagingError == null && string.IsNullOrWhiteSpace(SearchQuery);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            _scrollModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/message-list.js");
            if (_disposed) { await _scrollModule.DisposeAsync(); return; }
            _scrollReference = DotNetObjectReference.Create(this);
            await _scrollModule.InvokeVoidAsync("attach", ListElement, _scrollReference);
        }
        catch (JSDisconnectedException) { }
    }

    protected Task ChangeBatchSizeAsync(ChangeEventArgs args)
    {
        return int.TryParse(args.Value?.ToString(), out var size) && size is 100 or 200 or 500
            ? BatchSizeChanged.InvokeAsync(size)
            : Task.CompletedTask;
    }

    [JSInvokable]
    public Task BrowseMessagesAsync(int version, bool loadMore)
    {
        if (_disposed || version != ListVersion || IsLoading) return Task.CompletedTask;
        IsPaused = true;
        StateHasChanged();
        return loadMore && CanAutoLoad ? LoadMoreAsync() : Task.CompletedTask;
    }

    protected async Task LoadMoreAsync()
    {
        if (_disposed || _loadingNextPage || IsLoading || IsLoadingMore || !HasMore) return;
        _loadingNextPage = true;
        IsPaused = true;
        try { await OnLoadMore.InvokeAsync(); }
        finally { _loadingNextPage = false; }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_scrollModule != null)
        {
            try
            {
                await _scrollModule.InvokeVoidAsync("detach", ListElement);
                await _scrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        _scrollReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets or sets the list of messages to display.
    /// </summary>
    [Parameter]
    public List<QueueMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the callback invoked when a message is selected.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage?> OnMessageSelected { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the selection changes.
    /// </summary>
    [Parameter]
    public EventCallback<List<QueueMessage>> OnSelectionChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when refresh is requested.
    /// </summary>
    [Parameter]
    public EventCallback OnRefresh { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when messages are deleted.
    /// </summary>
    [Parameter]
    public EventCallback<List<string>> OnMessagesDeleted { get; set; }

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
    /// Gets or sets the callback when RefreshIntervalSeconds changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> RefreshIntervalSecondsChanged { get; set; }

    /// <summary>
    /// Gets or sets whether the list is currently loading.
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets whether a refresh is in progress.
    /// </summary>
    [Parameter]
    public bool IsRefreshing { get; set; }

    /// <summary>
    /// Gets or sets the callback when IsRefreshing changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsRefreshingChanged { get; set; }

    /// <summary>
    /// Gets or sets the currently selected queue.
    /// Used to display queue information in the header.
    /// </summary>
    [Parameter]
    public QueueInfo? SelectedQueue { get; set; }

    /// <summary>
    /// Gets or sets the message operations service for performing bulk operations.
    /// </summary>
    [Parameter]
    public IMessageOperationsService? MessageOperationsService { get; set; }

    /// <summary>
    /// Gets or sets the view type indicating whether showing queue messages or journal messages.
    /// Default is QueueMessages.
    /// </summary>
    [Parameter]
    public QueueViewType ViewType { get; set; } = QueueViewType.QueueMessages;

    /// <summary>
    /// Gets or sets the currently selected message.
    /// </summary>
    [Parameter]
    public QueueMessage? SelectedMessage { get; set; }

    /// <summary>
    /// Gets or sets the callback when SelectedMessage changes.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage?> SelectedMessageChanged { get; set; }

    /// <summary>
    /// Gets or sets the search query for filtering messages.
    /// </summary>
    protected string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether auto-refresh is paused.
    /// </summary>
    protected bool IsPaused { get; set; }

    /// <summary>
    /// Gets whether message bodies should be included in the search.
    /// </summary>
    protected bool SearchBody { get; set; }

    /// <summary>
    /// Gets whether bodies for the current page are being loaded for search.
    /// </summary>
    protected bool IsLoadingBodySearch => _isLoadingBodySearch;

    /// <summary>
    /// Gets the list of selected messages.
    /// </summary>
    protected List<QueueMessage> SelectedMessages { get; } = new();

    /// <summary>
    /// Gets the filtered and sorted list of messages.
    /// </summary>
    protected List<QueueMessage> FilteredMessages { get; private set; } = new();

    /// <summary>
    /// Gets or sets whether the delete confirmation dialog is visible.
    /// </summary>
    protected bool ShowDeleteConfirmation { get; set; }

    /// <summary>
    /// Gets or sets whether a delete operation is in progress.
    /// </summary>
    protected bool IsDeleting { get; set; }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_lastListVersion != ListVersion)
        {
            SelectedMessages.Clear();
            SearchBody = false;
            _lastListVersion = ListVersion;
        }
        if (_lastReadFromTail != ReadFromTail)
        {
            _currentSortColumn = "ArrivedTime";
            _sortAscending = !ReadFromTail;
            _lastReadFromTail = ReadFromTail;
        }
        ApplyFilterAndSort();
    }

    /// <summary>
    /// Gets the message count text for display.
    /// </summary>
    /// <returns>The message count text.</returns>
    protected string GetMessageCountText()
    {
        return $"{Messages.Count:N0} loaded";
    }

    /// <summary>
    /// Gets the accessible label and tooltip for the message time sort button.
    /// </summary>
    protected string GetTailSortLabel() => ReadFromTail ? "Newest messages first" : "Oldest messages first";

    /// <summary>
    /// Gets the view type display text for the header.
    /// </summary>
    /// <returns>The view type display text.</returns>
    protected string GetViewTypeText()
    {
        return ViewType switch
        {
            QueueViewType.QueueMessages => "Queue Messages",
            QueueViewType.JournalMessages => "Journal Messages",
            _ => "Messages"
        };
    }

    /// <summary>
    /// Gets the icon class for the view type.
    /// </summary>
    /// <returns>The icon CSS class.</returns>
    protected string GetViewTypeIcon()
    {
        return ViewType switch
        {
            QueueViewType.QueueMessages => "bi bi-envelope-fill",
            QueueViewType.JournalMessages => "bi bi-journal-text",
            _ => "bi bi-folder2-open"
        };
    }

    /// <summary>
    /// Gets the empty state message based on view type.
    /// </summary>
    /// <returns>The empty state message.</returns>
    protected string GetEmptyStateMessage()
    {
        return ViewType switch
        {
            QueueViewType.QueueMessages => "No messages in queue",
            QueueViewType.JournalMessages => "No journal messages",
            _ => "No messages"
        };
    }

    /// <summary>
    /// Gets the queue display name based on view type.
    /// For journal messages, appends the journal path suffix.
    /// </summary>
    /// <returns>The queue display name.</returns>
    protected string GetQueueDisplayName()
    {
        if (SelectedQueue == null)
        {
            return string.Empty;
        }

        // For journal messages, show the journal path if available
        if (ViewType == QueueViewType.JournalMessages && !string.IsNullOrEmpty(SelectedQueue.JournalPath))
        {
            return SelectedQueue.JournalPath;
        }

        // For regular queue messages, use FormatName or Path
        return !string.IsNullOrEmpty(SelectedQueue.FormatName)
            ? SelectedQueue.FormatName
            : SelectedQueue.Path;
    }

    /// <summary>
    /// Gets the sort icon for a column.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <returns>The sort icon markup.</returns>
    protected MarkupString GetSortIcon(string columnName)
    {
        if (_currentSortColumn != columnName)
        {
            return new MarkupString("<i class=\"bi bi-chevron-expand sort-icon-inactive\" aria-hidden=\"true\"></i>");
        }

        var icon = _sortAscending ? "bi-chevron-up" : "bi-chevron-down";
        return new MarkupString($"<i class=\"bi {icon} sort-icon-active\" aria-hidden=\"true\"></i>");
    }

    /// <summary>
    /// Determines whether a message is selected.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True if the message is selected, false otherwise.</returns>
    protected bool IsMessageSelected(QueueMessage message)
    {
        return SelectedMessages.Any(m => m.Id == message.Id);
    }

    /// <summary>
    /// Determines whether all filtered messages are selected.
    /// </summary>
    /// <returns>True if all messages are selected, false otherwise.</returns>
    protected bool AreAllSelected()
    {
        return FilteredMessages.Count > 0 && FilteredMessages.All(IsMessageSelected);
    }

    /// <summary>
    /// Handles search query changes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OnSearchChangedAsync()
    {
        ApplyFilterAndSort();
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Enables or disables body search. Enabling it loads bodies for the messages already in the list.
    /// </summary>
    protected async Task ChangeSearchBodyAsync(ChangeEventArgs args)
    {
        SearchBody = args.Value is bool value && value;
        if (SearchBody)
        {
            await LoadBodiesForSearchAsync();
        }

        ApplyFilterAndSort();
        StateHasChanged();
    }

    /// <summary>
    /// Toggles whether the newest or oldest messages are displayed first.
    /// </summary>
    protected Task ToggleTailAsync()
    {
        IsPaused = false;
        return ReadFromTailChanged.InvokeAsync(!ReadFromTail);
    }

    /// <summary>
    /// Clears the search query.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task ClearSearchAsync()
    {
        SearchQuery = string.Empty;
        ApplyFilterAndSort();
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Toggles the sort order for a column.
    /// </summary>
    /// <param name="columnName">The column name to sort by.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task ToggleSortAsync(string columnName)
    {
        if (_currentSortColumn == columnName)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _currentSortColumn = columnName;
            _sortAscending = true;
        }

        ApplyFilterAndSort();
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Selects a message.
    /// </summary>
    /// <param name="message">The message to select.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task SelectMessageAsync(QueueMessage message)
    {
        SelectedMessage = message;

        if (SelectedMessageChanged.HasDelegate)
        {
            await SelectedMessageChanged.InvokeAsync(message);
        }

        if (OnMessageSelected.HasDelegate)
        {
            await OnMessageSelected.InvokeAsync(message);
        }

        StateHasChanged();
    }

    /// <summary>
    /// Toggles the selection of a message.
    /// </summary>
    /// <param name="message">The message to toggle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ToggleMessageSelectionAsync(QueueMessage message)
    {
        if (IsMessageSelected(message))
        {
            SelectedMessages.RemoveAll(m => m.Id == message.Id);
        }
        else
        {
            SelectedMessages.Add(message);
        }

        if (OnSelectionChanged.HasDelegate)
        {
            await OnSelectionChanged.InvokeAsync(SelectedMessages);
        }

        StateHasChanged();
    }

    /// <summary>
    /// Toggles the selection of all filtered messages.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ToggleSelectAllAsync()
    {
        if (AreAllSelected())
        {
            // Deselect all
            foreach (var message in FilteredMessages)
            {
                SelectedMessages.RemoveAll(m => m.Id == message.Id);
            }
        }
        else
        {
            // Select all
            foreach (var message in FilteredMessages)
            {
                if (!IsMessageSelected(message))
                {
                    SelectedMessages.Add(message);
                }
            }
        }

        if (OnSelectionChanged.HasDelegate)
        {
            await OnSelectionChanged.InvokeAsync(SelectedMessages);
        }

        StateHasChanged();
    }

    /// <summary>
    /// Handles refresh requests.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleRefreshAsync()
    {
        if (OnRefresh.HasDelegate)
        {
            await OnRefresh.InvokeAsync();
        }
    }

    /// <summary>
    /// Shows the delete confirmation dialog for selected messages.
    /// </summary>
    protected void ShowDeleteSelectedDialog()
    {
        if (SelectedMessages.Count > 0)
        {
            ShowDeleteConfirmation = true;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles the delete confirmation result.
    /// </summary>
    /// <param name="confirmed">Whether the user confirmed the deletion.</param>
    protected async Task HandleDeleteConfirmationAsync(bool confirmed)
    {
        ShowDeleteConfirmation = false;
        
        if (confirmed && SelectedMessages.Count > 0)
        {
            await DeleteSelectedMessagesAsync();
        }
        
        StateHasChanged();
    }

    /// <summary>
    /// Deletes the selected messages.
    /// </summary>
    private async Task DeleteSelectedMessagesAsync()
    {
        if (MessageOperationsService == null || SelectedQueue == null || SelectedMessages.Count == 0)
        {
            return;
        }

        IsDeleting = true;
        StateHasChanged();

        try
        {
            var messageIds = SelectedMessages.Select(m => m.Id).ToList();
            var queuePath = ViewType == QueueViewType.JournalMessages 
                ? SelectedQueue.JournalPath 
                : (!string.IsNullOrEmpty(SelectedQueue.FormatName) ? SelectedQueue.FormatName : SelectedQueue.Path);

            var result = await MessageOperationsService.DeleteMessagesAsync(queuePath, messageIds);

            if (result.Success)
            {
                // Clear selected messages
                SelectedMessages.Clear();

                // Notify parent component about deleted messages
                if (OnMessagesDeleted.HasDelegate)
                {
                    await OnMessagesDeleted.InvokeAsync(messageIds);
                }

                // Trigger refresh
                if (OnRefresh.HasDelegate)
                {
                    await OnRefresh.InvokeAsync();
                }
            }
            // TODO: Handle errors with user notification
        }
        finally
        {
            IsDeleting = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets the delete button text based on selected count.
    /// </summary>
    protected string GetDeleteButtonText()
    {
        return SelectedMessages.Count == 1 
            ? "Delete 1 Message" 
            : $"Delete {SelectedMessages.Count} Messages";
    }

    /// <summary>
    /// Applies filtering and sorting to the message list.
    /// </summary>
    private void ApplyFilterAndSort()
    {
        var filtered = Messages.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.Trim();
            filtered = filtered.Where(m =>
                ContainsSearchText(m.Label, query) ||
                ContainsSearchText(m.Id, query) ||
                ContainsSearchText(m.CorrelationId, query) ||
                (SearchBody && m.IsBodyLoaded && ContainsSearchText(m.Body.RawContent, query)));
        }

        // Apply sorting
        filtered = _currentSortColumn switch
        {
            "Label" => _sortAscending
                ? filtered.OrderBy(m => m.Label)
                : filtered.OrderByDescending(m => m.Label),
            "Priority" => _sortAscending
                ? filtered.OrderBy(m => m.Priority)
                : filtered.OrderByDescending(m => m.Priority),
            "ArrivedTime" => _sortAscending
                ? filtered.OrderBy(m => m.ArrivedTime)
                : filtered.OrderByDescending(m => m.ArrivedTime),
            _ => filtered.OrderByDescending(m => m.ArrivedTime)
        };

        FilteredMessages = filtered.ToList();
    }

    private static bool ContainsSearchText(string? value, string query) =>
        !string.IsNullOrEmpty(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private async Task LoadBodiesForSearchAsync()
    {
        if (_isLoadingBodySearch || MsmqService == null) return;

        _isLoadingBodySearch = true;
        var listVersion = ListVersion;
        var messagesToLoad = Messages.Where(m => !m.IsBodyLoaded).ToList();
        try
        {
            foreach (var message in messagesToLoad)
            {
                if (_disposed || !SearchBody || listVersion != ListVersion) return;

                var result = message.LookupId != 0
                    ? await MsmqService.GetMessageByLookupIdAsync(message.QueuePath, message.LookupId)
                    : await MsmqService.GetMessageByIdAsync(message.QueuePath, message.Id);

                if (result.Success && result.Data != null)
                {
                    message.Body = result.Data.Body;
                    message.IsBodyLoaded = true;
                }
            }
        }
        finally
        {
            _isLoadingBodySearch = false;
        }
    }
}
