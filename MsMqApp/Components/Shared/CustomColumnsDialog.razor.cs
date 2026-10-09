using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Configuration;

namespace MsMqApp.Components.Shared;

public class CustomColumnsDialogBase : ComponentBase
{
    private bool _initializedForOpen;
    private CustomColumnProfile? _inputProfile;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public CustomColumnProfile? Profile { get; set; }
    [Parameter] public EventCallback<CustomColumnProfile> OnSave { get; set; }

    protected List<CustomColumnDefinition> DraftColumns { get; private set; } = [];
    protected string Header { get; set; } = string.Empty;
    protected string JsonPath { get; set; } = string.Empty;
    protected Guid? EditingId { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string DialogTitleId { get; } = $"custom-columns-title-{Guid.NewGuid():N}";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!IsOpen)
        {
            _initializedForOpen = false;
            _inputProfile = null;
            return;
        }

        if (!_initializedForOpen || !ReferenceEquals(_inputProfile, Profile))
        {
            _inputProfile = Profile;
            DraftColumns = Profile?.Columns
                .OrderBy(column => column.Order)
                .Select(Clone)
                .ToList() ?? [];
            ResetEditor();
            _initializedForOpen = true;
        }
    }

    protected void ShowAll()
    {
        foreach (var column in DraftColumns)
        {
            column.IsVisible = true;
        }
    }

    protected void HideAll()
    {
        foreach (var column in DraftColumns)
        {
            column.IsVisible = false;
        }
    }

    protected void BeginEdit(CustomColumnDefinition column)
    {
        EditingId = column.Id;
        Header = column.Header;
        JsonPath = column.JsonPath;
        ErrorMessage = null;
    }

    protected void Remove(CustomColumnDefinition column)
    {
        DraftColumns.Remove(column);
        if (EditingId == column.Id)
        {
            ResetEditor();
        }
    }

    protected void SetVisibility(CustomColumnDefinition column, ChangeEventArgs args)
    {
        column.IsVisible = args.Value is bool value && value;
    }

    protected void SaveColumn()
    {
        var header = Header.Trim();
        var jsonPath = JsonPath.Trim();
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(jsonPath))
        {
            ErrorMessage = "A header and JSON path are required.";
            return;
        }

        if (DraftColumns.Any(column => column.Id != EditingId &&
            string.Equals(column.Header, header, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "Each custom column must have a unique header.";
            return;
        }

        if (EditingId is Guid editingId)
        {
            var column = DraftColumns.FirstOrDefault(item => item.Id == editingId);
            if (column != null)
            {
                column.Header = header;
                column.JsonPath = jsonPath;
            }
        }
        else
        {
            DraftColumns.Add(new CustomColumnDefinition
            {
                Header = header,
                JsonPath = jsonPath,
                Order = DraftColumns.Count
            });
        }

        ResetEditor();
    }

    protected async Task SaveAsync()
    {
        if (!string.IsNullOrWhiteSpace(Header) || !string.IsNullOrWhiteSpace(JsonPath))
        {
            SaveColumn();
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                return;
            }
        }

        var profile = new CustomColumnProfile
        {
            Version = 1,
            ServerKey = Profile?.ServerKey ?? string.Empty,
            QueueKey = Profile?.QueueKey ?? string.Empty,
            Columns = DraftColumns
                .Select((column, index) =>
                {
                    column.Order = index;
                    return Clone(column);
                })
                .ToList()
        };

        if (OnSave.HasDelegate)
        {
            await OnSave.InvokeAsync(profile);
        }

        await CloseAsync();
    }

    protected Task CancelAsync() => CloseAsync();

    protected Task CloseFromBackdropAsync() => CloseAsync();

    private async Task CloseAsync()
    {
        IsOpen = false;
        if (IsOpenChanged.HasDelegate)
        {
            await IsOpenChanged.InvokeAsync(false);
        }
    }

    private void ResetEditor()
    {
        Header = string.Empty;
        JsonPath = string.Empty;
        EditingId = null;
        ErrorMessage = null;
    }

    private static CustomColumnDefinition Clone(CustomColumnDefinition column) => new()
    {
        Id = column.Id == Guid.Empty ? Guid.NewGuid() : column.Id,
        Header = column.Header,
        JsonPath = column.JsonPath,
        IsVisible = column.IsVisible,
        Order = column.Order
    };
}

