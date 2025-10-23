#Requires -Version 5.1

<#
.SYNOPSIS
    Dequeues messages from MSMQ queues to move them to journal queues.

.DESCRIPTION
    This script removes messages from specified MSMQ queues, which will cause them
    to appear in the journal queue if journaling is enabled. Useful for testing
    journal queue functionality.

.PARAMETER QueuePath
    The full path to the MSMQ queue (e.g., ".\private$\test_queue")

.PARAMETER MessageCount
    Number of messages to dequeue. If not specified, all messages will be dequeued.

.PARAMETER WhatIf
    Shows what would happen without actually dequeuing messages.

.PARAMETER EnableJournaling
    Enables journaling on the queue before dequeuing (if not already enabled).

.EXAMPLE
    .\Dequeue-Messages.ps1 -QueuePath ".\private$\test_queue" -MessageCount 5
    Dequeues 5 messages from the test_queue

.EXAMPLE
    .\Dequeue-Messages.ps1 -QueuePath ".\private$\test_queue" -EnableJournaling
    Enables journaling and dequeues all messages from test_queue

.EXAMPLE
    .\Dequeue-Messages.ps1 -QueuePath ".\private$\test_queue" -WhatIf
    Shows what messages would be dequeued without actually removing them
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$QueuePath,
    
    [Parameter()]
    [int]$MessageCount = 0,
    
    [Parameter()]
    [switch]$EnableJournaling
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
        Write-Log "Error checking if queue exists: $($_.Exception.Message)" -Level "Error"
        return $false
    }
}

function Get-QueueMessageCount {
    param([System.Messaging.MessageQueue]$Queue)
    
    try {
        $count = 0
        $enumerator = $Queue.GetMessageEnumerator2()
        while ($enumerator.MoveNext()) {
            $count++
        }
        return $count
    }
    catch {
        Write-Log "Error counting messages: $($_.Exception.Message)" -Level "Warning"
        return 0
    }
}

function Enable-QueueJournaling {
    param([System.Messaging.MessageQueue]$Queue)
    
    try {
        if (-not $Queue.UseJournalQueue) {
            Write-Log "Enabling journaling for queue: $($Queue.Path)"
            $Queue.UseJournalQueue = $true
            Write-Log "Journaling enabled successfully" -Level "Success"
            return $true
        }
        else {
            Write-Log "Journaling is already enabled for this queue" -Level "Success"
            return $true
        }
    }
    catch {
        Write-Log "Error enabling journaling: $($_.Exception.Message)" -Level "Error"
        return $false
    }
}

function Get-MessagePreview {
    param([System.Messaging.Message]$Message)
    
    $preview = @{
        Id = $Message.Id
        Label = $Message.Label
        Priority = $Message.Priority
        SentTime = $Message.SentTime
        BodyLength = 0
    }
    
    try {
        if ($Message.Body) {
            $bodyStr = $Message.Body.ToString()
            $preview.BodyLength = $bodyStr.Length
            $preview.BodyPreview = if ($bodyStr.Length -gt 50) { 
                $bodyStr.Substring(0, 50) + "..." 
            } else { 
                $bodyStr 
            }
        }
    }
    catch {
        $preview.BodyPreview = "[Unable to read body]"
    }
    
    return $preview
}

