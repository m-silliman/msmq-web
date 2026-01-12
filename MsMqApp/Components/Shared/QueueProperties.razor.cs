using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;

namespace MsMqApp.Components.Shared;

public class QueuePropertiesBase : ComponentBase
{
    [Parameter]
    public QueueInfo? Queue { get; set; }

    protected string ActiveTab { get; set; } = "general";

    protected void SetActiveTab(string tab)
    {
        ActiveTab = tab;
        StateHasChanged();
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
