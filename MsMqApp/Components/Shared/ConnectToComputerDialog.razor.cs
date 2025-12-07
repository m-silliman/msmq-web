using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Dialog component for connecting to remote MSMQ computers.
/// Allows user to enter a computer name and establish a connection.
/// </summary>
public class ConnectToComputerDialogBase : ComponentBase
{
    private bool _isOpen;
    private string _computerName = string.Empty;
    private string? _lastSuccessfulComputer;

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
                    ResetDialogState();
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
    /// Gets or sets a value indicating whether to show the close button (X) in the header.
    /// </summary>
    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether clicking the backdrop closes the dialog.
    /// </summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = false;

    /// <summary>
    /// Gets or sets the callback invoked when connection is successful.
    /// </summary>
    [Parameter]
    public EventCallback<QueueConnection> OnConnected { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the dialog is cancelled.
    /// </summary>
    [Parameter]
    public EventCallback OnCancelled { get; set; }

    /// <summary>
    /// Gets or sets the last successful computer name (for convenience).
    /// </summary>
    [Parameter]
    public string? LastSuccessfulComputer
    {
        get => _lastSuccessfulComputer;
        set => _lastSuccessfulComputer = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether a connection attempt is in progress.
    /// </summary>
    protected bool IsConnecting { get; set; }

    /// <summary>
    /// Gets or sets the computer name entered by the user.
    /// </summary>
    protected string ComputerName
    {
        get => _computerName;
        set
        {
            if (_computerName != value)
            {
                _computerName = value;
                ValidateInput();
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    protected string? ValidationError { get; set; }

    /// <summary>
    /// Gets or sets the connection error message.
    /// </summary>
    protected string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the input is valid.
    /// </summary>
    protected bool IsValid => string.IsNullOrWhiteSpace(ValidationError) &&
                              !string.IsNullOrWhiteSpace(ComputerName);

    /// <summary>
    /// Gets a unique ID for the dialog title.
    /// </summary>
    protected string DialogTitleId { get; } = $"connect-dialog-title-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets a unique ID for the dialog description.
    /// </summary>
    protected string DialogDescriptionId { get; } = $"connect-dialog-desc-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets a unique ID for the computer name input.
    /// </summary>
    protected string ComputerNameInputId { get; } = $"computer-name-input-{Guid.NewGuid():N}";

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
    /// Gets the CSS class for the input based on validation state.
    /// </summary>
    protected string GetInputClass()
    {
        if (string.IsNullOrWhiteSpace(ComputerName))
            return string.Empty;

        return string.IsNullOrWhiteSpace(ValidationError) ? "is-valid" : "is-invalid";
    }

    /// <summary>
    /// Handles the connect button click.
    /// </summary>
    protected async Task OnConnectAsync()
    {
        if (IsConnecting || !IsValid)
            return;

        ValidateInput();
        if (!IsValid)
        {
            StateHasChanged();
            return;
        }

        IsConnecting = true;
        ErrorMessage = null;
        StateHasChanged();

        // Callback will be invoked by parent component
        // Parent will call the connection manager and handle success/failure
        if (OnConnected.HasDelegate)
        {
            // Create a temporary connection object with the entered computer name
            var tempConnection = new QueueConnection
            {
                ComputerName = ComputerName.Trim(),
                DisplayName = ComputerName.Trim()
            };

            await OnConnected.InvokeAsync(tempConnection);
        }

        // Note: IsConnecting will be reset by parent via SetConnecting() or by closing dialog
    }

    /// <summary>
    /// Handles the cancel button click.
    /// </summary>
    protected async Task OnCancelAsync()
    {
        if (IsConnecting)
            return;

        if (OnCancelled.HasDelegate)
        {
            await OnCancelled.InvokeAsync();
        }

        await CloseDialogAsync();
    }

    /// <summary>
    /// Handles the backdrop click.
    /// </summary>
    protected async Task OnBackdropClickAsync()
    {
        if (CloseOnBackdropClick && !IsConnecting)
        {
            await OnCancelAsync();
        }
    }

    /// <summary>
    /// Uses the last successful computer name.
    /// </summary>
    protected void UseLastComputer()
    {
        if (!string.IsNullOrWhiteSpace(LastSuccessfulComputer))
        {
            ComputerName = LastSuccessfulComputer;
            ValidateInput();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Public method for parent component to indicate connection success.
    /// </summary>
    public async Task HandleConnectionSuccessAsync(string computerName)
    {
        LastSuccessfulComputer = computerName;
        await CloseDialogAsync();
    }

    /// <summary>
    /// Public method for parent component to indicate connection failure.
    /// </summary>
    public void HandleConnectionFailure(string errorMessage)
    {
        ErrorMessage = errorMessage;
        IsConnecting = false;
        StateHasChanged();
    }

    /// <summary>
    /// Public method for parent component to update connecting state.
    /// </summary>
    public void SetConnecting(bool isConnecting)
    {
        IsConnecting = isConnecting;
        StateHasChanged();
    }

    /// <summary>
    /// Validates the computer name input.
    /// </summary>
    private void ValidateInput()
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(ComputerName))
        {
            ValidationError = "Computer name is required";
            return;
        }

        var trimmed = ComputerName.Trim();

        // Basic validation - check for invalid characters
        if (trimmed.Contains(' '))
        {
            ValidationError = "Computer name cannot contain spaces";
            return;
        }

        // Check for common invalid characters (not exhaustive, just basic checks)
        char[] invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        if (trimmed.Any(c => invalidChars.Contains(c)))
        {
            ValidationError = "Computer name contains invalid characters";
            return;
        }
    }

    /// <summary>
    /// Resets the dialog state.
    /// </summary>
    private void ResetDialogState()
    {
        IsConnecting = false;
        ValidationError = null;
        ErrorMessage = null;
        // Don't reset ComputerName - keep it for user convenience
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    private async Task CloseDialogAsync()
    {
        IsOpen = false;
        IsConnecting = false;

        if (IsOpenChanged.HasDelegate)
        {
            await IsOpenChanged.InvokeAsync(IsOpen);
        }

        StateHasChanged();
    }
}