function Dequeue-Messages {
    param(
        [System.Messaging.MessageQueue]$Queue,
        [int]$Count
    )
    
    $dequeued = 0
    $errors = 0
    
    try {
        Write-Log "Starting message dequeue operation..."
        
        while ($dequeued -lt $Count -or $Count -eq 0) {
            try {
                # Set a timeout to avoid hanging
                $timeout = [TimeSpan]::FromSeconds(1)
                
                if ($WhatIfPreference) {
                    # In WhatIf mode, just peek at the message
                    $message = $Queue.Peek($timeout)
                } else {
                    # Actually receive (dequeue) the message
                    $message = $Queue.Receive($timeout)
                }
                
                if ($message) {
                    $preview = Get-MessagePreview $message
                    
                    if ($WhatIfPreference) {
                        Write-Log "WOULD DEQUEUE: ID=$($preview.Id), Label='$($preview.Label)', Priority=$($preview.Priority), Sent=$($preview.SentTime)"
                    }
                    else {
                        Write-Log "DEQUEUED: ID=$($preview.Id), Label='$($preview.Label)', Priority=$($preview.Priority), Sent=$($preview.SentTime)" -Level "Success"
                    }
                    
                    $dequeued++
                    
                    # Dispose the message
                    $message.Dispose()
                    
                    # In WhatIf mode, break after showing the first message to avoid infinite loop
                    if ($WhatIfPreference -and $Count -eq 0) {
                        Write-Log "... (would continue for all remaining messages)"
                        break
                    }
                }
            }
            catch [System.Messaging.MessageQueueException] {
                # No more messages available
                if ($_.Exception.MessageQueueErrorCode -eq "IOTimeout") {
                    Write-Log "No more messages available in queue"
                    break
                }
                else {
                    Write-Log "MessageQueue error: $($_.Exception.Message)" -Level "Error"
                    $errors++
                    if ($errors -gt 5) {
                        Write-Log "Too many errors, stopping operation" -Level "Error"
                        break
                    }
                }
            }
            catch {
                Write-Log "Unexpected error dequeuing message: $($_.Exception.Message)" -Level "Error"
                $errors++
                if ($errors -gt 5) {
                    Write-Log "Too many errors, stopping operation" -Level "Error"
                    break
                }
            }
        }
    }
    finally {
        Write-Log "Dequeue operation completed. Messages processed: $dequeued"
        if ($errors -gt 0) {
            Write-Log "Errors encountered: $errors" -Level "Warning"
        }
    }
    
    return $dequeued
}

# Main execution
try {
    Write-Log "=== MSMQ Message Dequeue Script ===" -Level "Success"
    Write-Log "Queue Path: $QueuePath"
    Write-Log "Message Count: $(if ($MessageCount -eq 0) { 'All' } else { $MessageCount })"
    Write-Log "What-If Mode: $WhatIfPreference"
    Write-Log "Enable Journaling: $EnableJournaling"
    Write-Log ""
    
    # Check if queue exists
    if (-not (Test-QueueExists $QueuePath)) {
        Write-Log "Queue does not exist: $QueuePath" -Level "Error"
        exit 1
    }
    
    # Open the queue
    $queue = New-Object System.Messaging.MessageQueue($QueuePath)
    $queue.MessageReadPropertyFilter.SetAll()
    
    Write-Log "Successfully connected to queue: $($queue.QueueName)" -Level "Success"
    
    # Check current message count
    $currentCount = Get-QueueMessageCount $queue
    Write-Log "Current message count in queue: $currentCount"
    
    if ($currentCount -eq 0) {
        Write-Log "No messages in queue to dequeue" -Level "Warning"
        exit 0
    }
    
    # Check journaling status
    Write-Log "Current journaling status: $($queue.UseJournalQueue)" 
    
    # Enable journaling if requested
    if ($EnableJournaling) {
        if (-not (Enable-QueueJournaling $queue)) {
            Write-Log "Failed to enable journaling, continuing anyway..." -Level "Warning"
        }
    }
    
    # Determine how many messages to dequeue
    $targetCount = if ($MessageCount -eq 0) { $currentCount } else { [Math]::Min($MessageCount, $currentCount) }
    
    Write-Log "Will dequeue $targetCount messages"
    Write-Log ""
    
    if ($WhatIfPreference) {
        Write-Log "=== WHAT-IF MODE - NO ACTUAL CHANGES WILL BE MADE ===" -Level "Warning"
    }
    
    # Confirm operation unless in WhatIf mode
    if (-not $WhatIfPreference) {
        $confirmation = Read-Host "Do you want to proceed with dequeuing $targetCount messages? (y/N)"
        if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
            Write-Log "Operation cancelled by user"
            exit 0
        }
    }
    
    Write-Log ""
    
    # Dequeue messages
    $processed = Dequeue-Messages $queue $targetCount
    
    Write-Log ""
    Write-Log "=== OPERATION SUMMARY ===" -Level "Success"
    Write-Log "Messages processed: $processed"
    
    if (-not $WhatIfPreference) {
        # Check final counts
        $finalCount = Get-QueueMessageCount $queue
        Write-Log "Messages remaining in queue: $finalCount"
        
        if ($queue.UseJournalQueue) {
            Write-Log "Messages should now be available in journal queue: $($QueuePath);journal$"
        }
        else {
            Write-Log "Note: Journaling is not enabled - messages were permanently deleted" -Level "Warning"
        }
    }
}
catch {
    Write-Log "Script execution failed: $($_.Exception.Message)" -Level "Error"
    Write-Log "Stack trace: $($_.ScriptStackTrace)" -Level "Error"
    exit 1
}
finally {
    # Clean up
    if ($queue) {
        $queue.Close()
        $queue.Dispose()
    }
}