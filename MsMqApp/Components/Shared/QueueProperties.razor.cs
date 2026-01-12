using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;

namespace MsMqApp.Components.Shared;

public class QueuePropertiesBase : ComponentBase
{
    [Parameter]
    public QueueInfo? Queue { get; set; }

    protected string ActiveTab { get; set; } = "general";

    // Edit mode state
    protected bool IsEditMode { get; set; } = false;
    protected bool IsDirty { get; set; } = false;
    protected bool IsSaving { get; set; } = false;

    // Editable properties
    protected string EditLabel { get; set; } = string.Empty;
    protected string EditTypeId { get; set; } = string.Empty;
    protected bool EditAuthenticate { get; set; }
    protected bool EditLimitMessageStorage { get; set; }
    protected long EditMaximumQueueSize { get; set; }
    protected int EditPrivacyLevel { get; set; }
    protected bool EditJournalEnabled { get; set; }
    protected bool EditLimitJournalStorage { get; set; }
    protected long EditMaximumJournalSize { get; set; }

    // Validation
    protected string? ValidationError { get; set; }
    protected Dictionary<string, string> FieldErrors { get; set; } = new();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Queue != null && !IsEditMode)
        {
            LoadQueueProperties();
        }
    }

    protected void LoadQueueProperties()
    {
        if (Queue == null)
        {
            return;
        }

        EditLabel = Queue.Label ?? string.Empty;
        EditTypeId = Queue.TypeId?.ToString().ToUpper() ?? "00000000-0000-0000-0000-000000000000";
        EditAuthenticate = Queue.Authenticate;
        EditLimitMessageStorage = Queue.MaximumQueueSize > 0;
        EditMaximumQueueSize = Queue.MaximumQueueSize > 0 ? Queue.MaximumQueueSize : 0;
        EditPrivacyLevel = Queue.PrivacyLevel;
        EditJournalEnabled = Queue.UseJournalQueue;
        EditLimitJournalStorage = Queue.MaximumJournalSize > 0;
        EditMaximumJournalSize = Queue.MaximumJournalSize > 0 ? Queue.MaximumJournalSize : 0;

        IsDirty = false;
        ValidationError = null;
        FieldErrors.Clear();
    }

    protected void SetActiveTab(string tab)
    {
        ActiveTab = tab;
        StateHasChanged();
    }

    protected void EnterEditMode()
    {
        IsEditMode = true;
        LoadQueueProperties();
        StateHasChanged();
    }

    protected void OnFieldChanged()
    {
        IsDirty = true;
        StateHasChanged();
    }

    protected bool ValidateFields()
    {
        FieldErrors.Clear();
        ValidationError = null;

        // Validate Type ID as GUID
        if (!string.IsNullOrWhiteSpace(EditTypeId) && !Guid.TryParse(EditTypeId, out _))
        {
            FieldErrors["TypeId"] = "Type ID must be a valid GUID format";
        }

        // Validate storage limits
        if (EditLimitMessageStorage && EditMaximumQueueSize <= 0)
        {
            FieldErrors["MaximumQueueSize"] = "Message storage limit must be greater than 0";
        }

        if (EditLimitJournalStorage && EditMaximumJournalSize <= 0)
        {
            FieldErrors["MaximumJournalSize"] = "Journal storage limit must be greater than 0";
        }

        if (FieldErrors.Any())
        {
            ValidationError = "Please fix the validation errors before saving.";
            StateHasChanged();
            return false;
        }

        return true;
    }

    protected async Task OnSaveAsync()
    {
        if (!ValidateFields())
        {
            return;
        }

        IsSaving = true;
        StateHasChanged();

        try
        {
            // TODO: Call backend service to save properties
            await Task.Delay(500); // Simulate API call

            // For now, just update the local Queue object
            if (Queue != null)
            {
                Queue.Label = EditLabel;
                if (Guid.TryParse(EditTypeId, out var typeId))
                {
                    Queue.TypeId = typeId;
                }
                Queue.Authenticate = EditAuthenticate;
                Queue.MaximumQueueSize = EditLimitMessageStorage ? EditMaximumQueueSize : 0;
                Queue.PrivacyLevel = EditPrivacyLevel;
                Queue.UseJournalQueue = EditJournalEnabled;
                Queue.MaximumJournalSize = EditLimitJournalStorage ? EditMaximumJournalSize : 0;
            }

            IsDirty = false;
            IsEditMode = false;
            ValidationError = null;
            // TODO: Show success message
        }
        catch (Exception ex)
        {
            ValidationError = $"Failed to save changes: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
            StateHasChanged();
        }
    }

    protected void OnCancel()
    {
        LoadQueueProperties();
        IsEditMode = false;
        IsDirty = false;
        StateHasChanged();
    }

    protected void OnApply()
    {
        // Apply without closing edit mode
        OnSaveAsync().GetAwaiter().GetResult();
        if (!FieldErrors.Any())
        {
            IsDirty = false;
        }
    }

    protected string GetTypeIdDisplay()
    {
        if (Queue?.TypeId != null && Queue.TypeId != Guid.Empty)
        {
            return $"{{{Queue.TypeId.Value.ToString().ToUpper()}}}";
        }
        return "{00000000-0000-0000-0000-000000000000}";
    }

    protected string GetPrivacyLevelDisplay()
    {
        return Queue?.PrivacyLevel switch
        {
            0 => "None",
            1 => "Optional",
            2 => "Body",
            _ => "Optional"
        };
    }

    protected bool HasMessageStorageLimit()
    {
        return Queue?.MaximumQueueSize > 0;
    }

    protected string GetMaxQueueSizeDisplay()
    {
        return Queue?.MaximumQueueSize > 0
            ? Queue.MaximumQueueSize.ToString("N0")
            : "Unlimited";
    }

    protected bool HasJournalStorageLimit()
    {
        return Queue?.MaximumJournalSize > 0;
    }

    protected string GetMaxJournalSizeDisplay()
    {
        return Queue?.MaximumJournalSize > 0
            ? Queue.MaximumJournalSize.ToString("N0")
            : "Unlimited";
    }

    protected string GetComputerName()
    {
        return Queue?.ComputerName ?? Environment.MachineName;
    }

    protected string GetCurrentUser()
    {
        return $"{Environment.UserDomainName}\\{Environment.UserName}";
    }
}
