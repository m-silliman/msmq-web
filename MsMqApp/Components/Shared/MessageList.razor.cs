using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying a virtualized list of MSMQ messages with sorting, filtering, and selection.
/// </summary>
public class MessageListBase : ComponentBase
{
    private string _searchQuery = string.Empty;
    private string _currentSortColumn = "ArrivedTime";
    private bool _sortAscending = false;

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
    /// Default is 5.
    /// </summary>
    [Parameter]
    public int RefreshIntervalSeconds { get; set; } = 5;

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
        ApplyFilterAndSort();
    }

    /// <summary>
    /// Gets the message count text for display.
    /// </summary>
    /// <returns>The message count text.</returns>
    protected string GetMessageCountText()
    {
        if (FilteredMessages.Count != Messages.Count)
        {
            return $"{FilteredMessages.Count} of {Messages.Count} messages";
        }

        return $"{Messages.Count} message{(Messages.Count != 1 ? "s" : "")}";
    }

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
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(m =>
                m.Label.ToLowerInvariant().Contains(query) ||
                m.Id.ToLowerInvariant().Contains(query) ||
                m.CorrelationId.ToLowerInvariant().Contains(query));
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
            "Size" => _sortAscending
                ? filtered.OrderBy(m => m.Body.SizeBytes)
                : filtered.OrderByDescending(m => m.Body.SizeBytes),
            _ => filtered.OrderByDescending(m => m.ArrivedTime)
        };

        FilteredMessages = filtered.ToList();
    }
}
