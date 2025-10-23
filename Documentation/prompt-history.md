
### Prompt 1.1 - Base Project
```
Create or modify existing MsMqApp to ensure that the Blazor Server solution structure for the MSMQ Monitor application following our coding standards. Include:
- Main Blazor Server project
- Class library for services
- Class library for models/domain
- Test project
- Proper folder structure following the standards document
Include the .gitignore, .editorconfig, and basic Program.cs setup with DI configuration.

Some of these items may already be satisified
```

### Prompt 1.2 - Core Models
```
Create the core domain models for the MSMQ Monitor application:
- QueueInfo (represents a queue with path, name, message count, type)
- QueueMessage (represents a message with all MSMQ properties)
- QueueConnection (represents local or remote computer connection)
- MessageBody (handles different serialization formats)

Follow the coding standards - keep files under 250 lines, use proper naming conventions.
```

### Prompt 1.3 - Basic Service Interfaces
```
Create service interfaces for:
- IMsmqService (queue discovery, message retrieval)
- IMessageSerializer (deserialize messages to XML/JSON/Text/Binary)
- IQueueConnectionManager (manage local/remote connections)

Include XML documentation comments for all public APIs.
```

---

## **Phase 2: Core MSMQ Functionality**

### Prompt 2.1 - MSMQ Service Implementation
```
Implement the MsmqService class that implements IMsmqService. Focus on:
- Discovering queues on local machine
- Reading queue properties (message count, queue type)
- Retrieving messages without removing them (peek operation)
- Proper error handling and logging

Keep the implementation under 250 lines. Extract helper methods as needed.
```

### Prompt 2.2 - Message Serialization
```
Implement the MessageSerializer service that can:
- Auto-detect message format (XML, JSON, text, binary)
- Deserialize message bodies to appropriate format
- Handle errors gracefully for corrupted messages
- Return formatted, syntax-highlighted output

Create separate classes for each format handler to keep files small.
```

### Prompt 2.3 - Remote Connection Manager
```
Implement the QueueConnectionManager to:
- Connect to remote computers using Windows Authentication
- Validate connections
- Cache connection information
- Handle connection errors and timeouts

Include proper disposal patterns.
```

---

## **Phase 3: Basic UI Components (Atomic Level)**

### Prompt 3.1 - Loading Spinner Component
```
Create a reusable LoadingSpinner component following our standards:
- LoadingSpinner.razor (markup only)
- LoadingSpinner.razor.cs (code-behind)
- LoadingSpinner.razor.css (isolated styles)

Parameters: Size, Message (optional). Keep under 150 lines total.
```

### Prompt 3.2 - Error Display Component
```
Create an ErrorDisplay component for showing error messages:
- Separate code-behind
- Parameters for Message, Title, Type (error/warning/info)
- Dismissible option
- Clean, user-friendly styling
```

### Prompt 3.3 - Element Styling Capabilityies for Dark Mode vs Light Mode
```
Using bootstrap extend the styling method that may or may not already exists
- Theme capability to allow user to toggle Dark Mode vs Light Mode within the application
- Should support Status indicator that follows bootstrap color scheme;
- Success, Primary, Danger, Warning, Info or match the standard requirement

All with code-behind, proper event handling, loading states, and disabled states.
```

---

## **Phase 4: Queue Tree View Component**

### Prompt 4.1 - Tree Node Component
```
Create a TreeNode component for the queue tree view:
- Displays queue name and message count badge
- Expand/collapse functionality
- Selection state
- Code-behind separation
- Keep under 150 lines for razor file, 200 for code-behind
- Use our theme for styling and new theme features if required
```

### Prompt 4.2 - Queue Tree View Component
```
Create the QueueTreeView component that:
- Uses TreeNode components
- Organizes queues hierarchically (Application/System/Journal)
- Handles queue selection events
- Shows connection status
- Implements proper state management
- Use our theme for styling and new theme features if required
- Implement a sample usage of this component in our component-demo

Use composition - break into smaller components if needed.
```

---

## **Phase 5: Message List Component**

### Prompt 5.1 - Message List Grid
```
Create the MessageList component with:
- Data grid showing messages (use virtualization for performance)
- Column sorting
- Row selection
- Search box integration
- Refresh controls (button, pause, countdown timer)
- Use our theme for styling and new theme features if required
- Implement a sample usage of this component in our component-demo

Separate code-behind. Keep razor under 150 lines.
```

### Prompt 5.2 - Message Row Component
```
Create MessageRow component for displaying individual messages in the grid:
- Shows key properties (Label, Priority, Time, ID)
- Click handling for selection
- Visual indication of selected state
- Proper formatting of dates and sizes
- Use our theme for styling and new theme features if required
- Augment our MessageList grid component found in our component-demo that is used for testing
```

---

## **Phase 6: Message Detail Panel**

