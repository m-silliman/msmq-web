#Requires -Version 5.1

<#
.SYNOPSIS
    Manages MSMQ queues and creates test messages for journal testing.

.DESCRIPTION
    This script helps set up MSMQ queues for testing journal functionality by:
    - Creating queues with journaling enabled
    - Sending test messages to queues
    - Checking queue and journal status
    - Enabling/disabling journaling on existing queues

.PARAMETER QueuePath
    The full path to the MSMQ queue (e.g., ".\private$\test_queue")

.PARAMETER Action
    Action to perform: Create, Send, Status, EnableJournaling, DisableJournaling

.PARAMETER MessageCount
    Number of test messages to send (default: 10)

.PARAMETER MessageContent
    Custom content for test messages (default: auto-generated)

.EXAMPLE
    .\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Create
    Creates a new queue with journaling enabled

.EXAMPLE
    .\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Send -MessageCount 20
    Sends 20 test messages to the queue

.EXAMPLE
    .\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Status
    Shows status of queue and journal
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$QueuePath,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("Create", "Send", "Status", "EnableJournaling", "DisableJournaling", "Delete")]
    [string]$Action,
    
    [Parameter()]
    [int]$MessageCount = 10,
    
    [Parameter()]
    [string]$MessageContent = ""
)

# Add MSMQ assemblies
Add-Type -AssemblyName "System.Messaging"

function Write-Log {
    param([string]$Message, [string]$Level = "Info")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "Error" { "Red" }
        "Warning" { "Yellow" }
        "Success" { "Green" }
        "Info" { "Cyan" }
        default { "White" }
    }
    Write-Host "[$timestamp] $Message" -ForegroundColor $color
}

function Test-QueueExists {
    param([string]$Path)
    try {
        return [System.Messaging.MessageQueue]::Exists($Path)
    }
    catch {
        return $false
    }
}

function Create-Queue {
    param([string]$Path)
    
    try {
        if (Test-QueueExists $Path) {
            Write-Log "Queue already exists: $Path" -Level "Warning"
            return $false
        }
        
        Write-Log "Creating queue: $Path"
        $queue = [System.Messaging.MessageQueue]::Create($Path)
        
        # Enable journaling
        $queue.UseJournalQueue = $true
        $queue.Label = "Test Queue - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        
        Write-Log "Queue created successfully with journaling enabled" -Level "Success"
        $queue.Close()
        return $true
    }
    catch {
        Write-Log "Error creating queue: $($_.Exception.Message)" -Level "Error"
        return $false
    }
}

function Delete-Queue {
    param([string]$Path)
    
    try {
        if (-not (Test-QueueExists $Path)) {
            Write-Log "Queue does not exist: $Path" -Level "Warning"
            return $false
        }
        
        Write-Log "Deleting queue: $Path"
        [System.Messaging.MessageQueue]::Delete($Path)
        Write-Log "Queue deleted successfully" -Level "Success"
        return $true
    }
    catch {
        Write-Log "Error deleting queue: $($_.Exception.Message)" -Level "Error"
        return $false
    }
}

function Send-TestMessages {
    param([string]$Path, [int]$Count, [string]$Content)
    
    try {
        if (-not (Test-QueueExists $Path)) {
            Write-Log "Queue does not exist: $Path" -Level "Error"
            return 0
        }
        
        $queue = New-Object System.Messaging.MessageQueue($Path)
        $sent = 0
        
        Write-Log "Sending $Count test messages to queue..."
        
        for ($i = 1; $i -le $Count; $i++) {
            try {
                $message = New-Object System.Messaging.Message
                
                # Set message properties
                $message.Label = "Test Message $i"
                $message.Priority = [System.Messaging.MessagePriority]::Normal
                
                # Set message body
                if ([string]::IsNullOrEmpty($Content)) {
                    $testData = @{
                        MessageNumber = $i
                        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
                        TestData = "This is test message number $i of $Count"
                        RandomGuid = [System.Guid]::NewGuid().ToString()
                        Priority = "Normal"
                    }
                    $message.Body = ($testData | ConvertTo-Json -Depth 2)
                }
                else {
                    $message.Body = "$Content - Message $i"
                }
                
                # Send the message
                $queue.Send($message)
                $sent++
                
                if ($i % 5 -eq 0 -or $i -eq $Count) {
                    Write-Log "Sent $i/$Count messages..." -Level "Info"
                }
                
                $message.Dispose()
            }
            catch {
                Write-Log "Error sending message $i: $($_.Exception.Message)" -Level "Error"
            }
        }
        
        $queue.Close()
        Write-Log "Successfully sent $sent messages" -Level "Success"
        return $sent
    }
    catch {
        Write-Log "Error sending messages: $($_.Exception.Message)" -Level "Error"
        return 0
    }
}

