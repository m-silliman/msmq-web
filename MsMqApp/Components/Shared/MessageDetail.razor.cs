using Microsoft.AspNetCore.Components;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;

namespace MsMqApp.Components.Shared;

/// <summary>
/// Component for displaying detailed message information in a sliding drawer panel.
/// </summary>
public class MessageDetailBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the message to display.
    /// </summary>
    [Parameter]
    public QueueMessage? Message { get; set; }

    /// <summary>
    /// Gets or sets whether the drawer is open.
    /// </summary>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Gets or sets the callback when IsOpen changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the close button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the delete button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage> OnDelete { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the move button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage> OnMove { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the export button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage> OnExport { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the resend button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<QueueMessage> OnResend { get; set; }

    /// <summary>
    /// Gets or sets whether an operation is in progress.
    /// </summary>
    protected bool IsOperationInProgress { get; set; }

    /// <summary>
    /// Gets the CSS class for the drawer based on its state.
    /// </summary>
    /// <returns>The CSS class string.</returns>
    protected string GetDrawerClass()
    {
        return IsOpen ? "drawer-open" : "drawer-closed";
    }

    /// <summary>
    /// Gets the priority badge CSS class.
    /// </summary>
    /// <returns>The CSS class string.</returns>
    protected string GetPriorityBadgeClass()
    {
        if (Message == null) return "bg-secondary";

        return Message.Priority switch
        {
            MessagePriority.Lowest => "bg-secondary",
            MessagePriority.VeryLow => "bg-info",
            MessagePriority.Low => "bg-primary",
            MessagePriority.Normal => "bg-success",
            MessagePriority.AboveNormal => "bg-success",
            MessagePriority.High => "bg-warning text-dark",
            MessagePriority.VeryHigh => "bg-danger",
            MessagePriority.Highest => "bg-danger",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Handles the close button click.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnCloseAsync()
    {
        IsOpen = false;

        if (IsOpenChanged.HasDelegate)
        {
            await IsOpenChanged.InvokeAsync(IsOpen);
        }

        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Handles the delete button click.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnDeleteClickedAsync()
    {
        if (Message == null || IsOperationInProgress) return;

        IsOperationInProgress = true;
        StateHasChanged();

        try
        {
            if (OnDelete.HasDelegate)
            {
                await OnDelete.InvokeAsync(Message);
            }
        }
        finally
        {
            IsOperationInProgress = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles the move button click.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnMoveClickedAsync()
    {
        if (Message == null || IsOperationInProgress) return;

        IsOperationInProgress = true;
        StateHasChanged();

        try
        {
            if (OnMove.HasDelegate)
            {
                await OnMove.InvokeAsync(Message);
            }
        }
        finally
        {
            IsOperationInProgress = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles the export button click.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnExportClickedAsync()
    {
        if (Message == null || IsOperationInProgress) return;

        IsOperationInProgress = true;
        StateHasChanged();

        try
        {
            if (OnExport.HasDelegate)
            {
                await OnExport.InvokeAsync(Message);
            }
        }
        finally
        {
            IsOperationInProgress = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles the resend button click.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnResendClickedAsync()
    {
        if (Message == null || IsOperationInProgress) return;

        IsOperationInProgress = true;
        StateHasChanged();

        try
        {
            if (OnResend.HasDelegate)
            {
                await OnResend.InvokeAsync(Message);
            }
        }
        finally
        {
            IsOperationInProgress = false;
            StateHasChanged();
        }
    }
}
