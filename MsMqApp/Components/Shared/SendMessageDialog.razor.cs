using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Dialog component for composing and sending MSMQ messages.
/// Allows selection of target queue, message content, formatter, and properties.
/// </summary>
public class SendMessageDialogBase : ComponentBase
{
    private bool _isOpen;
    private string _messageContent = string.Empty;
    private string _messageLabel = string.Empty;
    private MessageBodyFormat _selectedFormat = MessageBodyFormat.Text;
    private MessagePriority _selectedPriority = MessagePriority.Normal;
    private bool _recoverable = true;
    private bool _isTransactional = false;
    private int _timeToReachQueueMinutes = 0; // 0 = infinite
    private int _timeToBeReceivedMinutes = 0; // 0 = infinite
    private string _correlationId = string.Empty;
    private string? _selectedQueuePath;
    private string? _validationError;
    private bool _isAdvancedOptionsExpanded = false;
    private string _selectedTextEncoding = "UTF-8";

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
    /// Gets or sets the current queue connection (for queue selection).
    /// </summary>
    [Parameter]
    public QueueConnection? Connection { get; set; }

    /// <summary>
    /// Gets or sets the initially selected queue path.
    /// </summary>
    [Parameter]
    public string? InitialQueuePath { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user confirms sending the message.
    /// </summary>
    [Parameter]
    public EventCallback<SendMessageRequest> OnConfirm { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user cancels.
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a send operation is in progress.
    /// </summary>
    [Parameter]
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    protected string MessageContent
    {
        get => _messageContent;
        set
        {
            if (_messageContent != value)
            {
                _messageContent = value;
                ValidateInput();
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the message label.
    /// </summary>
    protected string MessageLabel
    {
        get => _messageLabel;
        set
        {
            if (_messageLabel != value)
            {
                _messageLabel = value;
                ValidateInput();
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected message format.
    /// </summary>
    protected MessageBodyFormat SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (_selectedFormat != value)
            {
                _selectedFormat = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected message priority.
    /// </summary>
    protected MessagePriority SelectedPriority
    {
        get => _selectedPriority;
        set
        {
            if (_selectedPriority != value)
            {
                _selectedPriority = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the message is recoverable.
    /// </summary>
    protected bool Recoverable
    {
        get => _recoverable;
        set
        {
            if (_recoverable != value)
            {
                _recoverable = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the message is transactional.
    /// </summary>
    protected bool IsTransactional
    {
        get => _isTransactional;
        set
        {
            if (_isTransactional != value)
            {
                _isTransactional = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the time to reach queue in minutes (0 = infinite).
    /// </summary>
    protected int TimeToReachQueueMinutes
    {
        get => _timeToReachQueueMinutes;
        set
        {
            if (_timeToReachQueueMinutes != value)
            {
                _timeToReachQueueMinutes = Math.Max(0, value);
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the time to be received in minutes (0 = infinite).
    /// </summary>
    protected int TimeToBeReceivedMinutes
    {
        get => _timeToBeReceivedMinutes;
        set
        {
            if (_timeToBeReceivedMinutes != value)
            {
                _timeToBeReceivedMinutes = Math.Max(0, value);
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    protected string CorrelationId
    {
        get => _correlationId;
        set
        {
            if (_correlationId != value)
            {
                _correlationId = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected queue path.
    /// </summary>
    protected string? SelectedQueuePath
    {
        get => _selectedQueuePath;
        set
        {
            if (_selectedQueuePath != value)
            {
                _selectedQueuePath = value;
                ValidateInput();
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets the available queues from the connection.
    /// </summary>
    protected IEnumerable<QueueInfo> AvailableQueues =>
        Connection?.Queues ?? Enumerable.Empty<QueueInfo>();

    /// <summary>
    /// Gets a value indicating whether the input is valid.
    /// </summary>
    protected bool IsInputValid => string.IsNullOrEmpty(_validationError);

    /// <summary>
    /// Gets the validation error message.
    /// </summary>
    protected string? ValidationError => _validationError;

    /// <summary>
    /// Gets or sets whether the advanced options section is expanded.
    /// </summary>
    protected bool IsAdvancedOptionsExpanded
    {
        get => _isAdvancedOptionsExpanded;
        set
        {
            if (_isAdvancedOptionsExpanded != value)
            {
                _isAdvancedOptionsExpanded = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected text encoding.
    /// </summary>
    protected string SelectedTextEncoding
    {
        get => _selectedTextEncoding;
        set
        {
            if (_selectedTextEncoding != value)
            {
                _selectedTextEncoding = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Gets a unique ID for the dialog title.
    /// </summary>
    protected string DialogTitleId { get; } = $"send-message-dialog-title-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets a unique ID for the dialog body.
    /// </summary>
    protected string DialogBodyId { get; } = $"send-message-dialog-body-{Guid.NewGuid():N}";

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
    /// Gets the display text for a message format.
    /// </summary>
    protected string GetFormatDisplayText(MessageBodyFormat format)
    {
        return format switch
        {
            MessageBodyFormat.Text => "Plain Text",
            MessageBodyFormat.Json => "JSON",
            MessageBodyFormat.Xml => "XML",
            MessageBodyFormat.Binary => "Binary/Hex",
            MessageBodyFormat.Serialized => "Serialized Object",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the display text for a message priority.
    /// </summary>
    protected string GetPriorityDisplayText(MessagePriority priority)
    {
        return priority switch
        {
            MessagePriority.Lowest => "Lowest (0)",
            MessagePriority.VeryLow => "Very Low (1)",
            MessagePriority.Low => "Low (2)",
            MessagePriority.Normal => "Normal (3)",
            MessagePriority.AboveNormal => "Above Normal (4)",
            MessagePriority.High => "High (5)",
            MessagePriority.VeryHigh => "Very High (6)",
            MessagePriority.Highest => "Highest (7)",
            _ => priority.ToString()
        };
    }

    /// <summary>
    /// Gets display text for a queue (handles journal queues).
    /// </summary>
    protected string GetQueueDisplayText(QueueInfo queue, bool isJournal = false)
    {
        var displayName = !string.IsNullOrEmpty(queue.Name) ? queue.Name : queue.Path;
        return isJournal ? $"{displayName} (Journal)" : displayName;
    }

    /// <summary>
    /// Gets the available text encoding options.
    /// </summary>
    protected static Dictionary<string, string> GetAvailableEncodings()
    {
        return new Dictionary<string, string>
        {
            { "UTF-8", "UTF-8 (Unicode)" },
            { "UTF-16", "UTF-16 (Unicode)" },
            { "ASCII", "ASCII (7-bit)" },
            { "UTF-32", "UTF-32 (Unicode)" },
            { "ISO-8859-1", "ISO-8859-1 (Latin-1)" },
            { "Windows-1252", "Windows-1252 (ANSI)" }
        };
    }

    /// <summary>
    /// Handles the confirm button click.
    /// </summary>
    protected async Task OnConfirmAsync()
    {
        if (IsProcessing || !IsInputValid)
            return;

        ValidateInput();
        if (!IsInputValid)
            return;

        var request = new SendMessageRequest
        {
            QueuePath = SelectedQueuePath!,
            MessageContent = MessageContent,
            Label = MessageLabel,
            Format = SelectedFormat,
            Priority = SelectedPriority,
            Recoverable = Recoverable,
            IsTransactional = IsTransactional,
            TimeToReachQueue = TimeToReachQueueMinutes > 0 ? TimeSpan.FromMinutes(TimeToReachQueueMinutes) : TimeSpan.MaxValue,
            TimeToBeReceived = TimeToBeReceivedMinutes > 0 ? TimeSpan.FromMinutes(TimeToBeReceivedMinutes) : TimeSpan.MaxValue,
            CorrelationId = CorrelationId,
            TextEncoding = SelectedTextEncoding
        };

        if (OnConfirm.HasDelegate)
        {
            await OnConfirm.InvokeAsync(request);
        }
    }

    /// <summary>
    /// Handles the cancel button click.
    /// </summary>
    protected async Task OnCancelAsync()
    {
        if (IsProcessing)
            return;

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
    /// Toggles the advanced options section.
    /// </summary>
    protected void ToggleAdvancedOptions()
    {
        IsAdvancedOptionsExpanded = !IsAdvancedOptionsExpanded;
    }

    /// <summary>
    /// Public method to close the dialog (for parent component).
    /// </summary>
    public async Task CloseAsync()
    {
        await CloseDialogAsync();
    }

    /// <summary>
    /// Validates the current input.
    /// </summary>
    private void ValidateInput()
    {
        _validationError = null;

        if (string.IsNullOrWhiteSpace(SelectedQueuePath))
        {
            _validationError = "Please select a target queue.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageContent))
        {
            _validationError = "Message content is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageLabel))
        {
            _validationError = "Message label is required.";
            return;
        }

        // Check for journal queue warning
        if (SelectedQueuePath.EndsWith(";journal", StringComparison.OrdinalIgnoreCase))
        {
            _validationError = "Warning: Sending to journal queues is unusual and may not work as expected.";
            // Don't return here - it's just a warning
        }
    }

    /// <summary>
    /// Resets the dialog state to default values.
    /// </summary>
    private void ResetDialogState()
    {
        _messageContent = string.Empty;
        _messageLabel = string.Empty;
        _selectedFormat = MessageBodyFormat.Text;
        _selectedPriority = MessagePriority.Normal;
        _recoverable = true;
        _isTransactional = false;
        _timeToReachQueueMinutes = 0;
        _timeToBeReceivedMinutes = 0;
        _correlationId = string.Empty;
        _selectedQueuePath = InitialQueuePath;
        _validationError = null;
        _isAdvancedOptionsExpanded = false;
        _selectedTextEncoding = "UTF-8";
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
/// Represents a request to send a message.
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// Gets or sets the target queue path.
    /// </summary>
    public string QueuePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string MessageContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message format.
    /// </summary>
    public MessageBodyFormat Format { get; set; } = MessageBodyFormat.Text;

    /// <summary>
    /// Gets or sets the message priority.
    /// </summary>
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;

    /// <summary>
    /// Gets or sets whether the message is recoverable.
    /// </summary>
    public bool Recoverable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the message is transactional.
    /// </summary>
    public bool IsTransactional { get; set; } = false;

    /// <summary>
    /// Gets or sets the time to reach queue.
    /// </summary>
    public TimeSpan TimeToReachQueue { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Gets or sets the time to be received.
    /// </summary>
    public TimeSpan TimeToBeReceived { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text encoding to use for the message content.
    /// </summary>
    public string TextEncoding { get; set; } = "UTF-8";
}