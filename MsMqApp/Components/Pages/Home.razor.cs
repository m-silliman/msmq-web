using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MsMqApp.Components.Shared;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;
using MsMqApp.Models.UI;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Components.Pages;

/// <summary>
/// Home page component integrating QueueTreeView, MessageList, and MessageDetail
/// with auto-refresh and message operations.
/// </summary>
public class HomeBase : ComponentBase, IAsyncDisposable
{
    private bool _disposed;
    private DotNetObjectReference<HomeBase>? _dotNetRef;
    private QueueMessage? _pendingOperationMessage;
    private PendingOperation _pendingOperation = PendingOperation.None;

    // Panel sizing
    private const int MinLeftPanelWidth = 200;
    private const int MaxLeftPanelWidth = 600;
    private const int DefaultLeftPanelWidth = 350;
    private int _leftPanelWidthPercent = 30; // 30% default

    // Services
    [Inject] protected IMsmqService MsmqService { get; set; } = default!;
    [Inject] protected IMessageOperationsService MessageOperationsService { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

    // State
    protected QueueConnection? CurrentConnection { get; set; }
    protected QueueInfo? SelectedQueue { get; set; }
    protected QueueViewType CurrentViewType { get; set; } = QueueViewType.QueueMessages;
    protected QueueMessage? SelectedMessage { get; set; }
    protected List<QueueMessage> Messages { get; set; } = new();
    protected List<QueueMessage> SelectedMessages { get; set; } = new();

    // UI State
    protected bool IsDetailDrawerOpen { get; set; }
    protected bool IsLoadingMessages { get; set; }
    protected bool IsRefreshing { get; set; }
    protected bool AutoRefreshEnabled { get; set; } = true;
    protected int RefreshIntervalSeconds { get; set; } = 5;

    // Confirmation Dialog State
    protected bool IsConfirmDialogOpen { get; set; }
    protected string ConfirmDialogTitle { get; set; } = string.Empty;
    protected string ConfirmDialogMessage { get; set; } = string.Empty;
    protected DialogSeverity ConfirmDialogSeverity { get; set; } = DialogSeverity.Warning;
    protected string ConfirmButtonText { get; set; } = "Confirm";

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Initialize with a default local connection
        CurrentConnection = new QueueConnection
        {
            ComputerName = Environment.MachineName,
            DisplayName = "Local Computer",
            IsLocal = true,
            Status = ConnectionStatus.Connected
        };

        // Load queues
        await RefreshQueuesAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // Create DotNetObjectReference for JS interop
            _dotNetRef = DotNetObjectReference.Create(this);

            // Setup resize listeners
            await JSRuntime.InvokeVoidAsync("eval", @"
                document.addEventListener('mousemove', window.homePageResizeHandler);
                document.addEventListener('mouseup', window.stopHomePageResize);
            ");
        }
    }

    #region Queue Operations

