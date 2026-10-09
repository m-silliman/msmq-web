using Microsoft.AspNetCore.Components;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Collects the ZIP filename and confirms a selected-message export.
/// </summary>
public class ExportMessagesDialogBase : ComponentBase
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter]
    public int MessageCount { get; set; }

    [Parameter]
    public string FileName { get; set; } = "messages";

    [Parameter]
    public EventCallback<string> FileNameChanged { get; set; }

    [Parameter]
    public bool IsProcessing { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public EventCallback OnConfirm { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected string DialogTitleId { get; } = $"export-dialog-title-{Guid.NewGuid():N}";
    protected string DialogMessageId { get; } = $"export-dialog-message-{Guid.NewGuid():N}";
    protected string FileNameInputId { get; } = $"export-file-name-{Guid.NewGuid():N}";

    protected bool IsFileNameValid => !string.IsNullOrWhiteSpace(FileName);

    protected async Task OnFileNameChangedAsync(ChangeEventArgs args)
    {
        if (FileNameChanged.HasDelegate)
        {
            await FileNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
        }
    }

    protected async Task OnConfirmAsync()
    {
        if (IsProcessing || !IsFileNameValid)
        {
            return;
        }

        if (OnConfirm.HasDelegate)
        {
            await OnConfirm.InvokeAsync();
        }
    }

    protected async Task OnCancelAsync()
    {
        if (IsProcessing)
        {
            return;
        }

        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }

        await CloseAsync();
    }

    protected async Task OnBackdropClickAsync()
    {
        if (!IsProcessing)
        {
            await OnCancelAsync();
        }
    }

    private async Task CloseAsync()
    {
        IsOpen = false;
        if (IsOpenChanged.HasDelegate)
        {
            await IsOpenChanged.InvokeAsync(false);
        }
    }
}
