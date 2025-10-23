using Microsoft.AspNetCore.Components;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Reusable confirmation dialog component for destructive operations.
/// Supports different severity levels and provides confirm/cancel callbacks.
/// </summary>
public class ConfirmationDialogBase : ComponentBase
{
    private bool _isOpen;
    private bool _confirmClicked;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is open.
    /// </summary>
    /// <remarks>
    /// This property has a custom setter to reset dialog state when opening.
    /// The BL0007 warning is suppressed because this behavior is intentional.
    /// </remarks>
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
    /// Gets or sets the dialog title.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Confirm Action";

    /// <summary>
    /// Gets or sets the dialog message.
    /// </summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the detail message (shown in expandable details section).
    /// </summary>
    [Parameter]
    public string? DetailMessage { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the dialog.
    /// </summary>
    [Parameter]
    public DialogSeverity Severity { get; set; } = DialogSeverity.Warning;

    /// <summary>
    /// Gets or sets the confirm button text.
    /// </summary>
    [Parameter]
    public string ConfirmButtonText { get; set; } = "Confirm";

    /// <summary>
    /// Gets or sets the cancel button text.
    /// </summary>
    [Parameter]
    public string CancelButtonText { get; set; } = "Cancel";

    /// <summary>
    /// Gets or sets a value indicating whether to show the close button (X) in the header.
    /// </summary>
    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to auto-focus the confirm button.
    /// </summary>
    [Parameter]
    public bool AutoFocusConfirm { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether clicking the backdrop closes the dialog.
    /// </summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = false;

    /// <summary>
    /// Gets or sets custom child content to display in the dialog body.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the confirm button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<ConfirmationResult> OnConfirm { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the cancel button is clicked or dialog is dismissed.
    /// </summary>
    [Parameter]
    public EventCallback<ConfirmationResult> OnCancel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress.
    /// </summary>
    [Parameter]
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Gets a value indicating whether the confirm button was clicked.
    /// </summary>
    protected bool ConfirmClicked => _confirmClicked;

    /// <summary>
    /// Gets a unique ID for the dialog title.
    /// </summary>
    protected string DialogTitleId { get; } = $"dialog-title-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets a unique ID for the dialog message.
    /// </summary>
    protected string DialogMessageId { get; } = $"dialog-message-{Guid.NewGuid():N}";

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
    /// Gets the CSS class for the dialog header based on severity.
    /// </summary>
    protected string GetHeaderClass()
    {
        return Severity switch
        {
            DialogSeverity.Info => "header-info",
            DialogSeverity.Warning => "header-warning",
            DialogSeverity.Danger => "header-danger",
            DialogSeverity.Success => "header-success",
            _ => "header-info"
        };
    }

    /// <summary>
    /// Gets the icon class based on severity.
    /// </summary>
    protected string GetIconClass()
    {
        return Severity switch
        {
            DialogSeverity.Info => "bi bi-info-circle-fill",
            DialogSeverity.Warning => "bi bi-exclamation-triangle-fill",
            DialogSeverity.Danger => "bi bi-exclamation-octagon-fill",
            DialogSeverity.Success => "bi bi-check-circle-fill",
            _ => "bi bi-question-circle-fill"
        };
    }

    /// <summary>
    /// Gets the CSS class for the confirm button based on severity.
    /// </summary>
    protected string GetConfirmButtonClass()
    {
        return Severity switch
        {
            DialogSeverity.Info => "btn-primary",
            DialogSeverity.Warning => "btn-warning",
            DialogSeverity.Danger => "btn-danger",
            DialogSeverity.Success => "btn-success",
            _ => "btn-primary"
        };
    }

    /// <summary>
    /// Gets the CSS class for the cancel button.
    /// </summary>
    protected string GetCancelButtonClass()
    {
        return "btn-secondary";
    }

    /// <summary>
    /// Gets the icon class for the confirm button.
    /// </summary>
    protected string GetConfirmIconClass()
    {
        return Severity == DialogSeverity.Danger
            ? "bi bi-trash"
            : "bi bi-check-lg";
    }

    /// <summary>
    /// Handles the confirm button click.
    /// </summary>
    protected async Task OnConfirmAsync()
    {
        if (IsProcessing) return;

        _confirmClicked = true;
        StateHasChanged();

        var result = new ConfirmationResult
        {
            Confirmed = true,
            Timestamp = DateTime.UtcNow
        };

        if (OnConfirm.HasDelegate)
        {
            await OnConfirm.InvokeAsync(result);
        }

        // Only close automatically if not processing
        if (!IsProcessing)
        {
            await CloseDialogAsync();
        }
    }

    /// <summary>
    /// Handles the cancel button click.
    /// </summary>
    protected async Task OnCancelAsync()
    {
        if (IsProcessing) return;

        _confirmClicked = false;

        var result = new ConfirmationResult
        {
            Confirmed = false,
            Timestamp = DateTime.UtcNow
        };

        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync(result);
        }

        await CloseDialogAsync();
    }

    /// <summary>
    /// Handles the backdrop click.
    /// </summary>
    protected async Task OnBackdropClickAsync()
    {
        if (CloseOnBackdropClick && !IsProcessing)
        {
            await OnCancelAsync();
        }
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
/// Defines the severity levels for confirmation dialogs.
/// </summary>
public enum DialogSeverity
{
    /// <summary>
    /// Informational dialog (blue).
    /// </summary>
    Info,

    /// <summary>
    /// Warning dialog (yellow/orange) - use for operations that should be reconsidered.
    /// </summary>
    Warning,

    /// <summary>
    /// Danger dialog (red) - use for destructive operations.
    /// </summary>
    Danger,

    /// <summary>
    /// Success dialog (green) - use for confirmations of positive actions.
    /// </summary>
    Success
}

/// <summary>
/// Represents the result of a confirmation dialog.
/// </summary>
public class ConfirmationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the user confirmed the action.
    /// </summary>
    public bool Confirmed { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user made the decision.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets optional data associated with the confirmation.
    /// </summary>
    public object? Data { get; set; }
}
