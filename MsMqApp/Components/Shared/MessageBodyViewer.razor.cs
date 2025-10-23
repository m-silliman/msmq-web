using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using System.Text;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for viewing and formatting message body content with syntax highlighting,
/// format selection, and copy-to-clipboard functionality.
/// </summary>
public class MessageBodyViewerBase : ComponentBase, IAsyncDisposable
{
    private MessageBodyFormat _selectedFormat = MessageBodyFormat.Unknown;
    private bool _showCopySuccess;
    private System.Timers.Timer? _copySuccessTimer;
    private const int CopySuccessDisplayMs = 2000;
    private const int MaxDisplaySizeBytes = 1024 * 1024; // 1MB default

    /// <summary>
    /// Gets or sets the JSRuntime for JavaScript interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the message body to display.
    /// </summary>
    [Parameter]
    public MessageBody? MessageBody { get; set; }

    /// <summary>
    /// Gets or sets whether the viewer is in a loading state.
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets whether the viewer is disabled.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Gets or sets whether to show metadata footer.
    /// </summary>
    [Parameter]
    public bool ShowMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum display size in bytes. Content larger than this will be truncated.
    /// Default is 1MB.
    /// </summary>
    [Parameter]
    public int MaxDisplaySize { get; set; } = MaxDisplaySizeBytes;