### Prompt 6.1 - Message Detail Drawer
```
Create the MessageDetail drawer component that:
- Slides in from right
- Displays all message properties
- Shows formatted message body
- Includes operation buttons (Delete, Move, Export, Resend)
- Code-behind separation with proper lifecycle management
- Use our theme for styling and new theme features if required
- Implement a sample usage of this component in our component-demo
```

### Prompt 6.2 - Message Body Viewer
```
Create MessageBodyViewer component with:
- Format selector dropdown (Auto, XML, JSON, Text, Binary)
- Syntax highlighting for XML/JSON
- Copy to clipboard functionality
- Proper handling of large message bodies
- Scrollable container
- Use our theme for styling and new theme features if required
- Look for any pre-existing work down to ensure there is no-overlap or duplicated functionality
- Implement a sample usage of this component in our component-demo
```

---

## **Phase 7: Message Operations**

### Prompt 7.1 - Confirmation Dialog Component
```
Create a reusable ConfirmationDialog component for destructive operations:
- Title and message parameters
- Confirm/Cancel buttons
- EventCallback for result
- Different severity levels (warning, danger)
- Use our theme for styling and new theme features if required
- Implement a sample usage of this component in our component-demo
```

### Prompt 7.2 - Message Operations Service
```
Implement IMessageOperationsService with methods for:
- DeleteMessage
- MoveMessage
- ResendMessage
- ExportMessage
- PurgeQueue

Proper error handling, logging, and validation.
```

---

## **Phase 8: Main Layout & Integration**

### Prompt 8.1 - Main Layout Component
```
Create the MainLayout component that:
- Implements the two-panel content verticle layout 
- Hamburger Menu Bar in Upper Left Corner to toggle the menu, menu in collapsed mode will show icons.  Fully expanded menu will then show icons and menu text
- Full menu will collapse to regular icons upon toggling of the Hamburger Menu
- Default to dark mode
- Resizable panels
- Responsive to window size
- Proper state management across panels
- Keep well-organized and under size limits
```

### **Prompt 8.3 - Journal Queue Display** ⭐ NEW
```
Implement journal queue viewing functionality to match MSMQ snap-in behavior:

1. Update the QueueTreeView component to show queues as expandable folders with two child nodes:
   - "Queue Messages" - displays messages currently in the queue
   - "Journal Messages" - displays messages from the journal queue

2. Update the QueueInfo model to include:
   - HasJournaling property (bool)
   - JournalPath property (string)
   - JournalMessageCount property (int)

3. Update IMsmqService interface to add:
   - Task<IEnumerable<QueueMessage>> GetJournalMessagesAsync(string queuePath)
   - Task<int> GetJournalMessageCountAsync(string queuePath)

4. Implement the journal message retrieval in MsmqService:
   - Access the journal queue path (typically queuePath + "\journal$")
   - Retrieve journal messages with same properties as regular messages
   - Handle cases where journaling is not enabled

5. Update TreeNode component to support:
   - Nested child nodes (queue -> queue messages / journal messages)
   - Different icons for queue types (folder icon for queue, message icon for messages, journal icon for journal)
   - Proper indentation for hierarchy
   - Selection state for both parent queue and child nodes

6. Update MessageList component to:
   - Accept a parameter indicating if showing journal vs queue messages
   - Display appropriate header ("Queue Messages" or "Journal Messages")
   - Handle empty states differently for journal queues

Visual hierarchy should look like:
```
📁 Private Queues
  📁 dink_q                    [10] [10]
    ├─ 📬 Queue Messages       [10]
    └─ 📋 Journal Messages     [10]
  📁 sample_q                  [2]
    ├─ 📬 Queue Messages       [2]
    └─ 📋 Journal Messages     [0]
```

The badges show message counts for: [Queue Count] [Journal Count] at queue level, 
and [Count] at the child level.

Follow all coding standards:
- Separate code-behind files
- Keep components under size limits
- Proper error handling for queues without journaling enabled
- XML documentation for new methods


## **Phase 9: Windows Service**

### Prompt 9.1 - Windows Service Configuration
```
Configure the Blazor app to run as a Windows service:
- Update Program.cs with Windows Service hosting
- Configure Kestrel to use configurable port (default 8080)
- Set up logging to Windows Event Log
- Implement graceful shutdown
```

### Prompt 9.2 - Service Installer
```
Create installation scripts and documentation for:
- Installing the Windows service
- Configuring appsettings.json
- Starting/stopping the service
- Uninstalling the service
- Troubleshooting common issues
```

---

## **Phase 10: Testing & Polish**

### Prompt 10.1 - Unit Tests
```
Create unit tests for:
- MsmqService methods
- MessageSerializer logic
- QueueConnectionManager
- Message operations

Use xUnit and Moq. Follow AAA pattern.
```

### Prompt 10.2 - Component Tests
```
Create bUnit tests for key components:
- QueueTreeView selection behavior
- MessageList filtering and sorting
- ConfirmationDialog user interactions