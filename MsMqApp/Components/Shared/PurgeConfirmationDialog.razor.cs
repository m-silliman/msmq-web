using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Specialized confirmation dialog for purging queue messages.
/// Requires user to type the exact queue name for confirmation.
/// </summary>
public class PurgeConfirmationDialogBase : ComponentBase
{
    private bool _isOpen;
    private bool _confirmClicked;
    private string _enteredQueueName = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is open.
    /// </summary>
    [Parameter]
#pragma warning disable BL0007 // Component parameter has a non-auto property setter
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen != value)
            {
                _isOpen = value;
                if (_isOpen)
                {
                    // Reset state when opening
                    _confirmClicked = false;
                    _enteredQueueName = string.Empty;
                    ErrorMessage = null;
                    IsProcessing = false;
                }
            }
        }
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets the callback invoked when IsOpen changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Gets or sets the queue to be purged.
    /// </summary>
    [Parameter]
    public QueueInfo? Queue { get; set; }

    /// <summary>
    /// Gets or sets the view type being purged (QueueMessages or JournalMessages).
    /// </summary>
    [Parameter]
    public QueueViewType ViewType { get; set; } = QueueViewType.QueueMessages;

    /// <summary>
    /// Gets or sets the callback invoked when the user confirms the purge.
    /// </summary>
    [Parameter]
    public EventCallback<PurgeConfirmationResult> OnConfirm { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user cancels the purge.
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a purge operation is in progress.
    /// </summary>
    [Parameter]
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the confirm button was clicked.
    /// </summary>
    protected bool ConfirmClicked => _confirmClicked;

    /// <summary>
    /// Gets or sets the queue name entered by the user.
    /// </summary>
    protected string EnteredQueueName
    {
        get => _enteredQueueName;
        set
        {
            if (_enteredQueueName != value)
            {
                _enteredQueueName = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets the display name of the queue.
    /// </summary>
    protected string QueueDisplayName =>
        !string.IsNullOrEmpty(Queue?.Name) ? Queue.Name : Queue?.Path ?? string.Empty;

    /// <summary>
    /// Gets the message count to be purged.
    /// </summary>
    protected int MessageCount =>
        ViewType == QueueViewType.JournalMessages ? (Queue?.JournalMessageCount ?? 0) : (Queue?.MessageCount ?? 0);

    /// <summary>
    /// Gets the text describing the type of messages being purged.
    /// </summary>
    protected string ViewTypeText =>
        ViewType == QueueViewType.JournalMessages ? "journal messages" : "messages";

    /// <summary>
    /// Gets a value indicating whether the entered queue name is valid.
    /// </summary>
    protected bool IsQueueNameValid =>
        !string.IsNullOrEmpty(EnteredQueueName) &&
        string.Equals(EnteredQueueName, QueueDisplayName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a unique ID for the dialog title.
    /// </summary>
    protected string DialogTitleId { get; } = $"purge-dialog-title-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets a unique ID for the dialog message.
    /// </summary>
    protected string DialogMessageId { get; } = $"purge-dialog-message-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets a unique ID for the queue name input.
    /// </summary>
    protected string QueueNameInputId { get; } = $"queue-name-input-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets the CSS class for the backdrop based on state.
    /// </summary>
    protected string GetBackdropClass()
    {
        return IsOpen ? "show" : string.Empty;
    }

    /// <summary>
    /// Gets the CSS class for the dialog based on state.
    /// </summary>
    protected string GetDialogClass()
    {
        return IsOpen ? "show" : string.Empty;
    }

    /// <summary>
    /// Gets the CSS class for the input validation state.
    /// </summary>
    protected string GetInputValidationClass()
    {
        if (string.IsNullOrEmpty(EnteredQueueName))
            return string.Empty;

        return IsQueueNameValid ? "is-valid" : "is-invalid";
    }

    /// <summary>
    /// Handles the confirm button click.
    /// </summary>
    protected async Task OnConfirmAsync()
    {
        if (IsProcessing || !IsQueueNameValid || Queue == null)
            return;

        _confirmClicked = true;
        StateHasChanged();

        var result = new PurgeConfirmationResult
        {
            Queue = Queue,
            ViewType = ViewType,
            ConfirmedQueueName = EnteredQueueName,
            Timestamp = DateTime.UtcNow
        };

        if (OnConfirm.HasDelegate)
        {
            await OnConfirm.InvokeAsync(result);
        }

        // Note: Don't auto-close here - let the parent handle success/failure
    }

    /// <summary>
    /// Handles the cancel button click.
    /// </summary>
    protected async Task OnCancelAsync()
    {
        if (IsProcessing)
            return;

        _confirmClicked = false;

        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }

        await CloseDialogAsync();
    }

    /// <summary>
    /// Handles the backdrop click.
    /// </summary>
    protected async Task OnBackdropClickAsync()
    {
        if (!IsProcessing)
        {
            await OnCancelAsync();
        }
    }

    /// <summary>
    /// Public method to close the dialog (for parent component).
    /// </summary>
    public async Task CloseAsync()
    {
        await CloseDialogAsync();
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    private async Task CloseDialogAsync()
    {
        IsOpen = false;

        if (IsOpenChanged.HasDelegate)
        {
            await IsOpenChanged.InvokeAsync(IsOpen);
        }

        StateHasChanged();
    }
}

/// <summary>
/// Represents the result of a purge confirmation dialog.
/// </summary>
public class PurgeConfirmationResult
{
    /// <summary>
    /// Gets or sets the queue to be purged.
    /// </summary>
    public QueueInfo Queue { get; set; } = null!;

    /// <summary>
    /// Gets or sets the view type being purged.
    /// </summary>
    public QueueViewType ViewType { get; set; }

    /// <summary>
    /// Gets or sets the queue name that was confirmed by the user.
    /// </summary>
    public string ConfirmedQueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the confirmation was made.
    /// </summary>
    public DateTime Timestamp { get; set; }
}