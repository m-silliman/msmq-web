# MSMQ Monitor & Management Tool

A Blazor Server web application for monitoring and managing Microsoft Message Queue (MSMQ) queues across local and remote computers.

## Solution Structure

```
MsMqManager.sln                    # Main solution file
├── MsMqApp/                       # Main Blazor Server application
│   ├── Components/
│   │   ├── Layout/               # Layout components (MainLayout, NavMenu)
│   │   ├── Pages/                # Routable page components
│   │   ├── Shared/               # Reusable shared components
│   │   └── Features/             # Feature-specific components
│   ├── Services/                 # Application-specific services
│   ├── Extensions/               # Extension methods
│   ├── wwwroot/                  # Static assets
│   └── Program.cs                # Application entry point with DI configuration
│
├── MsMqApp.Models/               # Domain models and DTOs
│   ├── Dtos/                     # Data Transfer Objects
│   │   ├── QueueDto.cs
│   │   └── MessageDto.cs
│   ├── Enums/                    # Enumerations
│   │   └── MessageBodyFormat.cs
│   └── Configuration/            # Configuration models
│       └── ApplicationSettings.cs
│
├── MsMqApp.Services/             # Business logic and MSMQ integration
│   ├── Interfaces/               # Service interfaces
│   │   └── IMsmqService.cs
│   └── Implementations/          # Service implementations
│       └── MsmqService.cs
│
└── MsMqApp.Tests/                # xUnit test project
    └── (Test files)
```

## Prerequisites

- .NET 9.0 SDK or later
- Windows OS with MSMQ feature installed
- Visual Studio 2022 / VS Code with C# extension
- Administrator privileges for service installation

## Getting Started

### 1. Clone the repository
```bash
git clone <repository-url>
cd msmqmgr
```

### 2. Restore dependencies
```bash
dotnet restore
```

### 3. Build the solution
```bash
dotnet build
```

### 4. Run the application
```bash
# Run in development mode (HTTP)
dotnet run --project MsMqApp --launch-profile http

# Run with HTTPS
dotnet run --project MsMqApp --launch-profile https
```

### 5. Run tests
```bash
dotnet test
```

## Configuration

Application settings are configured in `appsettings.json`:

```json
{
  "Application": {
    "DefaultRefreshIntervalSeconds": 5,
    "MaxMessageBodySizeBytes": 1048576,
    "MessageListPageSize": 100,
    "RemoteConnectionTimeoutSeconds": 30
  }
}
```

## Development

### Coding Standards

This project follows strict coding standards documented in `Documentation/coding-standards.md`. Key requirements:

- **Code-behind pattern is MANDATORY** for all Blazor components
- `.razor` files contain markup only
- `.razor.cs` files contain all logic
- File size limits enforced (300 lines for .razor, 500 lines for code-behind)
- All async methods must have `Async` suffix
- Use dependency injection for all services

### Project References

```
MsMqApp → MsMqApp.Services → MsMqApp.Models
MsMqApp → MsMqApp.Models
MsMqApp.Tests → All projects
```

## Build and Deployment

### Build for Release
```bash
dotnet build -c Release
```

### Publish as Windows Service
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Documentation

- `CLAUDE.md` - Guidance for AI assistants working with this codebase
- `Documentation/prd.md` - Product Requirements Document (880 lines)
- `Documentation/coding-standards.md` - Comprehensive coding standards (4,254 lines)

## Project Status

**Current Phase:** Early scaffolding

### Completed
- ✅ Service layer interfaces
- ✅ MSMQ integration using System.Messaging
- ✅ Queue discovery and monitoring
- ✅ Message viewing and operations
- ✅ UI components (tree view, data grid, detail drawer)
- ✅ Search and filtering functionality
- ✅ Windows Service hosting configuration

### To Do
- ⬜ Remote computer connection management
- ⬜ Bulk operations (delete, move, export, purge)

## License

TBD

## Contributing

[Contribution guidelines]