function Get-QueueStatus {
    param([string]$Path)
    
    try {
        if (-not (Test-QueueExists $Path)) {
            Write-Log "Queue does not exist: $Path" -Level "Error"
            return
        }
        
        $queue = New-Object System.Messaging.MessageQueue($Path)
        $queue.MessageReadPropertyFilter.SetAll()
        
        # Get message count
        $messageCount = 0
        try {
            $enumerator = $queue.GetMessageEnumerator2()
            while ($enumerator.MoveNext()) {
                $messageCount++
            }
        }
        catch {
            Write-Log "Could not enumerate messages: $($_.Exception.Message)" -Level "Warning"
        }
        
        # Get journal status
        $journalPath = "$Path\\journal$"
        $journalCount = 0
        $journalExists = $false
        
        try {
            if (Test-QueueExists $journalPath) {
                $journalExists = $true
                $journalQueue = New-Object System.Messaging.MessageQueue($journalPath)
                $journalQueue.MessageReadPropertyFilter.SetAll()
                
                $journalEnumerator = $journalQueue.GetMessageEnumerator2()
                while ($journalEnumerator.MoveNext()) {
                    $journalCount++
                }
                $journalQueue.Close()
            }
        }
        catch {
            Write-Log "Could not check journal queue: $($_.Exception.Message)" -Level "Warning"
        }
        
        # Display status
        Write-Log "=== QUEUE STATUS ===" -Level "Success"
        Write-Log "Queue Path: $($queue.Path)"
        Write-Log "Queue Name: $($queue.QueueName)"
        Write-Log "Label: $($queue.Label)"
        Write-Log "Message Count: $messageCount"
        Write-Log "Journaling Enabled: $($queue.UseJournalQueue)"
        Write-Log "Journal Queue Exists: $journalExists"
        Write-Log "Journal Message Count: $journalCount"
        Write-Log "Created: $(try { $queue.CreateTime } catch { 'Unknown' })"
        Write-Log "Last Modified: $(try { $queue.LastModifyTime } catch { 'Unknown' })"
        
        $queue.Close()
    }
    catch {
        Write-Log "Error getting queue status: $($_.Exception.Message)" -Level "Error"
    }
}

function Set-QueueJournaling {
    param([string]$Path, [bool]$Enable)
    
    try {
        if (-not (Test-QueueExists $Path)) {
            Write-Log "Queue does not exist: $Path" -Level "Error"
            return $false
        }
        
        $queue = New-Object System.Messaging.MessageQueue($Path)
        
        $action = if ($Enable) { "Enabling" } else { "Disabling" }
        Write-Log "$action journaling for queue: $Path"
        
        $queue.UseJournalQueue = $Enable
        
        $status = if ($Enable) { "enabled" } else { "disabled" }
        Write-Log "Journaling $status successfully" -Level "Success"
        
        $queue.Close()
        return $true
    }
    catch {
        Write-Log "Error setting journaling: $($_.Exception.Message)" -Level "Error"
        return $false
    }
}

# Main execution
try {
    Write-Log "=== MSMQ Test Queue Manager ===" -Level "Success"
    Write-Log "Action: $Action"
    Write-Log "Queue Path: $QueuePath"
    Write-Log ""
    
    switch ($Action) {
        "Create" {
            Create-Queue $QueuePath
        }
        
        "Delete" {
            $confirmation = Read-Host "Are you sure you want to delete queue '$QueuePath'? (y/N)"
            if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
                Delete-Queue $QueuePath
            }
            else {
                Write-Log "Delete operation cancelled"
            }
        }
        
        "Send" {
            $sent = Send-TestMessages $QueuePath $MessageCount $MessageContent
            Write-Log "Operation completed. Messages sent: $sent"
        }
        
        "Status" {
            Get-QueueStatus $QueuePath
        }
        
        "EnableJournaling" {
            Set-QueueJournaling $QueuePath $true
        }
        
        "DisableJournaling" {
            Set-QueueJournaling $QueuePath $false
        }
    }
    
    Write-Log ""
    Write-Log "=== OPERATION COMPLETED ===" -Level "Success"
}
catch {
    Write-Log "Script execution failed: $($_.Exception.Message)" -Level "Error"
    Write-Log "Stack trace: $($_.ScriptStackTrace)" -Level "Error"
    exit 1
}