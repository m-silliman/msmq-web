using Microsoft.AspNetCore.Components;
using MsMqApp.Components.Shared;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.UI;
using MsMqApp.Services;

namespace MsMqApp.Components.Pages;

/// <summary>
/// Demo page showcasing reusable components with theme support.
/// </summary>
public class ComponentDemoBase : ComponentBase, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets or sets the theme service.
    /// </summary>
    [Inject]
    protected IThemeService ThemeService { get; set; } = default!;

    /// <summary>
    /// Gets the sample tree data for demonstration.
    /// </summary>
    protected TreeNodeData? SampleTreeData { get; private set; }

    /// <summary>
    /// Gets the information about the selected node.
    /// </summary>
    protected string SelectedNodeInfo { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the sample queue connection for demonstration.
    /// </summary>
    protected QueueConnection? SampleConnection { get; private set; }

    /// <summary>
    /// Gets the selected queue from the tree view.
    /// </summary>
    protected QueueInfo? SelectedQueueFromTree { get; private set; }

    /// <summary>
    /// Gets the sample selected queue for MessageList demonstration.
    /// </summary>
    protected QueueInfo? SampleSelectedQueue { get; private set; }

    /// <summary>
    /// Gets the sample messages for demonstration.
    /// </summary>
    protected List<QueueMessage> SampleMessages { get; private set; } = new();

    /// <summary>
    /// Gets the selected message from the list.
    /// </summary>
    protected QueueMessage? SelectedMessageFromList { get; set; }

    /// <summary>
    /// Gets the list of bulk selected messages.
    /// </summary>
    protected List<QueueMessage> BulkSelectedMessages { get; private set; } = new();

    /// <summary>
    /// Gets or sets whether messages are being refreshed.
    /// </summary>
    protected bool IsRefreshingMessages { get; set; }

    /// <summary>
    /// Gets or sets whether messages are loading.
    /// </summary>
    protected bool IsLoadingMessages { get; set; }

    /// <summary>
    /// Gets the demo selected message ID.
    /// </summary>
    protected string? DemoSelectedMessageId { get; private set; }

    /// <summary>
    /// Gets the demo checked message IDs.
    /// </summary>
    protected HashSet<string> DemoCheckedMessageIds { get; } = new();

    /// <summary>
    /// Gets the demo message info text.
    /// </summary>
    protected string DemoMessageInfo { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message for the detail drawer.
    /// </summary>
    protected QueueMessage? DetailDrawerMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the detail drawer is open.
    /// </summary>
    protected bool IsDetailDrawerOpen { get; set; }

    /// <summary>
    /// Gets the detail drawer operation log.
    /// </summary>
    protected string DetailDrawerOperationLog { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the confirmation dialog is open.
    /// </summary>
    protected bool IsDialogOpen { get; set; }

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    protected string DialogTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the dialog message.
    /// </summary>
    protected string DialogMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the dialog detail message.
    /// </summary>
    protected string? DialogDetailMessage { get; private set; }

    /// <summary>
    /// Gets the dialog custom content.
    /// </summary>
    protected string? DialogCustomContent { get; private set; }

    /// <summary>
    /// Gets the current dialog severity.
    /// </summary>
    protected DialogSeverity CurrentDialogSeverity { get; private set; } = DialogSeverity.Warning;

    /// <summary>
    /// Gets the dialog confirm button text.
    /// </summary>
    protected string DialogConfirmText { get; private set; } = "Confirm";

    /// <summary>
    /// Gets the dialog cancel button text.
    /// </summary>
    protected string DialogCancelText { get; private set; } = "Cancel";

    /// <summary>
    /// Gets whether the dialog should show the close button.
    /// </summary>
    protected bool DialogShowCloseButton { get; private set; } = true;

    /// <summary>
    /// Gets whether the dialog closes on backdrop click.
    /// </summary>
    protected bool DialogCloseOnBackdrop { get; private set; }

    /// <summary>
    /// Gets whether the dialog is processing an operation.
    /// </summary>
    protected bool IsDialogProcessing { get; private set; }

    /// <summary>
    /// Gets the dialog result message.
    /// </summary>
    protected string DialogResultMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the last dialog result was confirmed.
    /// </summary>
    protected bool DialogResultConfirmed { get; private set; }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ThemeService.ThemeChanged += OnThemeChanged;
        InitializeSampleTreeData();
        InitializeSampleConnection();
        InitializeSampleMessages();
        InitializeSampleSelectedQueue();
    }

    /// <summary>
    /// Initializes the sample tree data for demonstration.
    /// </summary>
    private void InitializeSampleTreeData()
    {
        SampleTreeData = new TreeNodeData
        {
            Id = "root",
            Text = "Local Computer",
            IconClass = "bi bi-pc-display",
            IsExpanded = true,
            HasChildren = true,
            Level = 0,
            Children = new List<TreeNodeData>
            {
                new TreeNodeData
                {
                    Id = "private",
                    Text = "Private Queues",
                    IconClass = "bi bi-folder",
                    IsExpanded = true,
                    HasChildren = true,
                    Level = 1,
                    Children = new List<TreeNodeData>
                    {
                        new TreeNodeData
                        {
                            Id = "orders",
                            Text = "orders",
                            IconClass = "bi bi-inbox",
                            BadgeCount = 42,
                            HasChildren = false,
                            Level = 2
                        },
                        new TreeNodeData
                        {
                            Id = "payments",
                            Text = "payments",
                            IconClass = "bi bi-inbox",
                            BadgeCount = 127,
                            HasChildren = false,
                            Level = 2
                        },
                        new TreeNodeData
                        {
                            Id = "notifications",
                            Text = "notifications",
                            IconClass = "bi bi-inbox",
                            BadgeCount = 0,
                            HasChildren = false,
                            Level = 2
                        }
                    }
                },
                new TreeNodeData
                {
                    Id = "system",
                    Text = "System Queues",
                    IconClass = "bi bi-folder",
                    IsExpanded = false,
                    HasChildren = true,
                    Level = 1,
                    Children = new List<TreeNodeData>
                    {
                        new TreeNodeData
                        {
                            Id = "deadletter",
                            Text = "Dead Letter Queue",
                            IconClass = "bi bi-x-circle",
                            BadgeCount = 5,
                            HasChildren = false,
                            Level = 2
                        },
                        new TreeNodeData
                        {
                            Id = "journal",
                            Text = "Journal Queue",
                            IconClass = "bi bi-journal-text",
                            BadgeCount = 1523,
                            HasChildren = false,
                            Level = 2
                        }
                    }
                },
                new TreeNodeData
                {
                    Id = "public",
                    Text = "Public Queues",
                    IconClass = "bi bi-folder",
                    IsExpanded = false,
                    HasChildren = true,
                    Level = 1,
                    Children = new List<TreeNodeData>
                    {
                        new TreeNodeData
                        {
                            Id = "audit",
                            Text = "audit",
                            IconClass = "bi bi-inbox",
                            BadgeCount = 10532,
                            HasChildren = false,
                            Level = 2
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Initializes the sample queue connection for demonstration.
    /// </summary>
    private void InitializeSampleConnection()
    {
        SampleConnection = new QueueConnection
        {
            Id = "local-connection",
            ComputerName = Environment.MachineName,
            DisplayName = "Local Computer",
            IsLocal = true,
            Status = ConnectionStatus.Connected,
            ConnectedAt = DateTime.UtcNow.AddMinutes(-15),
            LastRefreshedAt = DateTime.UtcNow.AddSeconds(-30),
            AutoRefreshEnabled = true,
            AutoRefreshIntervalSeconds = 5,
            ShowSystemQueues = true,
            ShowJournalQueues = true
        };

        // Add sample queues
        var queues = new List<QueueInfo>
        {
            // Private Queues
            new QueueInfo
            {
                Id = "queue-orders",
                Name = "orders",
                Path = @".\private$\orders",
                FormatName = @"DIRECT=OS:.\private$\orders",
                ComputerName = ".",
                QueueType = QueueType.Private,
                MessageCount = 42,
                IsTransactional = true,
                Label = "Order Processing Queue",
                CreateTime = DateTime.UtcNow.AddDays(-30),
                CanRead = true,
                CanWrite = true,
                IsLocal = true,
                IsAccessible = true
            },
            new QueueInfo
            {
                Id = "queue-payments",
                Name = "payments",
                Path = @".\private$\payments",
                FormatName = @"DIRECT=OS:.\private$\payments",
                ComputerName = ".",
                QueueType = QueueType.Private,
                MessageCount = 127,
                IsTransactional = true,
                Label = "Payment Processing Queue",
                CreateTime = DateTime.UtcNow.AddDays(-25),
                CanRead = true,
                CanWrite = true,
                IsLocal = true,
                IsAccessible = true
            },
            new QueueInfo
            {
                Id = "queue-notifications",
                Name = "notifications",
                Path = @".\private$\notifications",
                FormatName = @"DIRECT=OS:.\private$\notifications",
                ComputerName = ".",
                QueueType = QueueType.Private,
                MessageCount = 0,
                IsTransactional = false,
                Label = "Notification Queue",
                CreateTime = DateTime.UtcNow.AddDays(-20),
                CanRead = true,
                CanWrite = true,
                IsLocal = true,
                IsAccessible = true
            },
            new QueueInfo
            {
                Id = "queue-logging",
                Name = "logging",
                Path = @".\private$\logging",
                FormatName = @"DIRECT=OS:.\private$\logging",
                ComputerName = ".",
                QueueType = QueueType.Private,
                MessageCount = 3,
                IsTransactional = false,
                Label = "Application Logging Queue",
                CreateTime = DateTime.UtcNow.AddDays(-15),
                CanRead = true,
                CanWrite = true,
                IsLocal = true,
                IsAccessible = true
            },
            // System Queues
            new QueueInfo
            {
                Id = "queue-deadletter",
                Name = "DeadLetter",
                Path = @".\System$\DeadLetter",
                FormatName = @"DIRECT=OS:.\System$\DeadLetter",
                ComputerName = ".",
                QueueType = QueueType.DeadLetter,
                MessageCount = 5,
                IsTransactional = false,
                Label = "Dead Letter Queue",
                CanRead = true,
                CanWrite = false,
                IsLocal = true,
                IsAccessible = true
            },
            new QueueInfo
            {
                Id = "queue-transdeadletter",
                Name = "TransactionalDeadLetter",
                Path = @".\System$\TransDeadLetter",
                FormatName = @"DIRECT=OS:.\System$\TransDeadLetter",
                ComputerName = ".",
                QueueType = QueueType.TransactionalDeadLetter,
                MessageCount = 2,
                IsTransactional = true,
                Label = "Transactional Dead Letter Queue",
                CanRead = true,
                CanWrite = false,
                IsLocal = true,
                IsAccessible = true
            },
            // Journal Queue
            new QueueInfo
            {
                Id = "queue-journal",
                Name = "Journal",
                Path = @".\System$\Journal",
                FormatName = @"DIRECT=OS:.\System$\Journal",
                ComputerName = ".",
                QueueType = QueueType.Journal,
                MessageCount = 1523,
                IsTransactional = false,
                Label = "System Journal Queue",
                CanRead = true,
                CanWrite = false,
                IsLocal = true,
                IsAccessible = true
            },
            // Public Queue
            new QueueInfo
            {
                Id = "queue-audit",
                Name = "audit",
                Path = @"MACHINE\audit",
                FormatName = @"DIRECT=OS:MACHINE\audit",
                ComputerName = Environment.MachineName,
                QueueType = QueueType.Public,
                MessageCount = 10532,
                IsTransactional = true,
                Label = "Audit Queue",
                CreateTime = DateTime.UtcNow.AddDays(-60),
                CanRead = true,
                CanWrite = true,
                IsLocal = true,
                IsAccessible = true
            }
        };

        SampleConnection.RefreshQueues(queues);
    }

    /// <summary>
    /// Handles queue selection from the tree view.
    /// </summary>
    /// <param name="queue">The selected queue.</param>
    protected void HandleQueueSelected(QueueInfo? queue)
    {
        SelectedQueueFromTree = queue;
        StateHasChanged();
    }

    /// <summary>
    /// Handles refresh request from the tree view.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleRefreshRequested()
    {
        if (SampleConnection == null)
        {
            return;
        }

        // Simulate a refresh delay
        await Task.Delay(1000);

        // Update message counts to simulate changes
        var random = new Random();
        foreach (var queue in SampleConnection.Queues)
        {
            // Randomly adjust message count
            var change = random.Next(-5, 10);
            queue.MessageCount = Math.Max(0, queue.MessageCount + change);
        }

        SampleConnection.LastRefreshedAt = DateTime.UtcNow;
        StateHasChanged();
    }

    /// <summary>
    /// Initializes sample messages for demonstration.
    /// </summary>
    private void InitializeSampleMessages()
    {
        var baseTime = DateTime.Now;
        var random = new Random(42); // Fixed seed for consistent demo

        var labels = new[] { "Order Created", "Payment Processed", "Shipping Notification", "Inventory Update",
            "Customer Registration", "Email Sent", "Report Generated", "Backup Completed",
            "Data Sync", "API Request", "Cache Invalidation", "User Login", "Error Report",
            "System Health Check", "Configuration Updated", "File Uploaded", "Task Scheduled",
            "Notification Queued", "Log Entry", "Audit Record" };

        var priorities = Enum.GetValues<MessagePriority>();

        for (int i = 0; i < 50; i++)
        {
            var label = labels[i % labels.Length];
            var priority = priorities[random.Next(priorities.Length)];
            var minutesAgo = random.Next(1, 10080); // Up to 7 days ago
            var bodySize = random.Next(100, 50000);

            var message = new QueueMessage
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"{label} #{i + 1}",
                Body = new MessageBody($"Sample message body content for {label}")
                {
                    Format = MessageBodyFormat.Text
                },
                QueuePath = @".\private$\orders",
                Priority = priority,
                ArrivedTime = baseTime.AddMinutes(-minutesAgo),
                SentTime = baseTime.AddMinutes(-minutesAgo - 1),
                CorrelationId = random.Next(100) > 70 ? Guid.NewGuid().ToString() : string.Empty,
                Recoverable = random.Next(100) > 30,
                IsTransactional = random.Next(100) > 50,
                ResponseQueue = random.Next(100) > 80 ? @".\private$\responses" : null,
                UseJournalQueue = random.Next(100) > 60
            };

            // Set body size
            message.Body.RawBytes = new byte[bodySize];

            SampleMessages.Add(message);
        }
    }

    /// <summary>
    /// Initializes sample selected queue for MessageList demonstration.
    /// </summary>
    private void InitializeSampleSelectedQueue()
    {
        SampleSelectedQueue = new QueueInfo
        {
            Id = Guid.NewGuid().ToString(),
            Path = @".\private$\orders.processing",
            Name = "orders.processing",
            QueueType = QueueType.Private,
            MessageCount = 42,
            IsTransactional = true,
            CanRead = true,
            CanWrite = true
        };
    }

    /// <summary>
    /// Handles message selection from the list.
    /// </summary>
    /// <param name="message">The selected message.</param>
    protected void HandleMessageListSelection(QueueMessage? message)
    {
        SelectedMessageFromList = message;
        StateHasChanged();
    }

    /// <summary>
    /// Handles bulk message selection changes.
    /// </summary>
    /// <param name="selectedMessages">The list of selected messages.</param>
    protected void HandleBulkSelection(List<QueueMessage> selectedMessages)
    {
        BulkSelectedMessages = selectedMessages;
        StateHasChanged();
    }

    /// <summary>
    /// Handles message list refresh requests.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleMessageRefreshAsync()
    {
        IsRefreshingMessages = true;
        StateHasChanged();

        try
        {
            // Simulate refresh delay
            await Task.Delay(1500);

            // Update some message properties to simulate changes
            var random = new Random();
            foreach (var message in SampleMessages.Take(5))
            {
                // Randomly update arrived time for some messages
                if (random.Next(100) > 50)
                {
                    message.ArrivedTime = DateTime.Now.AddMinutes(-random.Next(1, 60));
                }
            }
        }
        finally
        {
            IsRefreshingMessages = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets sample messages organized by priority for demonstration.
    /// </summary>
    /// <returns>A list of messages with one of each priority.</returns>
    protected List<QueueMessage> GetSampleMessagesByPriority()
    {
        var priorities = Enum.GetValues<MessagePriority>();
        var result = new List<QueueMessage>();

        foreach (var priority in priorities)
        {
            var message = SampleMessages.FirstOrDefault(m => m.Priority == priority);
            if (message != null)
            {
                result.Add(message);
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if a demo message is checked.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>True if checked, false otherwise.</returns>
    protected bool IsDemoMessageChecked(string messageId)
    {
        return DemoCheckedMessageIds.Contains(messageId);
    }

    /// <summary>
    /// Handles demo row click.
    /// </summary>
    /// <param name="message">The clicked message.</param>
    protected void HandleDemoRowClick(QueueMessage message)
    {
        DemoSelectedMessageId = message.Id;
        DemoMessageInfo = $"Selected: {message.Label} (Priority: {message.PriorityText})";
        StateHasChanged();
    }

    /// <summary>
    /// Handles demo checkbox change.
    /// </summary>
    /// <param name="message">The message whose checkbox changed.</param>
    protected void HandleDemoCheckboxChanged(QueueMessage message)
    {
        if (DemoCheckedMessageIds.Contains(message.Id))
        {
            DemoCheckedMessageIds.Remove(message.Id);
        }
        else
        {
            DemoCheckedMessageIds.Add(message.Id);
        }

        DemoMessageInfo = $"Checked messages: {DemoCheckedMessageIds.Count}";
        StateHasChanged();
    }

    /// <summary>
    /// Gets the display text for the current theme.
    /// </summary>
    /// <returns>The theme display text.</returns>
    protected string GetCurrentThemeDisplay()
    {
        return ThemeService.CurrentTheme == ThemeMode.Dark ? "Dark Mode" : "Light Mode";
    }

    /// <summary>
    /// Gets the display text for dark mode status.
    /// </summary>
    /// <returns>The dark mode status text.</returns>
    protected string GetIsDarkModeDisplay()
    {
        return ThemeService.IsDarkMode ? "Yes" : "No";
    }

    /// <summary>
    /// Handles tree node click events.
    /// </summary>
    /// <param name="nodeData">The clicked node data.</param>
    protected void HandleNodeClick(TreeNodeData nodeData)
    {
        // Clear previous selection
        ClearSelection(SampleTreeData);

        // Set new selection
        nodeData.IsSelected = true;

        // Update info display
        var badgeInfo = nodeData.BadgeCount.HasValue
            ? $" ({nodeData.BadgeCount.Value:N0} messages)"
            : string.Empty;

        SelectedNodeInfo = $"{nodeData.Text}{badgeInfo}";

        StateHasChanged();
    }

    /// <summary>
    /// Handles tree node toggle events.
    /// </summary>
    /// <param name="nodeData">The toggled node data.</param>
    protected void HandleNodeToggle(TreeNodeData nodeData)
    {
        // The node's expanded state is already toggled by the TreeNode component
        // This handler is here for demonstration purposes
        StateHasChanged();
    }

    /// <summary>
    /// Recursively clears selection from all nodes.
    /// </summary>
    /// <param name="node">The node to process.</param>
    private void ClearSelection(TreeNodeData? node)
    {
        if (node == null)
        {
            return;
        }

        node.IsSelected = false;

        foreach (var child in node.Children)
        {
            ClearSelection(child);
        }
    }

    /// <summary>
    /// Handles theme change events.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Gets sample messages for the detail drawer demo.
    /// </summary>
    /// <returns>List of sample messages with different body formats.</returns>
    protected List<QueueMessage> GetDetailDemoMessages()
    {
        return new List<QueueMessage>
        {
            new QueueMessage
            {
                Id = Guid.NewGuid().ToString(),
                Label = "XML Customer Order",
                Priority = MessagePriority.High,
                ArrivedTime = DateTime.Now.AddMinutes(-15),
                SentTime = DateTime.Now.AddMinutes(-15),
                Body = new MessageBody("<Order><CustomerId>12345</CustomerId><Items><Item><ProductId>ABC123</ProductId><Quantity>5</Quantity><Price>29.99</Price></Item></Items><Total>149.95</Total></Order>")
                {
                    Format = MessageBodyFormat.Xml
                },
                CorrelationId = Guid.NewGuid().ToString(),
                ResponseQueue = @".\Private$\responses",
                SenderId = System.Text.Encoding.UTF8.GetBytes("System.OrderProcessor"),
                Recoverable = true,
                Authenticated = true,
                UseJournalQueue = true
            },
            new QueueMessage
            {
                Id = Guid.NewGuid().ToString(),
                Label = "JSON User Profile Update",
                Priority = MessagePriority.Normal,
                ArrivedTime = DateTime.Now.AddMinutes(-30),
                SentTime = DateTime.Now.AddMinutes(-30),
                Body = new MessageBody("{\"userId\":\"user-789\",\"profile\":{\"firstName\":\"Jane\",\"lastName\":\"Smith\",\"email\":\"jane.smith@example.com\",\"preferences\":{\"theme\":\"dark\",\"notifications\":true}},\"timestamp\":\"2025-10-19T10:30:00Z\"}")
                {
                    Format = MessageBodyFormat.Json
                },
                CorrelationId = Guid.NewGuid().ToString(),
                ResponseQueue = string.Empty,
                SenderId = System.Text.Encoding.UTF8.GetBytes("System.ProfileService"),
                Recoverable = true,
                Authenticated = false,
                UseJournalQueue = false
            },
            new QueueMessage
            {
                Id = Guid.NewGuid().ToString(),
                Label = "Plain Text Notification",
                Priority = MessagePriority.Low,
                ArrivedTime = DateTime.Now.AddHours(-2),
                SentTime = DateTime.Now.AddHours(-2),
                Body = new MessageBody("This is a plain text notification message.\nIt contains multiple lines of text.\nUse this format for simple text-based messages.\n\nBest regards,\nThe System")
                {
                    Format = MessageBodyFormat.Text
                },
                CorrelationId = string.Empty,
                ResponseQueue = string.Empty,
                SenderId = System.Text.Encoding.UTF8.GetBytes("System.NotificationService"),
                Recoverable = false,
                Authenticated = false,
                UseJournalQueue = false
            },
            new QueueMessage
            {
                Id = Guid.NewGuid().ToString(),
                Label = "Binary Data Package",
                Priority = MessagePriority.VeryHigh,
                ArrivedTime = DateTime.Now.AddMinutes(-5),
                SentTime = DateTime.Now.AddMinutes(-5),
                Body = new MessageBody("Binary content would be displayed as hexadecimal dump with ASCII representation on the right.")
                {
                    Format = MessageBodyFormat.Binary
                },
                CorrelationId = Guid.NewGuid().ToString(),
                ResponseQueue = @".\Private$\binary-responses",
                SenderId = System.Text.Encoding.UTF8.GetBytes("System.BinaryProcessor"),
                Recoverable = true,
                Authenticated = true,
                UseJournalQueue = true
            }
        };
    }

    /// <summary>
    /// Gets the priority badge class for detail demo messages.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The CSS class string.</returns>
    protected string GetDetailDemoPriorityBadge(QueueMessage message)
    {
        return message.Priority switch
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
    /// Opens the message detail drawer.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OpenMessageDetailAsync(QueueMessage message)
    {
        DetailDrawerMessage = message;
        IsDetailDrawerOpen = true;
        DetailDrawerOperationLog = string.Empty;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the detail drawer close event.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task HandleDetailDrawerCloseAsync()
    {
        IsDetailDrawerOpen = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the delete operation from the detail drawer.
    /// </summary>
    /// <param name="message">The message to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task HandleDetailDeleteAsync(QueueMessage message)
    {
        DetailDrawerOperationLog = $"Delete requested for message: {message.Label}";
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the move operation from the detail drawer.
    /// </summary>
    /// <param name="message">The message to move.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task HandleDetailMoveAsync(QueueMessage message)
    {
        DetailDrawerOperationLog = $"Move requested for message: {message.Label}";
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the export operation from the detail drawer.
    /// </summary>
    /// <param name="message">The message to export.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task HandleDetailExportAsync(QueueMessage message)
    {
        DetailDrawerOperationLog = $"Export requested for message: {message.Label}";
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the resend operation from the detail drawer.
    /// </summary>
    /// <param name="message">The message to resend.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task HandleDetailResendAsync(QueueMessage message)
    {
        DetailDrawerOperationLog = $"Resend requested for message: {message.Label}";
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens a confirmation dialog with the specified severity.
    /// </summary>
    /// <param name="severity">The dialog severity level.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OpenDialogAsync(DialogSeverity severity)
    {
        CurrentDialogSeverity = severity;
        DialogResultMessage = string.Empty;

        switch (severity)
        {
            case DialogSeverity.Info:
                DialogTitle = "Information";
                DialogMessage = "This is an informational dialog. It provides useful information to the user.";
                DialogConfirmText = "OK";
                DialogCancelText = "Cancel";
                break;

            case DialogSeverity.Warning:
                DialogTitle = "Warning";
                DialogMessage = "This action may have consequences. Are you sure you want to proceed?";
                DialogConfirmText = "Proceed";
                DialogCancelText = "Go Back";
                break;

            case DialogSeverity.Danger:
                DialogTitle = "Delete Item";
                DialogMessage = "This will permanently delete the item. This action cannot be undone.";
                DialogConfirmText = "Delete";
                DialogCancelText = "Cancel";
                break;

            case DialogSeverity.Success:
                DialogTitle = "Confirm Success";
                DialogMessage = "Everything looks good! Would you like to proceed with this action?";
                DialogConfirmText = "Continue";
                DialogCancelText = "Cancel";
                break;
        }

        DialogDetailMessage = null;
        DialogCustomContent = null;
        DialogShowCloseButton = true;
        DialogCloseOnBackdrop = false;
        IsDialogOpen = true;

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens a dialog with a detail message.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OpenDialogWithDetailsAsync()
    {
        CurrentDialogSeverity = DialogSeverity.Warning;
        DialogTitle = "Clear Cache";
        DialogMessage = "This will clear all cached data from the application.";
        DialogDetailMessage = "Clearing the cache may cause a temporary slowdown as data is reloaded. " +
                             "Your settings and preferences will not be affected. " +
                             "This operation typically takes 5-10 seconds to complete.";
        DialogCustomContent = null;
        DialogConfirmText = "Clear Cache";
        DialogCancelText = "Cancel";
        DialogShowCloseButton = true;
        DialogCloseOnBackdrop = false;
        DialogResultMessage = string.Empty;
        IsDialogOpen = true;

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens a dialog with custom content.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OpenDialogWithCustomContentAsync()
    {
        CurrentDialogSeverity = DialogSeverity.Info;
        DialogTitle = "Update Settings";
        DialogMessage = "You are about to change important application settings.";
        DialogCustomContent = "Custom content can include any HTML or Blazor components. " +
                             "This allows for rich, interactive dialogs with forms, lists, and more.";
        DialogDetailMessage = null;
        DialogConfirmText = "Update";
        DialogCancelText = "Cancel";
        DialogShowCloseButton = true;
        DialogCloseOnBackdrop = false;
        DialogResultMessage = string.Empty;
        IsDialogOpen = true;

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens a dialog that can be closed by clicking the backdrop.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OpenDialogWithBackdropCloseAsync()
    {
        CurrentDialogSeverity = DialogSeverity.Info;
        DialogTitle = "Quick Info";
        DialogMessage = "This dialog can be closed by clicking outside of it (on the backdrop).";
        DialogDetailMessage = null;
        DialogCustomContent = null;
        DialogConfirmText = "OK";
        DialogCancelText = "Cancel";
        DialogShowCloseButton = true;
        DialogCloseOnBackdrop = true;
        DialogResultMessage = string.Empty;
        IsDialogOpen = true;

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens a dialog that simulates an async operation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OpenDialogWithAsyncOperationAsync()
    {
        CurrentDialogSeverity = DialogSeverity.Warning;
        DialogTitle = "Process Data";
        DialogMessage = "This will start a background process that may take a few seconds.";
        DialogDetailMessage = "The dialog will show a loading indicator while the operation is in progress.";
        DialogCustomContent = null;
        DialogConfirmText = "Start Processing";
        DialogCancelText = "Cancel";
        DialogShowCloseButton = true;
        DialogCloseOnBackdrop = false;
        DialogResultMessage = string.Empty;
        IsDialogOpen = true;

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the dialog confirm event.
    /// </summary>
    /// <param name="result">The confirmation result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleDialogConfirmAsync(ConfirmationResult result)
    {
        // Simulate async operation for the async demo
        if (DialogTitle == "Process Data")
        {
            IsDialogProcessing = true;
            StateHasChanged();

            await Task.Delay(2000); // Simulate processing

            IsDialogProcessing = false;
        }

        DialogResultConfirmed = true;
        DialogResultMessage = $"Action confirmed at {result.Timestamp:HH:mm:ss}. " +
                             $"Dialog: '{DialogTitle}'";
        IsDialogOpen = false;
        StateHasChanged();
    }

    /// <summary>
    /// Handles the dialog cancel event.
    /// </summary>
    /// <param name="result">The confirmation result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task HandleDialogCancelAsync(ConfirmationResult result)
    {
        DialogResultConfirmed = false;
        DialogResultMessage = $"Action cancelled at {result.Timestamp:HH:mm:ss}. " +
                             $"Dialog: '{DialogTitle}'";
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the alert class for the dialog result.
    /// </summary>
    /// <returns>The CSS class string.</returns>
    protected string GetDialogResultAlertClass()
    {
        return DialogResultConfirmed ? "alert-success" : "alert-secondary";
    }

    /// <summary>
    /// Gets the icon class for the dialog result.
    /// </summary>
    /// <returns>The icon class string.</returns>
    protected string GetDialogResultIcon()
    {
        return DialogResultConfirmed ? "bi-check-circle-fill me-2" : "bi-x-circle-fill me-2";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources used by this component.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
        }

        _disposed = true;
    }
}
