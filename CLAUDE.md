# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Blazor Server (.NET 9.0)** web application designed to be deployed as a Windows Service. It provides a centralized web-based interface for monitoring and managing Microsoft Message Queue (MSMQ) queues across local and remote computers.

**Current Status:** Early scaffolding phase - base Blazor template is in place, but core MSMQ integration has not yet been implemented.

## Common Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run in development (HTTP only)
dotnet run --launch-profile http

# Run with HTTPS
dotnet run --launch-profile https

# Release build
dotnet build -c Release

# Publish as self-contained Windows service
dotnet publish -c Release -r win-x64 --self-contained
```

### Testing
Currently no test projects exist. When tests are added:
```bash
dotnet test
dotnet test --no-build --verbosity normal
```

## Critical Coding Standards

This project has **mandatory** coding standards documented in `Documentation/coding-standards.md` (4,254 lines). Key requirements:

### Code-Behind Pattern (MANDATORY)
- **All Blazor components MUST use code-behind separation**
- `.razor` files contain markup ONLY
- `.razor.cs` files contain all logic, state, and event handlers
- Components without code-behind will be rejected in code review

Example structure:
```
MyComponent.razor      # Markup only
MyComponent.razor.cs   # All C# logic
```

### File Size Limits
- Razor files: Max 300 lines
- Code-behind files: Max 500 lines
- Service classes: Max 400 lines
- Enforce by splitting components/services when limits are approached

### Async Patterns
- All I/O operations MUST be async
- All async methods MUST have `Async` suffix
- Never use `async void` except for event handlers

### Dependency Injection
- Use constructor injection for all dependencies
- Register services in `Program.cs`
- Prefer interface-based abstractions

## Architecture

### Three-Panel UI Layout (from PRD)
The application uses a 3-panel architecture:
1. **Left Panel:** Queue tree view (local/remote computers, application/system queues, journal queues)
2. **Middle Panel:** Message list (data grid with virtualization)
3. **Right Panel:** Message detail drawer (expandable/collapsible)

### Project Structure
```
MsMqApp/
├── Components/
│   ├── Layout/          # MainLayout.razor, NavMenu.razor
│   ├── Pages/           # Routable page components
│   ├── Shared/          # Reusable components (to be created)
│   └── Features/        # Feature-specific components (to be created)
├── Services/            # Business logic services (to be created)
├── Models/              # DTOs and domain models (to be created)
├── wwwroot/             # Static assets
└── Program.cs           # Application entry point
```

### Expected Service Layer (Not Yet Implemented)
When implementing MSMQ functionality, create these services:
- `IMsmqService` - Core MSMQ operations wrapper
- `IQueueDiscoveryService` - Queue enumeration and discovery
- `IMessageService` - Message retrieval and operations
- `IRemoteConnectionService` - Remote computer connection management

### Data Models to Create
Based on PRD requirements:
- `QueueDto` - Queue metadata (name, path, message count, type)
- `MessageDto` - Message data (ID, label, body, timestamp, priority)
- `RemoteComputerDto` - Remote connection details
- `ExportFormat` enum - Export format options (JSON, XML, CSV, Text)

## Key Architectural Decisions

### Windows Service Deployment
The application will be deployed as a self-contained Windows Service:
- Configure in `Program.cs` using `Microsoft.Extensions.Hosting.WindowsServices`
- Default port: 8080 (configurable via appsettings.json)
- Auto-start on Windows boot
- Windows Authentication required

### MSMQ Integration
Use `System.Messaging` namespace for MSMQ operations:
- MessageQueue class for queue access
- Message class for message operations
- Requires Windows OS with MSMQ feature enabled
- Administrator privileges needed for installation

### Real-Time Updates
Implement auto-refresh mechanism:
- Default interval: 5 seconds (configurable)
- Pause/resume controls in UI
- Use Blazor's `StateHasChanged()` for UI updates
- Consider SignalR for future multi-user scenarios

## Configuration

### appsettings.json Structure
Expected configuration (to be implemented):
```json
{
  "Service": {
    "Port": 8080,
    "ServiceName": "MSMQMonitor",
    "DisplayName": "MSMQ Monitor & Management Tool",
    "AutoStart": true
  },
  "Application": {
    "DefaultRefreshIntervalSeconds": 5,
    "MaxMessageBodySizeBytes": 1048576,
    "MessageListPageSize": 100,
    "RemoteConnectionTimeoutSeconds": 30
  }
}
```

## Important Documentation

### Product Requirements Document
`Documentation/prd.md` - Comprehensive 880-line PRD covering:
- All functional requirements (FR-001 through FR-038)
- UI specifications and layout
- Non-functional requirements
- Acceptance criteria
- Installation and deployment process

**Always reference the PRD when implementing new features.**

### Coding Standards
`Documentation/coding-standards.md` - Comprehensive 4,254-line standards document covering:
- Mandatory code-behind pattern
- File size limits and enforcement
- C# 11+ language features to use
- Razor markup standards
- State management patterns
- Performance optimization guidelines
- Testing standards (AAA pattern, bUnit for components)
- Code review checklist

**All code must comply with these standards.**

## Development Environment

### Prerequisites
- .NET 9.0 SDK or later
- Windows OS with MSMQ feature installed
- Visual Studio 2022 or VS Code with C# extension
- Administrator privileges for service installation

### MSMQ Setup
Enable MSMQ on Windows:
1. Control Panel → Programs → Turn Windows features on or off
2. Enable: Microsoft Message Queue (MSMQ) Server
3. Enable: MSMQ HTTP Support (if needed)
4. Reboot if prompted

## Current Implementation Status

### Completed
- Base Blazor Server project structure
- Bootstrap UI framework integration
- Basic layout and navigation
- Launch profiles and configuration files

### Not Yet Implemented
- MSMQ service integration
- Queue discovery and monitoring services
- Message viewing and management UI
- Data models and DTOs
- Remote computer connection logic
- Windows Service hosting configuration
- Search and filtering functionality
- Bulk operations (delete, move, export, purge)
- Message body format detection and display

## Next Steps for Development

Based on the PRD, implement in this order:

1. **Core Services:**
   - Create MSMQ service wrapper using System.Messaging
   - Implement queue discovery service
   - Implement message retrieval service

2. **Data Models:**
   - Create Queue, Message, and RemoteComputer DTOs
   - Define export format enums

3. **UI Components:**
   - Queue tree view component (left panel)
   - Message grid component (middle panel)
   - Message detail drawer component (right panel)

4. **Operations:**
   - Single message operations (delete, move, resend)
   - Bulk operations (purge, export all)
   - Search and filtering logic

5. **Windows Service:**
   - Configure Windows Service hosting
   - Add service installation scripts
   - Configure auto-start behavior

## Important Notes

- This is a Windows-only application (MSMQ is Windows-specific)
- All destructive operations (delete, purge) require confirmation dialogs
- Message bodies support XML, JSON, Plain Text, and Binary (hex) display
- Remote queue access requires appropriate Windows permissions
- The application uses Windows Authentication for security
