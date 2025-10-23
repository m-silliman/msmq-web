# MSMQ Journal Testing Scripts

This folder contains PowerShell scripts to help test the journal queue functionality in your MSMQ Manager application.

## Scripts Overview

### 1. `Manage-TestQueues.ps1`
Creates and manages test queues with journaling capabilities.

### 2. `Dequeue-Messages.ps1`
Dequeues messages from queues to move them to journal queues.

## Quick Start Guide

### Step 1: Create a Test Queue with Journaling
```powershell
.\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Create
```

### Step 2: Send Test Messages
```powershell
.\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Send -MessageCount 15
```

### Step 3: Check Queue Status
```powershell
.\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Status
```

### Step 4: Dequeue Messages to Journal
```powershell
# Preview what will be dequeued (safe)
.\Dequeue-Messages.ps1 -QueuePath ".\private$\test_queue" -MessageCount 10 -WhatIf

# Actually dequeue 10 messages
.\Dequeue-Messages.ps1 -QueuePath ".\private$\test_queue" -MessageCount 10

# Dequeue all messages
.\Dequeue-Messages.ps1 -QueuePath ".\private$\test_queue"
```

### Step 5: Check Results
```powershell
.\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Status
```

## Common Use Cases

### Create Multiple Test Queues
```powershell
# Create queues for different test scenarios
.\Manage-TestQueues.ps1 -QueuePath ".\private$\dink_q" -Action Create
.\Manage-TestQueues.ps1 -QueuePath ".\private$\sample_q" -Action Create
.\Manage-TestQueues.ps1 -QueuePath ".\private$\journal_test" -Action Create

# Add different amounts of messages to each
.\Manage-TestQueues.ps1 -QueuePath ".\private$\dink_q" -Action Send -MessageCount 10
.\Manage-TestQueues.ps1 -QueuePath ".\private$\sample_q" -Action Send -MessageCount 5
.\Manage-TestQueues.ps1 -QueuePath ".\private$\journal_test" -Action Send -MessageCount 20
```

### Test Journal Functionality
```powershell
# Dequeue some messages from each queue to create journal entries
.\Dequeue-Messages.ps1 -QueuePath ".\private$\dink_q" -MessageCount 5
.\Dequeue-Messages.ps1 -QueuePath ".\private$\sample_q" -MessageCount 2
.\Dequeue-Messages.ps1 -QueuePath ".\private$\journal_test" -MessageCount 15

# Check final state
.\Manage-TestQueues.ps1 -QueuePath ".\private$\dink_q" -Action Status
.\Manage-TestQueues.ps1 -QueuePath ".\private$\sample_q" -Action Status
.\Manage-TestQueues.ps1 -QueuePath ".\private$\journal_test" -Action Status
```

### Enable Journaling on Existing Queues
```powershell
# If you have existing queues without journaling
.\Manage-TestQueues.ps1 -QueuePath ".\private$\existing_queue" -Action EnableJournaling

# Then dequeue messages to populate journal
.\Dequeue-Messages.ps1 -QueuePath ".\private$\existing_queue" -EnableJournaling
```

## Expected Results

After running these scripts, you should see:

1. **Queue Messages**: Remaining messages in the main queue
2. **Journal Messages**: Dequeued messages in the journal queue (path + ";journal")

Example final state:
```
📁 Private Queues
  📁 dink_q                    [5] [5]    # 5 queue messages, 5 journal messages
    ├─ 📬 Queue Messages       [5]
    └─ 📋 Journal Messages     [5]
  📁 sample_q                  [3] [2]    # 3 queue messages, 2 journal messages
    ├─ 📬 Queue Messages       [3]
    └─ 📋 Journal Messages     [2]
```

## Troubleshooting

### No Journal Messages Appearing
1. Verify journaling is enabled: `.\Manage-TestQueues.ps1 -QueuePath "your_queue" -Action Status`
2. Enable journaling: `.\Manage-TestQueues.ps1 -QueuePath "your_queue" -Action EnableJournaling`
3. Dequeue with journaling flag: `.\Dequeue-Messages.ps1 -QueuePath "your_queue" -EnableJournaling`

### Permission Issues
- Run PowerShell as Administrator
- Ensure MSMQ service is running: `Get-Service MSMQ`

### Queue Not Found
- Use full path format: `".\private$\queue_name"`
- Verify queue exists: `.\Manage-TestQueues.ps1 -QueuePath "your_queue" -Action Status`

## Clean Up

To remove test queues when done:
```powershell
.\Manage-TestQueues.ps1 -QueuePath ".\private$\test_queue" -Action Delete
.\Manage-TestQueues.ps1 -QueuePath ".\private$\dink_q" -Action Delete
.\Manage-TestQueues.ps1 -QueuePath ".\private$\sample_q" -Action Delete
```

## Script Parameters

### Manage-TestQueues.ps1
- `QueuePath`: Full MSMQ queue path
- `Action`: Create, Send, Status, EnableJournaling, DisableJournaling, Delete
- `MessageCount`: Number of messages to send (default: 10)
- `MessageContent`: Custom message content

### Dequeue-Messages.ps1
- `QueuePath`: Full MSMQ queue path
- `MessageCount`: Number to dequeue (0 = all, default: 0)
- `EnableJournaling`: Enable journaling before dequeuing
- `WhatIf`: Preview mode (no actual changes)