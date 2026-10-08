using System.Text.Json;
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
    // Static field to persist refresh interval across queue selections and component lifecycle
    private static int _persistentRefreshIntervalSeconds = 10;
private const string OutgoingQueueReadNotSupportedMessage = "Outgoing queues support monitoring counts, but reading individual outgoing messages is not supported.";
    
    private bool _disposed;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _listRequest;
    private CancellationTokenSource? _detailRequest;
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
    [Inject] protected IQueueConnectionManager ConnectionManager { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    // State
    protected QueueConnection? CurrentConnection { get; set; }
    protected QueueInfo? SelectedQueue { get; set; }
    protected QueueViewType CurrentViewType { get; set; } = QueueViewType.QueueMessages;
    protected QueueMessage? SelectedMessage { get; set; }
    protected List<QueueMessage> Messages { get; set; } = new();
    protected List<QueueMessage> SelectedMessages { get; set; } = new();
    protected TreeNodeData? SelectedFolderNode { get; set; }
    protected List<QueueInfo> FilteredQueues { get; set; } = new();

    // UI State
    protected bool IsDetailDrawerOpen { get; set; }
    protected bool IsLoadingMessages { get; set; }
    protected bool IsLoadingQueueProperties { get; set; }
    protected string? QueuePropertiesError { get; set; }
    protected bool IsRefreshing { get; set; }
    protected bool AutoRefreshEnabled { get; set; } = true;
    protected bool IsPurgeDialogOpen { get; set; }
    protected bool IsPurgeProcessing { get; set; }
    protected string? PurgeErrorMessage { get; set; }
    protected string? OperationFeedbackMessage { get; set; }
    protected string OperationFeedbackClass { get; set; } = "alert-success";
    protected int RefreshIntervalSeconds
    {
        get => _persistentRefreshIntervalSeconds;
        set => _persistentRefreshIntervalSeconds = value;
    }

    // Confirmation Dialog State
    protected bool IsConfirmDialogOpen { get; set; }
    protected string ConfirmDialogTitle { get; set; } = string.Empty;
    protected string ConfirmDialogMessage { get; set; } = string.Empty;
    protected DialogSeverity ConfirmDialogSeverity { get; set; } = DialogSeverity.Warning;
    protected string ConfirmButtonText { get; set; } = "Confirm";

    // Connection Dialog State
    protected bool IsConnectDialogOpen { get; set; }
    protected string? LastSuccessfulComputer { get; set; }
    protected ConnectToComputerDialog? ConnectDialogRef { get; set; }

    // Send Message Dialog State
    protected bool IsSendMessageDialogOpen { get; set; }
    protected bool IsSendMessageProcessing { get; set; }
    protected string? SendMessageErrorMessage { get; set; }
    protected string? InitialSendQueuePath { get; set; }
    protected SendMessageDialog? SendMessageDialogRef { get; set; }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        ConnectionManager.ConnectionRefreshed += OnConnectionRefreshed;

        await ConnectToUrlHostAsync();
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

    private void OnConnectionRefreshed(object? sender, ConnectionRefreshedEventArgs args)
    {
        if (_disposed) return;
        _ = InvokeAsync(() =>
        {
            if (_disposed || CurrentConnection?.Id != args.ConnectionId) return;
            if (SelectedQueue != null)
            {
                var current = CurrentConnection.Queues.FirstOrDefault(q => q.Id == SelectedQueue.Id);
                if (current != null)
                    SelectedQueue.ApplyMetrics(new QueueMetrics(current.MessageCountKnown ? current.MessageCount : null,
                        current.JournalMessageCountKnown ? current.JournalMessageCount : null));
            }
            StateHasChanged();
        });
    }

    /// <summary>
    /// Handles node selection from the tree view.
    /// Extracts queue info and view type to load appropriate messages.
    /// </summary>
    protected async Task HandleNodeSelectedAsync(TreeNodeData? nodeData)
    {
        _listRequest?.Cancel();
        _detailRequest?.Cancel();
        IsLoadingQueueProperties = false;
        QueuePropertiesError = null;
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
                Console.WriteLine(JsonSerializer.Serialize(queueInfo, new JsonSerializerOptions { WriteIndented = true }));

                // For journal messages, use JournalPath; for regular messages, use FormatName or Path
                var queuePath = nodeData.ViewType == QueueViewType.JournalMessages
                    ? queueInfo.JournalPath
                    : (!string.IsNullOrEmpty(queueInfo.FormatName) ? queueInfo.FormatName : queueInfo.Path);

                Console.WriteLine($"[DEBUG] QueueInfo FormatName: '{queueInfo.FormatName}', Path: '{queueInfo.Path}' ViewType: {nodeData.ViewType}");
                Console.WriteLine($"[DEBUG] Selected path for {nodeData.ViewType}: {queuePath}");
                await LoadMessagesAsync(queuePath, nodeData.ViewType);
            }
            else
            {
                Messages.Clear();
            }
        }
        else if (nodeData.ViewType == QueueViewType.Queue)
        {
            // For queue nodes (parent node), show queue properties
            SelectedQueue = queueInfo;
            CurrentViewType = QueueViewType.QueueProperties;
            Messages.Clear();
            SelectedMessage = null;
            IsDetailDrawerOpen = false;
            if (queueInfo != null && !queueInfo.IsSystemQueue && queueInfo.QueueType != QueueType.Outgoing)
            {
                IsLoadingQueueProperties = true;
                var properties = await MsmqService.GetQueueInfoAsync(queueInfo.Path, _lifetime.Token);
                if (ReferenceEquals(SelectedQueue, queueInfo) && CurrentViewType == QueueViewType.QueueProperties)
                {
                    IsLoadingQueueProperties = false;
                    if (properties.Success && properties.Data != null)
                    {
                        properties.Data.Id = queueInfo.Id;
                        properties.Data.ApplyMetrics(new QueueMetrics(
                            queueInfo.MessageCountKnown ? queueInfo.MessageCount : null,
                            queueInfo.JournalMessageCountKnown ? queueInfo.JournalMessageCount : null));
                        SelectedQueue = properties.Data;
                    }
                    else QueuePropertiesError = properties.ErrorMessage ?? "Queue properties unavailable.";
                }
            }
        }
        else if (nodeData.ViewType == QueueViewType.Folder)
        {
            // For folder nodes (e.g., "Private Queues"), show queue list summary
            SelectedQueue = null;
            CurrentViewType = QueueViewType.Folder;
            SelectedFolderNode = nodeData;
            Messages.Clear();
            SelectedMessage = null;
            IsDetailDrawerOpen = false;
            
            // Extract filtered queues from the folder node's children
            FilteredQueues = ExtractQueuesFromNode(nodeData);
        }
        else
        {
            // For other nodes, just update state without loading messages
            SelectedQueue = queueInfo;
            CurrentViewType = nodeData.ViewType;
            Messages.Clear();
            SelectedMessage = null;
            IsDetailDrawerOpen = false;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Extracts all QueueInfo objects from a tree node and its children.
    /// </summary>
    private List<QueueInfo> ExtractQueuesFromNode(TreeNodeData node)
    {
        var queues = new List<QueueInfo>();
        
        // Check if this node has queue info
        if (node.Data is QueueInfo queueInfo)
        {
            queues.Add(queueInfo);
        }
        
        // Recursively extract from children
        foreach (var child in node.Children)
        {
            queues.AddRange(ExtractQueuesFromNode(child));
        }
        
        // Remove duplicates based on queue path (same queue appears in parent and child nodes)
        return queues.DistinctBy(q => q.Path).ToList();
    }

    /// <summary>
    /// Handles queue selection from the queue list summary.
    /// </summary>
    protected async Task HandleSummaryQueueSelectedAsync(QueueInfo queue)
    {
        _detailRequest?.Cancel();
        SelectedQueue = queue;
        CurrentViewType = QueueViewType.QueueMessages;
        SelectedMessage = null;
        IsDetailDrawerOpen = false;
        
        // Use FormatName if available, otherwise fall back to Path
        var queuePath = !string.IsNullOrEmpty(queue.FormatName) ? queue.FormatName : queue.Path;
        await LoadMessagesAsync(queuePath, QueueViewType.QueueMessages);
        
        StateHasChanged();
    }

    /// <summary>
    /// Handles filter changes from the queue tree view.
    /// Updates the filtered queues list if a folder is currently selected.
    /// </summary>
    protected Task HandleFilterChangedAsync(TreeNodeData? filteredRoot)
    {
        // If a folder is currently selected, update the filtered queues
        if (CurrentViewType == QueueViewType.Folder && SelectedFolderNode != null && filteredRoot != null)
        {
            // Find the corresponding folder node in the filtered tree
            var folderNodeInFilteredTree = FindNodeById(filteredRoot, SelectedFolderNode.Id);
            if (folderNodeInFilteredTree != null)
            {
                FilteredQueues = ExtractQueuesFromNode(folderNodeInFilteredTree);
                StateHasChanged();
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Finds a node by ID in the tree recursively.
    /// </summary>
    private TreeNodeData? FindNodeById(TreeNodeData node, string id)
    {
        if (node.Id == id)
        {
            return node;
        }
        
        foreach (var child in node.Children)
        {
            var found = FindNodeById(child, id);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
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
        if (CurrentConnection == null || CurrentConnection.IsRefreshing) return;
        var connection = CurrentConnection;

        try
        {
            connection.IsRefreshing = true;
            StateHasChanged();

            var result = await ConnectionManager.RefreshConnectionAsync(connection.Id, true, _lifetime.Token);
            if (!result.Success) ShowOperationFeedback(result.ErrorMessage ?? "Queue refresh failed.", false);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            connection.Status = ConnectionStatus.Failed;
        }
        finally
        {
            connection.IsRefreshing = false;
            if (!_disposed) StateHasChanged();
        }
    }

    /// <summary>
    /// Handles purge request from the queue tree view by opening the confirmation dialog.
    /// </summary>
    /// <param name="purgeRequest">Tuple containing the queue and view type to purge.</param>
    protected Task HandlePurgeRequestedAsync((QueueInfo Queue, QueueViewType ViewType) purgeRequest)
    {
        var (queue, viewType) = purgeRequest;
        
        // Set up the purge dialog
        SelectedQueue = queue;
        CurrentViewType = viewType;
        PurgeErrorMessage = null;
        IsPurgeProcessing = false;
        IsPurgeDialogOpen = true;
        
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles confirmation from the purge dialog.
    /// </summary>
    /// <param name="result">The purge confirmation result.</param>
    protected async Task HandlePurgeConfirmAsync(PurgeConfirmationResult result)
    {
        try
        {
            IsPurgeProcessing = true;
            PurgeErrorMessage = null;
            ClearOperationFeedback();
            StateHasChanged();

            // Determine the correct queue path for purging
            string queuePathToPurge;
            if (result.ViewType == QueueViewType.JournalMessages)
            {
                queuePathToPurge = result.Queue.JournalPath;
            }
            else
            {
                queuePathToPurge = !string.IsNullOrEmpty(result.Queue.FormatName)
                    ? result.Queue.FormatName
                    : result.Queue.Path;
            }

            // Perform the purge
            var purgeResult = await MsmqService.PurgeQueueAsync(queuePathToPurge);

            if (purgeResult.Success)
            {
                // Success - close dialog and refresh
                IsPurgeDialogOpen = false;

                var messageScope = result.ViewType == QueueViewType.JournalMessages
                    ? "journal"
                    : "queue";

                ShowOperationFeedback(
                    $"The {messageScope} for {result.Queue.Name} was purged.",
                    isSuccess: true);
                
                // Refresh the queue data and current view
                await RefreshQueuesAsync();
                await RefreshMessagesAsync();

                StateHasChanged();
            }
            else
            {
                // Show error in dialog
                PurgeErrorMessage = purgeResult.ErrorMessage ?? "Unknown error occurred during purge operation.";
                ShowOperationFeedback(PurgeErrorMessage, isSuccess: false);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            PurgeErrorMessage = $"Error occurred while purging: {ex.Message}";
            ShowOperationFeedback(PurgeErrorMessage, isSuccess: false);
            StateHasChanged();
        }
        finally
        {
            IsPurgeProcessing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles cancellation from the purge dialog.
    /// </summary>
    protected Task HandlePurgeCancelAsync()
    {
        IsPurgeDialogOpen = false;
        PurgeErrorMessage = null;
        IsPurgeProcessing = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dismisses the operation feedback alert.
    /// </summary>
    protected Task DismissOperationFeedbackAsync()
    {
        ClearOperationFeedback();
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows operation feedback in the page header area.
    /// </summary>
    private void ShowOperationFeedback(string message, bool isSuccess)
    {
        OperationFeedbackMessage = message;
        OperationFeedbackClass = isSuccess ? "alert-success" : "alert-danger";
    }

    /// <summary>
    /// Clears the operation feedback message.
    /// </summary>
    private void ClearOperationFeedback()
    {
        OperationFeedbackMessage = null;
        OperationFeedbackClass = "alert-success";
    }

    #endregion

    #region Send Message Dialog

    /// <summary>
    /// Opens the send message dialog with optional initial queue selection.
    /// </summary>
    /// <param name="queuePath">Optional initial queue path to pre-select</param>
    protected Task OpenSendMessageDialogAsync(string? queuePath = null)
    {
        if (CurrentConnection == null)
        {
            return Task.CompletedTask;
        }

        InitialSendQueuePath = queuePath ?? SelectedQueue?.Path;
        SendMessageErrorMessage = null;
        IsSendMessageProcessing = false;
        IsSendMessageDialogOpen = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles send message confirmation from the dialog.
    /// </summary>
    /// <param name="request">The send message request</param>
    protected async Task HandleSendMessageAsync(SendMessageRequest request)
    {
        if (CurrentConnection == null || IsSendMessageProcessing)
        {
            return;
        }

        try
        {
            IsSendMessageProcessing = true;
            SendMessageErrorMessage = null;
            StateHasChanged();

            // Create QueueMessage from the request
            var messageBody = new MessageBody(request.MessageContent)
            {
                Format = request.Format,
                Encoding = request.TextEncoding
            };

            var queueMessage = new QueueMessage
            {
                Label = request.Label,
                Body = messageBody,
                Priority = request.Priority,
                Recoverable = request.Recoverable,
                IsTransactional = request.IsTransactional,
                TimeToReachQueue = request.TimeToReachQueue,
                TimeToBeReceived = request.TimeToBeReceived,
                CorrelationId = request.CorrelationId
            };

            // Send the message
            var result = await MsmqService.SendMessageAsync(
                request.QueuePath,
                queueMessage,
                CancellationToken.None);

            if (result.Success)
            {
                // Success - show success message and optionally close dialog
                var queueName = SelectedQueue?.Name ?? "queue";
                var successMessage = $"Message sent successfully to {queueName} at {DateTime.Now:HH:mm:ss} using {request.TextEncoding} encoding";
                
                SendMessageDialogRef?.ShowSuccess(successMessage);
                
                // Close dialog only if requested
                if (request.CloseAfterSend)
                {
                    IsSendMessageDialogOpen = false;
                }
                
                // Clear any previous error
                SendMessageErrorMessage = null;
                
                // If we sent to the currently selected queue, refresh the message list
                if (SelectedQueue != null &&
                    (string.Equals(request.QueuePath, SelectedQueue.Path, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(request.QueuePath, SelectedQueue.JournalPath, StringComparison.OrdinalIgnoreCase)))
                {
                    await RefreshMessagesAsync();
                }

                StateHasChanged();
            }
            else
            {
                // Show error in dialog
                SendMessageErrorMessage = result.ErrorMessage ?? "Unknown error occurred while sending message.";
                // Clear any previous success message
                SendMessageDialogRef?.ClearSuccess();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            SendMessageErrorMessage = $"Error occurred while sending message: {ex.Message}";
            StateHasChanged();
        }
        finally
        {
            IsSendMessageProcessing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles cancellation from the send message dialog.
    /// </summary>
    protected Task HandleSendMessageCancelAsync()
    {
        IsSendMessageDialogOpen = false;
        SendMessageErrorMessage = null;
        IsSendMessageProcessing = false;
        InitialSendQueuePath = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    #endregion

    #region Message Operations

    /// <summary>
    /// Loads messages for the specified queue based on view type.
    /// </summary>
    private async Task LoadMessagesAsync(string queuePath, QueueViewType viewType)
    {
        _listRequest?.Cancel();
        using var request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _listRequest = request;
        var requestToken = request.Token;
        IsLoadingMessages = true;
        Messages.Clear();
        StateHasChanged();

        try
        {
            // Debug logging to track what we're trying to load
            Console.WriteLine($"[DEBUG] LoadMessagesAsync called - ViewType: {viewType}, QueuePath: {queuePath}");

            if (viewType == QueueViewType.QueueMessages && SelectedQueue?.QueueType == QueueType.Outgoing)
            {
                Console.WriteLine($"[DEBUG] Skipping message retrieval for outgoing queue: {queuePath}");
                Messages.Clear();
                SelectedMessage = null;
                IsDetailDrawerOpen = false;
                ShowOperationFeedback(OutgoingQueueReadNotSupportedMessage, isSuccess: false);
                return;
            }

            OperationResult<IEnumerable<QueueMessage>> result;

            if (viewType == QueueViewType.JournalMessages)
            {
                Console.WriteLine($"[DEBUG] Loading JOURNAL messages from: {queuePath}");
                // Load journal messages
                result = await MsmqService.GetJournalMessagesAsync(
                    queuePath,
                    maxMessages: 0, cancellationToken: requestToken);
            }
            else
            {
                Console.WriteLine($"[DEBUG] Loading REGULAR messages from: {queuePath}");
                // Load regular queue messages
                result = await MsmqService.GetMessagesAsync(
                    queuePath,
                    peekOnly: true,
                    maxMessages: 0, cancellationToken: requestToken);
            }

            requestToken.ThrowIfCancellationRequested();
            if (result.Success && result.Data != null)
            {
                Messages = result.Data.ToList();
                Console.WriteLine($"[DEBUG] Successfully loaded {Messages.Count} messages");
            }
            else
            {
                // Log the error for debugging
                Console.WriteLine($"Failed to load messages. ViewType: {viewType}, QueuePath: {queuePath}, Error: {result.ErrorMessage}");
                ShowOperationFeedback(result.ErrorMessage ?? "Unable to load messages from this queue.", isSuccess: false);
                Messages.Clear();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception loading messages. ViewType: {viewType}, QueuePath: {queuePath}, Exception: {ex.Message}");
            ShowOperationFeedback($"Unable to load messages: {ex.Message}", isSuccess: false);
            Messages.Clear();
        }
        finally
        {
            if (ReferenceEquals(_listRequest, request))
            {
                _listRequest = null;
                IsLoadingMessages = false;
                if (!_disposed) StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Handles message selection from the list.
    /// </summary>
    protected async Task HandleMessageSelectedAsync(QueueMessage? message)
    {
        _detailRequest?.Cancel();
        using var request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _detailRequest = request;
        SelectedMessage = null;
        IsDetailDrawerOpen = false;
        try
        {
            if (message == null) return;
            var result = message.LookupId != 0
                ? await MsmqService.GetMessageByLookupIdAsync(message.QueuePath, message.LookupId, cancellationToken: request.Token)
                : await MsmqService.GetMessageByIdAsync(message.QueuePath, message.Id, cancellationToken: request.Token);
            request.Token.ThrowIfCancellationRequested();
            if (result.Success && result.Data != null)
            {
                SelectedMessage = result.Data;
                IsDetailDrawerOpen = true;
            }
            else ShowOperationFeedback(result.ErrorMessage ?? "The message is no longer available.", false);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_detailRequest, request)) _detailRequest = null;
            if (!_disposed) StateHasChanged();
        }
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
    /// Handles when messages are deleted from the MessageList component.
    /// </summary>
    protected async Task HandleMessagesDeletedAsync(List<string> deletedMessageIds)
    {
        // Remove deleted messages from our local Messages list
        Messages.RemoveAll(m => deletedMessageIds.Contains(m.Id));
        
        // Clear selected messages since they've been deleted
        SelectedMessages.Clear();
        
        // Close detail drawer if the selected message was deleted
        if (SelectedMessage != null && deletedMessageIds.Contains(SelectedMessage.Id))
        {
            await CloseDetailDrawerAsync();
        }
        
        StateHasChanged();
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
        _detailRequest?.Cancel();
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
        // Check if we're trying to delete from a journal queue
        if (CurrentViewType == QueueViewType.JournalMessages)
        {
            // Journal messages are read-only and cannot be deleted
            ConfirmDialogTitle = "Cannot Delete Journal Message";
            ConfirmDialogMessage = "Journal messages are read-only archives and cannot be deleted.\n\nTo remove journal messages, you must purge the entire journal.";
            ConfirmDialogSeverity = DialogSeverity.Warning;
            ConfirmButtonText = "OK";
            IsConfirmDialogOpen = true;
            
            // Don't set pending operation since we won't actually delete
            _pendingOperation = PendingOperation.None;
            _pendingOperationMessage = null;
            return;
        }

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
            // Always use the main queue path for deletion (never the journal path)
            // Journal messages cannot be deleted individually
            var queuePath = !string.IsNullOrEmpty(SelectedQueue.FormatName)
                ? SelectedQueue.FormatName
                : SelectedQueue.Path;

            var result = await MessageOperationsService.DeleteMessageAsync(
                queuePath,
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

    #region Connection Management

    /// <summary>
    /// Opens the Connect to Computer dialog.
    /// </summary>
    protected void OpenConnectDialog()
    {
        IsConnectDialogOpen = true;
    }

    /// <summary>
    /// Connects to the host from the current URL.
    /// </summary>
    protected async Task ConnectToUrlHostAsync()
    {
        var computerName = GetComputerNameFromCurrentUrl();
        var result = await ConnectionManager.ConnectAsync(computerName, displayName: computerName, cancellationToken: _lifetime.Token);

        if (result.Success && result.Data != null)
        {
            CurrentConnection = result.Data;
            LastSuccessfulComputer = computerName;
            StateHasChanged();
            return;
        }

        ShowOperationFeedback(
            $"Failed to connect to URL host '{computerName}'. Use Connect to choose another server.",
            isSuccess: false);
        StateHasChanged();
    }

    /// <summary>
    /// Gets the target computer name from the current navigation URL host.
    /// </summary>
    private string GetComputerNameFromCurrentUrl()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var host = uri.Host;

        return string.IsNullOrWhiteSpace(host)
            ? "."
            : host;
    }

    /// <summary>
    /// Connects to localhost.
    /// </summary>
    protected async Task ConnectToLocalhostAsync()
    {
        var result = await ConnectionManager.ConnectAsync("localhost", displayName: "localhost", cancellationToken: _lifetime.Token);
        if (result.Success && result.Data != null)
        {
            CurrentConnection = result.Data;
            LastSuccessfulComputer = "localhost";
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles connection request from the dialog.
    /// </summary>
    protected async Task HandleConnectionRequestAsync(QueueConnection tempConnection)
    {
        if (ConnectDialogRef == null) return;

        var computerName = tempConnection.ComputerName;

        try
        {
            // Attempt connection via ConnectionManager
            var result = await ConnectionManager.ConnectAsync(computerName, cancellationToken: _lifetime.Token);

            if (result.Success && result.Data != null)
            {
                // Connection successful
                CurrentConnection = result.Data;
                LastSuccessfulComputer = computerName;

                // Clear any selected queue/messages
                SelectedQueue = null;
                SelectedMessage = null;
                Messages.Clear();
                IsDetailDrawerOpen = false;

                // Notify dialog of success
                await ConnectDialogRef.HandleConnectionSuccessAsync(computerName);
                StateHasChanged();
            }
            else
            {
                // Connection failed - show error in dialog
                ConnectDialogRef.HandleConnectionFailure(result.ErrorMessage ?? "Connection failed");
            }
        }
        catch (Exception ex)
        {
            ConnectDialogRef.HandleConnectionFailure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles connection dialog cancellation.
    /// </summary>
    protected Task HandleConnectionCancelledAsync()
    {
        IsConnectDialogOpen = false;
        return Task.CompletedTask;
    }

    #endregion

    #region Disposal

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        ConnectionManager.ConnectionRefreshed -= OnConnectionRefreshed;
        _lifetime.Cancel();
        _lifetime.Dispose();

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