    /// <summary>
    /// Handles node selection from the tree view.
    /// Extracts queue info and view type to load appropriate messages.
    /// </summary>
    protected async Task HandleNodeSelectedAsync(TreeNodeData? nodeData)
    {
        if (nodeData == null)
        {
            SelectedQueue = null;
            CurrentViewType = QueueViewType.QueueMessages;
            Messages.Clear();
            SelectedMessage = null;
            IsDetailDrawerOpen = false;
            StateHasChanged();
            return;
        }

        // Extract queue info from node data
        var queueInfo = nodeData.Data as QueueInfo;

        Console.WriteLine($"[DEBUG] HandleNodeSelectedAsync - NodeId: {nodeData.Id}, ViewType: {nodeData.ViewType}, HasQueueInfo: {queueInfo != null}");
        if (queueInfo != null)
        {
            Console.WriteLine($"[DEBUG] QueueInfo - Name: {queueInfo.Name}, Path: {queueInfo.Path}, JournalPath: {queueInfo.JournalPath}");
        }

        // Only load messages for queue message and journal message nodes
        if (nodeData.ViewType == QueueViewType.QueueMessages ||
            nodeData.ViewType == QueueViewType.JournalMessages)
        {
            SelectedQueue = queueInfo;
            CurrentViewType = nodeData.ViewType;
            SelectedMessage = null;
            IsDetailDrawerOpen = false;

            if (queueInfo != null)
            {
                // For journal messages, use JournalPath; for regular messages, use FormatName or Path
                var queuePath = nodeData.ViewType == QueueViewType.JournalMessages
                    ? queueInfo.JournalPath
                    : (!string.IsNullOrEmpty(queueInfo.FormatName) ? queueInfo.FormatName : queueInfo.Path);

                Console.WriteLine($"[DEBUG] QueueInfo FormatName: '{queueInfo.FormatName}', Path: '{queueInfo.Path}'");
                Console.WriteLine($"[DEBUG] Selected path for {nodeData.ViewType}: {queuePath}");
                await LoadMessagesAsync(queuePath, nodeData.ViewType);
            }
            else
            {
                Messages.Clear();
            }
        }
        else
        {
            // For folder or queue nodes, just update state without loading messages
            SelectedQueue = queueInfo;
            CurrentViewType = nodeData.ViewType;
            Messages.Clear();
            SelectedMessage = null;
            IsDetailDrawerOpen = false;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Handles queue selection from the tree view (legacy compatibility).
    /// </summary>
    protected async Task HandleQueueSelectedAsync(QueueInfo? queue)
    {
        SelectedQueue = queue;
        CurrentViewType = QueueViewType.QueueMessages;
        SelectedMessage = null;
        IsDetailDrawerOpen = false;

        if (queue != null)
        {
            // Use FormatName if available, otherwise fall back to Path
            var queuePath = !string.IsNullOrEmpty(queue.FormatName) ? queue.FormatName : queue.Path;
            await LoadMessagesAsync(queuePath, QueueViewType.QueueMessages);
        }
        else
        {
            Messages.Clear();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Refreshes the queue list.
    /// </summary>
    protected async Task RefreshQueuesAsync()
    {
        if (CurrentConnection == null) return;

        try
        {
            CurrentConnection.IsRefreshing = true;
            StateHasChanged();

            var result = await MsmqService.GetQueuesAsync(
                CurrentConnection.ComputerName,
                includeSystemQueues: false);

            if (result.Success && result.Data != null)
            {
                CurrentConnection.Queues = result.Data.ToList();
                CurrentConnection.Status = ConnectionStatus.Connected;

                // Populate journal information for each queue
                foreach (var queue in CurrentConnection.Queues)
                {
                    // Always try to get journal info - some queues may have journaling even if flag isn't set
                    if (!string.IsNullOrEmpty(queue.Path))
                    {
                        // Use FormatName or Path depending on what's available
                        var queuePath = !string.IsNullOrEmpty(queue.FormatName) ? queue.FormatName : queue.Path;

                        // Construct journal path (uppercase JOURNAL for FormatName, lowercase for regular paths)
                        queue.JournalPath = queuePath.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase)
                            ? $"{queuePath};journal"
                            : $"FormatName:{queuePath};journal";

                        // Get journal message count using the queue path/formatname
                        var journalCountResult = await MsmqService.GetJournalMessageCountAsync(queuePath);
                        if (journalCountResult.Success)
                        {
                            queue.JournalMessageCount = journalCountResult.Data;
                        }
                    }
                }
            }
            else
            {
                CurrentConnection.Status = ConnectionStatus.Failed;
            }
        }
        catch (Exception)
        {
            CurrentConnection.Status = ConnectionStatus.Failed;
        }
        finally
        {
            CurrentConnection.IsRefreshing = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Message Operations

    /// <summary>
    /// Loads messages for the specified queue based on view type.
    /// </summary>
    private async Task LoadMessagesAsync(string queuePath, QueueViewType viewType)
    {
        IsLoadingMessages = true;
        StateHasChanged();

        try
        {
            // Debug logging to track what we're trying to load
            Console.WriteLine($"[DEBUG] LoadMessagesAsync called - ViewType: {viewType}, QueuePath: {queuePath}");

            OperationResult<IEnumerable<QueueMessage>> result;

            if (viewType == QueueViewType.JournalMessages)
            {
                Console.WriteLine($"[DEBUG] Loading JOURNAL messages from: {queuePath}");
                // Load journal messages
                result = await MsmqService.GetJournalMessagesAsync(
                    queuePath,
                    maxMessages: 1000);
            }
            else
            {
                Console.WriteLine($"[DEBUG] Loading REGULAR messages from: {queuePath}");
                // Load regular queue messages
                result = await MsmqService.GetMessagesAsync(
                    queuePath,
                    peekOnly: true,
                    maxMessages: 1000);
            }

            if (result.Success && result.Data != null)
            {
                Messages = result.Data.ToList();
                Console.WriteLine($"[DEBUG] Successfully loaded {Messages.Count} messages");
            }
            else
            {
                // Log the error for debugging
                Console.WriteLine($"Failed to load messages. ViewType: {viewType}, QueuePath: {queuePath}, Error: {result.ErrorMessage}");
                Messages.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception loading messages. ViewType: {viewType}, QueuePath: {queuePath}, Exception: {ex.Message}");
            Messages.Clear();
        }
        finally
        {
            IsLoadingMessages = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles message selection from the list.
    /// </summary>
    protected async Task HandleMessageSelectedAsync(QueueMessage? message)
    {
        SelectedMessage = message;
        IsDetailDrawerOpen = message != null;
        StateHasChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles multiple message selection changes.
    /// </summary>
    protected async Task HandleSelectionChangedAsync(List<QueueMessage> messages)
    {
        SelectedMessages = messages;
        StateHasChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Refreshes the message list for the currently selected queue.
    /// </summary>
    protected async Task RefreshMessagesAsync()
    {
        if (SelectedQueue != null)
        {
            IsRefreshing = true;
            StateHasChanged();

            // For journal messages, use JournalPath; for regular messages, use FormatName or Path
            var queuePath = CurrentViewType == QueueViewType.JournalMessages
                ? SelectedQueue.JournalPath
                : (!string.IsNullOrEmpty(SelectedQueue.FormatName) ? SelectedQueue.FormatName : SelectedQueue.Path);

            await LoadMessagesAsync(queuePath, CurrentViewType);

            IsRefreshing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Closes the message detail drawer.
    /// </summary>
    protected async Task CloseDetailDrawerAsync()
    {
        IsDetailDrawerOpen = false;
        SelectedMessage = null;
        StateHasChanged();
        await Task.CompletedTask;
    }

    #endregion

    #region Message Operations Handlers

    /// <summary>
    /// Handles the delete message request.
    /// </summary>
    protected async Task HandleDeleteMessageAsync(QueueMessage message)
    {
        _pendingOperationMessage = message;
        _pendingOperation = PendingOperation.Delete;

        ConfirmDialogTitle = "Delete Message";
        ConfirmDialogMessage = $"Are you sure you want to delete this message?\n\nLabel: {message.Label}\nID: {message.Id}";
        ConfirmDialogSeverity = DialogSeverity.Danger;
        ConfirmButtonText = "Delete";
        IsConfirmDialogOpen = true;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles the move message request.
    /// </summary>
    protected async Task HandleMoveMessageAsync(QueueMessage message)
    {
        // TODO: Implement move dialog to select target queue
        // For now, just show a placeholder
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles the export message request.
    /// </summary>
    protected async Task HandleExportMessageAsync(QueueMessage message)
    {
        if (SelectedQueue == null) return;

        try
        {
            // Export as JSON to default location
            var exportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"message_{message.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            var result = await MessageOperationsService.ExportMessageAsync(
                SelectedQueue.Path,
                message.Id,
                exportPath,
                ExportFormat.Json);

            if (result.Success)
            {
                // TODO: Show success toast/notification
            }
        }
        catch (Exception)
        {
            // TODO: Show error notification
        }
    }

    /// <summary>
    /// Handles the resend message request.
    /// </summary>
    protected async Task HandleResendMessageAsync(QueueMessage message)
    {
        if (SelectedQueue == null) return;

        try
        {
            var result = await MessageOperationsService.ResendMessageAsync(
                SelectedQueue.Path,
                SelectedQueue.Path,  // Resend to same queue
                message.Id);

            if (result.Success)
            {
                // TODO: Show success toast/notification
                await RefreshMessagesAsync();
            }
        }
        catch (Exception)
        {
            // TODO: Show error notification
        }
    }

    /// <summary>
    /// Handles confirmation dialog confirm action.
    /// </summary>
    protected async Task HandleConfirmAsync()
    {
        IsConfirmDialogOpen = false;

        switch (_pendingOperation)
        {
            case PendingOperation.Delete:
                await ExecuteDeleteMessageAsync();
                break;
            case PendingOperation.Purge:
                await ExecutePurgeQueueAsync();
                break;
        }

        _pendingOperation = PendingOperation.None;
        _pendingOperationMessage = null;
    }

    /// <summary>
    /// Handles confirmation dialog cancel action.
    /// </summary>
    protected async Task HandleCancelAsync()
    {
        IsConfirmDialogOpen = false;
        _pendingOperation = PendingOperation.None;
        _pendingOperationMessage = null;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes the delete message operation.
    /// </summary>
    private async Task ExecuteDeleteMessageAsync()
    {
        if (_pendingOperationMessage == null || SelectedQueue == null) return;

        try
        {
            var result = await MessageOperationsService.DeleteMessageAsync(
                SelectedQueue.Path,
                _pendingOperationMessage.Id);

            if (result.Success)
            {
                // Remove from local list
                Messages.Remove(_pendingOperationMessage);

                // Close drawer if this was the selected message
                if (SelectedMessage?.Id == _pendingOperationMessage.Id)
                {
                    await CloseDetailDrawerAsync();
                }

                await RefreshMessagesAsync();
            }
        }
        catch (Exception)
        {
            // TODO: Show error notification
        }
    }

    /// <summary>
    /// Executes the purge queue operation.
    /// </summary>
    private async Task ExecutePurgeQueueAsync()
    {
        if (SelectedQueue == null) return;

        try
        {
            var result = await MessageOperationsService.PurgeQueueAsync(SelectedQueue.Path);

            if (result.Success)
            {
                Messages.Clear();
                await CloseDetailDrawerAsync();
                await RefreshMessagesAsync();
            }
        }
        catch (Exception)
        {
            // TODO: Show error notification
        }
    }

    #endregion

    #region Panel Resizing

    /// <summary>
    /// Starts the panel resize operation.
    /// </summary>
    protected async Task StartResizeAsync()
    {
        if (_dotNetRef == null) return;

        await JSRuntime.InvokeVoidAsync("startHomePageResize", _dotNetRef);
    }

    /// <summary>
    /// Updates the panel width during resize.
    /// Called from JavaScript.
    /// </summary>
    [JSInvokable]
    public void UpdatePanelWidth(int widthPercent)
    {
        _leftPanelWidthPercent = widthPercent;
        StateHasChanged();
    }

    /// <summary>
    /// Gets the inline style for the left panel.
    /// </summary>
    protected string GetLeftPanelStyle()
    {
        return $"width: {_leftPanelWidthPercent}%;";
    }

    /// <summary>
    /// Gets the inline style for the right panel.
    /// </summary>
    protected string GetRightPanelStyle()
    {
        return $"width: {100 - _leftPanelWidthPercent}%;";
    }

    #endregion

    #region Disposal

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Clean up JS interop
        if (_dotNetRef != null)
        {
            await JSRuntime.InvokeVoidAsync("eval", @"
                window.homePageResizing = false;
                window.homePageDotNetRef = null;
                window.homePageResizeHandler = null;
                window.homePageStopResize = null;
            ");

            _dotNetRef.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    #endregion

    /// <summary>
    /// Enumeration of pending operations requiring confirmation.
    /// </summary>
    private enum PendingOperation
    {
        None,
        Delete,
        Purge
    }
}
