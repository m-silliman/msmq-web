using System.IO.Compression;
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
    /// Gets or sets whether the search syntax help is visible.
    /// </summary>
    protected bool ShowSearchHelp { get; set; }

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

    /// <summary>
    /// Gets or sets whether an export operation is in progress.
    /// </summary>
    protected bool IsExporting { get; set; }

    /// <summary>
    /// Gets or sets whether the export confirmation dialog is visible.
    /// </summary>
    protected bool ShowExportConfirmation { get; set; }

    /// <summary>
    /// Gets or sets the base name for the exported ZIP file.
    /// </summary>
    protected string ExportFileName { get; set; } = "messages";

    /// <summary>
    /// Gets or sets the export error shown in the confirmation dialog.
    /// </summary>
    protected string? ExportErrorMessage { get; set; }

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
    /// Shows or hides the search syntax help.
    /// </summary>
    protected void ToggleSearchHelp() => ShowSearchHelp = !ShowSearchHelp;

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
            var queuePath = GetOperationQueuePath();

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
    /// Shows the export confirmation dialog for the selected messages.
    /// </summary>
    protected void ShowExportSelectedDialog()
    {
        if (SelectedMessages.Count == 0)
        {
            return;
        }

        ExportFileName = $"messages_{DateTime.Now:yyyyMMdd_HHmmss}";
        ExportErrorMessage = null;
        ShowExportConfirmation = true;
        StateHasChanged();
    }

    /// <summary>
    /// Handles cancellation of the export confirmation dialog.
    /// </summary>
    protected Task HandleExportCancellationAsync()
    {
        ExportErrorMessage = null;
        ShowExportConfirmation = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Exports the selected messages as a single JSON file.
    /// </summary>
    protected async Task ExportSelectedMessagesAsync()
    {
        if (MessageOperationsService == null || SelectedQueue == null || SelectedMessages.Count == 0)
        {
            return;
        }

        IsExporting = true;
        ExportErrorMessage = null;
        StateHasChanged();

        string? exportDirectory = null;
        try
        {
            exportDirectory = Path.Combine(Path.GetTempPath(), "MsMqManager", $"export-{Guid.NewGuid():N}");
            Directory.CreateDirectory(exportDirectory);

            var queuePath = GetOperationQueuePath();
            var exportedFiles = new List<string>();
            var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var failedMessages = new List<string>();

            foreach (var message in SelectedMessages.ToList())
            {
                var fileName = GetUniqueMessageFileName(message, usedFileNames);
                var filePath = Path.Combine(exportDirectory, fileName);
                var result = await MessageOperationsService.ExportMessageAsync(
                    queuePath,
                    message.Id,
                    filePath,
                    ExportFormat.Json);

                if (result.Success)
                {
                    exportedFiles.Add(filePath);
                }
                else
                {
                    failedMessages.Add(message.Id);
                }
            }

            if (failedMessages.Count > 0 || exportedFiles.Count != SelectedMessages.Count)
            {
                var failedCount = SelectedMessages.Count - exportedFiles.Count;
                throw new InvalidOperationException(
                    $"Could not export {failedCount} of {SelectedMessages.Count} selected message{(SelectedMessages.Count == 1 ? "" : "s")}. No download was created.");
            }

            var zipFileName = $"{SanitizeZipFileName(ExportFileName)}.zip";
            var zipPath = Path.Combine(exportDirectory, zipFileName);
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var filePath in exportedFiles)
                {
                    archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath), CompressionLevel.Fastest);
                }
            }

            await using var zipStream = File.OpenRead(zipPath);
            using var streamReference = new DotNetStreamReference(zipStream);
            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", zipFileName, streamReference);

            ShowExportConfirmation = false;
        }
        catch (Exception exception)
        {
            ExportErrorMessage = exception is InvalidOperationException
                ? exception.Message
                : "The messages could not be exported. Please try again.";
        }
        finally
        {
            if (exportDirectory != null && Directory.Exists(exportDirectory))
            {
                Directory.Delete(exportDirectory, recursive: true);
            }

            IsExporting = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets a safe, unique JSON filename for an exported message.
    /// </summary>
    private static string GetUniqueMessageFileName(QueueMessage message, ISet<string> usedFileNames)
    {
        var label = SanitizeFileNamePart(message.Label, "message");
        var messageId = SanitizeFileNamePart(message.Id, "messageid");
        var baseName = $"{label}-{messageId}";
        var fileName = $"{baseName}.json";
        var suffix = 2;

        while (!usedFileNames.Add(fileName))
        {
            fileName = $"{baseName}-{suffix++}.json";
        }

        return fileName;
    }

    /// <summary>
    /// Removes characters that are unsafe in a Windows filename.
    /// </summary>
    private static string SanitizeFileNamePart(string? value, string fallback)
    {
        var safeCharacters = value?.Where(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_').ToArray() ?? Array.Empty<char>();
        var sanitized = new string(safeCharacters);
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }

    /// <summary>
    /// Normalizes the user-provided ZIP download name.
    /// </summary>
    private static string SanitizeZipFileName(string? value)
    {
        var name = value?.Trim() ?? string.Empty;
        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        var safeCharacters = name.Where(character =>
            char.IsLetterOrDigit(character) || character is ' ' or '-' or '_').ToArray();
        var sanitized = new string(safeCharacters).Trim();
        return string.IsNullOrWhiteSpace(sanitized)
            ? $"messages_{DateTime.Now:yyyyMMdd_HHmmss}"
            : sanitized;
    }

    /// <summary>
    /// Gets the queue path used by message operations for the current view.
    /// </summary>
    private string GetOperationQueuePath()
    {
        if (SelectedQueue == null)
        {
            return string.Empty;
        }

        if (ViewType == QueueViewType.JournalMessages && !string.IsNullOrEmpty(SelectedQueue.JournalPath))
        {
            return SelectedQueue.JournalPath;
        }

        return !string.IsNullOrEmpty(SelectedQueue.FormatName)
            ? SelectedQueue.FormatName
            : SelectedQueue.Path;
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
            var query = SearchQueryExpression.Parse(SearchQuery);
            filtered = filtered.Where(m => query.Matches(m, SearchBody));
        }

        // Apply sorting
        filtered = _currentSortColumn switch
        {
            "Label" => _sortAscending
                ? filtered.OrderBy(m => m.Label)
                : filtered.OrderByDescending(m => m.Label),
            "Id" => _sortAscending
                ? filtered.OrderBy(m => m.Id)
                : filtered.OrderByDescending(m => m.Id),
            "Size" => _sortAscending
                ? filtered.OrderBy(GetBodySizeForSort)
                : filtered.OrderByDescending(GetBodySizeForSort),
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

    private static long GetBodySizeForSort(QueueMessage message) =>
        message.BodySizeBytes ?? (message.IsBodyLoaded ? message.Body.SizeBytes : -1);

    private static bool ContainsSearchText(string? value, string query) =>
        !string.IsNullOrEmpty(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Represents a parsed message search expression. Terms use the same fields as the
    /// original search, while '+' and '|' combine terms and parentheses group them.
    /// </summary>
    private abstract class SearchQueryExpression
    {
        public abstract bool Matches(QueueMessage message, bool searchBody);

        public static SearchQueryExpression Parse(string query)
        {
            var parser = new SearchQueryParser(query);
            return parser.TryParse(out var expression)
                ? expression
                : new SearchTermExpression(query.Trim());
        }
    }

    private sealed class SearchTermExpression(string term) : SearchQueryExpression
    {
        public override bool Matches(QueueMessage message, bool searchBody) =>
            ContainsSearchText(message.Label, term) ||
            ContainsSearchText(message.Id, term) ||
            ContainsSearchText(message.CorrelationId, term) ||
            (searchBody && message.IsBodyLoaded && ContainsSearchText(message.Body.RawContent, term));
    }

    private sealed class SearchAndExpression(
        SearchQueryExpression left,
        SearchQueryExpression right) : SearchQueryExpression
    {
        public override bool Matches(QueueMessage message, bool searchBody) =>
            left.Matches(message, searchBody) && right.Matches(message, searchBody);
    }

    private sealed class SearchOrExpression(
        SearchQueryExpression left,
        SearchQueryExpression right) : SearchQueryExpression
    {
        public override bool Matches(QueueMessage message, bool searchBody) =>
            left.Matches(message, searchBody) || right.Matches(message, searchBody);
    }

    /// <summary>
    /// Parses terms, quoted phrases, '+', '|', and parentheses. AND has higher
    /// precedence than OR. Whitespace around operators and parentheses is ignored;
    /// whitespace inside a quoted term is part of that term.
    /// </summary>
    private sealed class SearchQueryParser(string input)
    {
        private int _position;

        public bool TryParse(out SearchQueryExpression expression)
        {
            expression = null!;
            SkipWhitespace();
            if (_position >= input.Length || !TryParseOr(out expression))
            {
                return false;
            }

            SkipWhitespace();
            return _position == input.Length;
        }

        private bool TryParseOr(out SearchQueryExpression expression)
        {
            if (!TryParseAnd(out expression))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (!TryConsume('|'))
                {
                    return true;
                }

                if (!TryParseAnd(out var right))
                {
                    expression = null!;
                    return false;
                }

                expression = new SearchOrExpression(expression, right);
            }
        }

        private bool TryParseAnd(out SearchQueryExpression expression)
        {
            if (!TryParsePrimary(out expression))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (!TryConsume('+'))
                {
                    return true;
                }

                if (!TryParsePrimary(out var right))
                {
                    expression = null!;
                    return false;
                }

                expression = new SearchAndExpression(expression, right);
            }
        }

        private bool TryParsePrimary(out SearchQueryExpression expression)
        {
            expression = null!;
            SkipWhitespace();
            if (_position >= input.Length)
            {
                return false;
            }

            if (TryConsume('('))
            {
                if (!TryParseOr(out expression))
                {
                    return false;
                }

                SkipWhitespace();
                return TryConsume(')');
            }

            if (input[_position] == ')' || input[_position] == '|' || input[_position] == '+')
            {
                return false;
            }

            var term = input[_position] == '"'
                ? ReadQuotedTerm()
                : ReadUnquotedTerm();

            if (term == null || term.Length == 0)
            {
                return false;
            }

            expression = new SearchTermExpression(term);
            return true;
        }

        private string? ReadQuotedTerm()
        {
            _position++; // Opening quote.
            var builder = new System.Text.StringBuilder();

            while (_position < input.Length)
            {
                var character = input[_position++];
                if (character == '"')
                {
                    return builder.ToString();
                }

                if (character == '\\' && _position < input.Length)
                {
                    var next = input[_position];
                    if (next is '"' or '\\')
                    {
                        builder.Append(next);
                        _position++;
                        continue;
                    }
                }

                builder.Append(character);
            }

            return null;
        }

        private string ReadUnquotedTerm()
        {
            var start = _position;
            while (_position < input.Length && input[_position] is not ('+' or '|' or '(' or ')' or '"'))
            {
                _position++;
            }

            return input[start.._position].Trim();
        }

        private void SkipWhitespace()
        {
            while (_position < input.Length && char.IsWhiteSpace(input[_position]))
            {
                _position++;
            }
        }

        private bool TryConsume(char character)
        {
            if (_position >= input.Length || input[_position] != character)
            {
                return false;
            }

            _position++;
            return true;
        }
    }

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