    /// <summary>
    /// Gets or sets the CSS class for the component container.
    /// </summary>
    [Parameter]
    public string? CssClass { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the format changes.
    /// </summary>
    [Parameter]
    public EventCallback<MessageBodyFormat> OnFormatChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when copy to clipboard is successful.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnCopySuccess { get; set; }

    /// <summary>
    /// Gets or sets the selected format override.
    /// </summary>
    protected MessageBodyFormat SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (_selectedFormat != value)
            {
                _selectedFormat = value;
                OnSelectedFormatChangedAsync();
            }
        }
    }

    /// <summary>
    /// Gets whether content is being copied.
    /// </summary>
    protected bool IsCopying { get; private set; }

    /// <summary>
    /// Gets whether to show the copy success indicator.
    /// </summary>
    protected bool ShowCopySuccess => _showCopySuccess;

    /// <summary>
    /// Gets whether the content is truncated.
    /// </summary>
    protected bool IsTruncated { get; private set; }

    /// <summary>
    /// Gets the formatted content for display.
    /// </summary>
    protected string FormattedContent { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the formatted size string.
    /// </summary>
    protected string FormattedSize => FormatBytes(MessageBody?.SizeBytes ?? 0);

    /// <summary>
    /// Gets the formatted original size (for truncated content).
    /// </summary>
    protected string FormattedOriginalSize => FormatBytes(MessageBody?.SizeBytes ?? 0);

    /// <summary>
    /// Gets the max display size in KB for display purposes.
    /// </summary>
    protected int MaxDisplaySizeKB => MaxDisplaySize / 1024;

    /// <summary>
    /// Gets a unique ID for the format selector.
    /// </summary>
    protected string FormatSelectId { get; } = $"format-select-{Guid.NewGuid():N}";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateFormattedContent();
    }

    /// <summary>
    /// Updates the formatted content based on the current body and format selection.
    /// </summary>
    private void UpdateFormattedContent()
    {
        if (MessageBody == null || string.IsNullOrWhiteSpace(MessageBody.RawContent))
        {
            FormattedContent = string.Empty;
            IsTruncated = false;
            return;
        }

        // Determine the effective format
        var effectiveFormat = _selectedFormat == MessageBodyFormat.Unknown
            ? MessageBody.DetectFormat()
            : _selectedFormat;

        // Check if we need to truncate
        var originalSize = MessageBody.SizeBytes;
        IsTruncated = originalSize > MaxDisplaySize;

        // Get the content to format
        string contentToFormat;
        if (IsTruncated)
        {
            // Truncate the raw content before formatting
            var bytes = Encoding.UTF8.GetBytes(MessageBody.RawContent);
            var truncatedBytes = bytes.Take(MaxDisplaySize).ToArray();
            contentToFormat = Encoding.UTF8.GetString(truncatedBytes);
        }
        else
        {
            contentToFormat = MessageBody.RawContent;
        }

        // Create a temporary MessageBody for formatting
        var tempBody = new MessageBody(contentToFormat)
        {
            Format = effectiveFormat,
            Encoding = MessageBody.Encoding,
            RawBytes = MessageBody.RawBytes != null && IsTruncated
                ? MessageBody.RawBytes.Take(MaxDisplaySize).ToArray()
                : MessageBody.RawBytes
        };

        FormattedContent = tempBody.GetFormattedContent();
    }

    /// <summary>
    /// Gets the CSS class for the content area based on format.
    /// </summary>
    protected string GetContentClass()
    {
        var classes = new List<string>();

        if (!string.IsNullOrWhiteSpace(CssClass))
        {
            classes.Add(CssClass);
        }

        if (IsDisabled)
        {
            classes.Add("disabled");
        }

        return string.Join(" ", classes);
    }

    /// <summary>
    /// Gets the syntax highlighting CSS class based on the current format.
    /// </summary>
    protected string GetSyntaxHighlightClass()
    {
        if (MessageBody == null) return string.Empty;

        var effectiveFormat = _selectedFormat == MessageBodyFormat.Unknown
            ? MessageBody.DetectFormat()
            : _selectedFormat;

        return effectiveFormat switch
        {
            MessageBodyFormat.Xml => "language-xml",
            MessageBodyFormat.Json => "language-json",
            MessageBodyFormat.Binary => "language-none",
            MessageBodyFormat.Text => "language-none",
            _ => "language-none"
        };
    }

    /// <summary>
    /// Gets the display text for the current format.
    /// </summary>
    protected string GetFormatDisplayText()
    {
        if (MessageBody == null) return "Unknown";

        var effectiveFormat = _selectedFormat == MessageBodyFormat.Unknown
            ? MessageBody.DetectFormat()
            : _selectedFormat;

        return effectiveFormat switch
        {
            MessageBodyFormat.Xml => "XML",
            MessageBodyFormat.Json => "JSON",
            MessageBodyFormat.Text => "Plain Text",
            MessageBodyFormat.Binary => "Binary (Hex)",
            MessageBodyFormat.Serialized => "Serialized Object",
            MessageBodyFormat.Unknown => "Unknown",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Handles the format selection change.
    /// </summary>
    private async void OnSelectedFormatChangedAsync()
    {
        UpdateFormattedContent();
        StateHasChanged();

        if (OnFormatChanged.HasDelegate)
        {
            await OnFormatChanged.InvokeAsync(_selectedFormat);
        }
    }

    /// <summary>
    /// Copies the formatted content to the clipboard.
    /// </summary>
    protected async Task CopyToClipboardAsync()
    {
        if (string.IsNullOrWhiteSpace(FormattedContent) || IsCopying)
        {
            return;
        }

        try
        {
            IsCopying = true;
            StateHasChanged();

            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", FormattedContent);

            // Show success indicator
            _showCopySuccess = true;
            StateHasChanged();

            // Start timer to hide success indicator
            _copySuccessTimer?.Dispose();
            _copySuccessTimer = new System.Timers.Timer(CopySuccessDisplayMs);
            _copySuccessTimer.Elapsed += (s, e) =>
            {
                _showCopySuccess = false;
                _copySuccessTimer?.Dispose();
                InvokeAsync(StateHasChanged);
            };
            _copySuccessTimer.AutoReset = false;
            _copySuccessTimer.Start();

            // Invoke callback if provided
            if (OnCopySuccess.HasDelegate)
            {
                await OnCopySuccess.InvokeAsync(FormattedContent);
            }
        }
        catch (Exception)
        {
            // Silently fail - clipboard access might be denied
            // In production, you might want to show a toast notification
        }
        finally
        {
            IsCopying = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Formats a byte count into a human-readable string.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _copySuccessTimer?.Dispose();
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
