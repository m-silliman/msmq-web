using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Components.Shared;

public class QueuePropertiesBase : ComponentBase
{
    [Parameter]
    public QueueInfo? Queue { get; set; }

    [Inject]
    protected IQueueManagementService QueueManagementService { get; set; } = default!;

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
    protected string? SuccessMessage { get; set; }
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

        if (Queue == null)
        {
            ValidationError = "No queue selected";
            return;
        }

        IsSaving = true;
        ValidationError = null;
        StateHasChanged();

        try
        {
            // Use FormatName or Path for queue identification
            var queuePath = !string.IsNullOrEmpty(Queue.FormatName) ? Queue.FormatName : Queue.Path;

            // Call the service to update queue properties
            var result = await QueueManagementService.UpdateQueuePropertiesAsync(
                queuePath,
                EditLabel,
                EditAuthenticate,
                EditLimitMessageStorage ? EditMaximumQueueSize : 0,
                EditPrivacyLevel,
                EditJournalEnabled,
                EditLimitJournalStorage ? EditMaximumJournalSize : 0);

            if (result.Success)
            {
                // Update the local Queue object to reflect the changes
                Queue.Label = EditLabel;
                Queue.Authenticate = EditAuthenticate;
                Queue.MaximumQueueSize = EditLimitMessageStorage ? EditMaximumQueueSize : 0;
                Queue.PrivacyLevel = EditPrivacyLevel;
                Queue.UseJournalQueue = EditJournalEnabled;
                Queue.MaximumJournalSize = EditLimitJournalStorage ? EditMaximumJournalSize : 0;

                IsDirty = false;
                IsEditMode = false;
                ValidationError = null;
                SuccessMessage = "Queue properties updated successfully";

                // Clear success message after 3 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    SuccessMessage = null;
                    await InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                ValidationError = result.ErrorMessage ?? "Failed to update queue properties";
            }
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
