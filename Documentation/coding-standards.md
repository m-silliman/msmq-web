\# Blazor Coding Standards \& Best Practices



\## Document Overview



This document establishes coding standards and best practices for Blazor development to ensure consistency, maintainability, and code quality across the project.



\*\*Version\*\*: 1.0  

\*\*Last Updated\*\*: October 18, 2025  

\*\*Applies To\*\*: All Blazor Server and WebAssembly projects



---



\## Table of Contents



1\. \[Core Principles](#core-principles)

2\. \[Component Architecture](#component-architecture)

3\. \[Code-Behind Pattern](#code-behind-pattern)

4\. \[File Size Limits](#file-size-limits)

5\. \[Component Design](#component-design)

6\. \[C# Coding Standards](#c-coding-standards)

7\. \[Razor Markup Standards](#razor-markup-standards)

8\. \[State Management](#state-management)

9\. \[Dependency Injection](#dependency-injection)

10\. \[Error Handling](#error-handling)

11\. \[Performance Guidelines](#performance-guidelines)

12\. \[Testing Standards](#testing-standards)

13\. \[Naming Conventions](#naming-conventions)

14\. \[Code Review Checklist](#code-review-checklist)



---



\## 1. Core Principles



\### 1.1 Clean Code Fundamentals



All code must adhere to the following clean code principles:



\- \*\*Readability First\*\*: Code is read far more often than it is written. Optimize for clarity.

\- \*\*Single Responsibility Principle (SRP)\*\*: Each class, method, and component should have one reason to change.

\- \*\*Don't Repeat Yourself (DRY)\*\*: Eliminate code duplication through abstraction and reuse.

\- \*\*KISS (Keep It Simple, Stupid)\*\*: Favor simple solutions over complex ones.

\- \*\*YAGNI (You Aren't Gonna Need It)\*\*: Don't add functionality until it's necessary.

\- \*\*Open/Closed Principle\*\*: Components should be open for extension but closed for modification.

\- \*\*Dependency Inversion\*\*: Depend on abstractions, not concretions.



\### 1.2 Code Quality Standards



\- Code must be self-documenting with clear, meaningful names

\- Complex logic must include explanatory comments

\- All public APIs must have XML documentation comments

\- Code must be formatted consistently using `.editorconfig` settings

\- No compiler warnings are acceptable in production code

\- Code must pass static analysis without suppressions (unless explicitly justified)



---



\## 2. Component Architecture



\### 2.1 Code-Behind Separation (MANDATORY)



\*\*Rule\*\*: All C# logic MUST be separated from Razor markup using the code-behind pattern.



\#### ✅ Correct Approach



\*\*MyComponent.razor\*\*

```razor

@inherits MyComponentBase



<div class="my-component">

&nbsp;   <h2>@Title</h2>

&nbsp;   <button @onclick="HandleClickAsync">Click Me</button>

&nbsp;   @if (IsLoading)

&nbsp;   {

&nbsp;       <LoadingSpinner />

&nbsp;   }

&nbsp;   else

&nbsp;   {

&nbsp;       <p>@Message</p>

&nbsp;   }

</div>

```



\*\*MyComponent.razor.cs\*\*

```csharp

namespace MyApp.Components;



public partial class MyComponentBase : ComponentBase

{

&nbsp;   \[Parameter]

&nbsp;   public string Title { get; set; } = string.Empty;



&nbsp;   protected string Message { get; set; } = string.Empty;

&nbsp;   protected bool IsLoading { get; set; }



&nbsp;   protected override async Task OnInitializedAsync()

&nbsp;   {

&nbsp;       await LoadDataAsync();

&nbsp;   }



&nbsp;   protected async Task HandleClickAsync()

&nbsp;   {

&nbsp;       IsLoading = true;

&nbsp;       await ProcessActionAsync();

&nbsp;       IsLoading = false;

&nbsp;   }



&nbsp;   private async Task LoadDataAsync()

&nbsp;   {

&nbsp;       // Implementation

&nbsp;   }



&nbsp;   private async Task ProcessActionAsync()

&nbsp;   {

&nbsp;       // Implementation

&nbsp;   }

}

```



\#### ❌ Incorrect Approach



\*\*MyComponent.razor\*\* (DO NOT DO THIS)

```razor

@code {

&nbsp;   \[Parameter]

&nbsp;   public string Title { get; set; }

&nbsp;   

&nbsp;   private string message;

&nbsp;   private bool isLoading;

&nbsp;   

&nbsp;   protected override async Task OnInitializedAsync()

&nbsp;   {

&nbsp;       // Logic mixed with markup

&nbsp;   }

&nbsp;   

&nbsp;   private async Task HandleClickAsync()

&nbsp;   {

&nbsp;       // More logic in the razor file

&nbsp;   }

}

```



\### 2.2 Rationale for Code-Behind



\- \*\*Separation of Concerns\*\*: Markup and logic serve different purposes

\- \*\*Testability\*\*: Code-behind classes are easier to unit test

\- \*\*Readability\*\*: Developers can focus on UI or logic independently

\- \*\*Maintainability\*\*: Changes to logic don't require navigating markup

\- \*\*Tooling\*\*: Better IntelliSense and refactoring support

\- \*\*Code Reviews\*\*: Easier to review logic changes separately from UI changes



---



\## 3. File Size Limits



\### 3.1 Hard Limits (NON-NEGOTIABLE)



| File Type | Hard Limit | Action Required |

|-----------|------------|-----------------|

| C# Code Files (.cs) | \*\*1000 lines\*\* | MUST refactor if exceeded |

| Razor Files (.razor) | \*\*500 lines\*\* | MUST split into smaller components |

| CSS Files (.css) | \*\*800 lines\*\* | MUST split into multiple files |



\*\*Enforcement\*\*: 

\- Pre-commit hooks will reject files exceeding hard limits

\- CI/CD pipeline will fail builds with oversized files

\- Code reviews will automatically flag files approaching limits



\### 3.2 Target Limits (RECOMMENDED)



| File Type | Target Limit | Reasoning |

|-----------|--------------|-----------|

| C# Code Files (.cs) | \*\*250 lines\*\* | Encourages focused, testable classes |

| Razor Files (.razor) | \*\*150 lines\*\* | Promotes component reusability |

| Methods | \*\*50 lines\*\* | Ensures single responsibility |

| Classes | \*\*300 lines\*\* | Maintains cohesion |



\### 3.3 Line Counting Rules



\- Lines are counted including whitespace and comments

\- Exclude: auto-generated code, license headers

\- Use code metrics tools to track and report

\- Tooling recommendations:

&nbsp; - Visual Studio Code Metrics

&nbsp; - SonarQube

&nbsp; - Custom PowerShell scripts for CI/CD



\### 3.4 What to Do When Approaching Limits



When a file approaches size limits:



1\. \*\*Extract Methods\*\*: Break down large methods into smaller, named methods

2\. \*\*Extract Classes\*\*: Move related functionality into separate classes

3\. \*\*Extract Components\*\*: Split large components into smaller, reusable ones

4\. \*\*Use Partial Classes\*\*: For generated code or very specific scenarios

5\. \*\*Create Service Classes\*\*: Move business logic to dedicated service classes

6\. \*\*Introduce View Models\*\*: Separate data transformation logic



\*\*Example Refactoring\*\*:



Before (500+ lines):

```csharp

public partial class UserManagementComponent : ComponentBase

{

&nbsp;   // 500 lines of user CRUD, validation, export, import, etc.

}

```



After (refactored):

```csharp

// UserManagementComponent.razor.cs (150 lines)

public partial class UserManagementComponent : ComponentBase

{

&nbsp;   \[Inject] private IUserService UserService { get; set; }

&nbsp;   \[Inject] private IUserValidator Validator { get; set; }

&nbsp;   \[Inject] private IUserExportService ExportService { get; set; }

&nbsp;   

&nbsp;   // Component coordination logic only

}



// Services/UserService.cs (200 lines)

public class UserService : IUserService

{

&nbsp;   // CRUD operations

}



// Services/UserValidator.cs (100 lines)

public class UserValidator : IUserValidator

{

&nbsp;   // Validation logic

}



// Services/UserExportService.cs (150 lines)

public class UserExportService : IUserExportService

{

&nbsp;   // Export/import logic

}

```



---



\## 4. Component Design



\### 4.1 Component Reusability (MANDATORY)



\*\*Rule\*\*: Favor small, reusable components over large, monolithic ones.



\#### Benefits of Small Components



\- \*\*Reusability\*\*: Can be used across multiple pages and contexts

\- \*\*Testability\*\*: Easier to test in isolation

\- \*\*Maintainability\*\*: Simpler to understand and modify

\- \*\*Performance\*\*: Can be optimized independently

\- \*\*Collaboration\*\*: Multiple developers can work without conflicts



\### 4.2 Component Size Guidelines



| Component Type | Max Lines (Razor) | Max Lines (Code-Behind) | Max Parameters |

|----------------|-------------------|-------------------------|----------------|

| Atomic/Basic | 50 | 100 | 5 |

| Composite | 150 | 250 | 10 |

| Page/Route | 200 | 300 | 8 |

| Layout | 100 | 150 | 5 |



\### 4.3 Component Hierarchy



Follow the Atomic Design methodology:



```

Atoms (Basic building blocks)

├── Button

├── Input

├── Label

└── Icon



Molecules (Simple component groups)

├── FormField (Label + Input)

├── SearchBox (Input + Button)

└── Alert (Icon + Message)



Organisms (Complex component groups)

├── DataGrid

├── NavigationBar

└── UserProfile



Templates (Page layouts)

├── MainLayout

├── AdminLayout

└── PublicLayout



Pages (Route components)

├── Dashboard

├── UserList

└── Settings

```



\### 4.4 Component Extraction Checklist



Extract a new component when:



\- \[ ] Markup block exceeds 30 lines

\- \[ ] Same markup pattern appears 2+ times

\- \[ ] Logic can be isolated and reused

\- \[ ] Component has clear, single purpose

\- \[ ] Component could benefit from independent testing

\- \[ ] Component would improve readability of parent



\### 4.5 Component Organization



```

Components/

├── Shared/              # Reusable across entire app

│   ├── Buttons/

│   │   ├── PrimaryButton.razor

│   │   ├── PrimaryButton.razor.cs

│   │   ├── PrimaryButton.razor.css

│   │   └── SecondaryButton.razor

│   ├── Forms/

│   │   ├── FormField.razor

│   │   └── ValidationSummary.razor

│   └── Layout/

│       ├── Header.razor

│       └── Footer.razor

├── Features/            # Feature-specific components

│   ├── Users/

│   │   ├── UserCard.razor

│   │   ├── UserList.razor

│   │   └── UserEditForm.razor

│   └── Queues/

│       ├── QueueTreeView.razor

│       ├── MessageList.razor

│       └── MessageDetail.razor

└── Pages/               # Routable page components

&nbsp;   ├── Index.razor

&nbsp;   ├── Users.razor

&nbsp;   └── Settings.razor

```



---



\## 5. C# Coding Standards



\### 5.1 General Guidelines



\- Use C# 11+ language features where appropriate

\- Enable nullable reference types in all projects

\- Use `var` when type is obvious from right side

\- Use explicit types when clarity is improved

\- Prefer expression-bodied members for simple operations

\- Use pattern matching for type checks and null checks

\- Prefer `async/await` over `Task.Result` or `Task.Wait()`



\### 5.2 Naming Conventions



```csharp

// Namespaces: PascalCase

namespace MyApp.Components.Shared;



// Classes: PascalCase

public class UserService { }



// Interfaces: I + PascalCase

public interface IUserService { }



// Methods: PascalCase

public async Task LoadDataAsync() { }



// Properties: PascalCase

public string FirstName { get; set; }



// Private fields: \_camelCase

private readonly IUserService \_userService;



// Parameters: camelCase

public void ProcessUser(string userId) { }



// Local variables: camelCase

var userName = "John";



// Constants: PascalCase

private const int MaxRetries = 3;



// Component Parameters: PascalCase with \[Parameter]

\[Parameter]

public string Title { get; set; } = string.Empty;



// Event Callbacks: On + PascalCase

\[Parameter]

public EventCallback<string> OnValueChanged { get; set; }

```



\### 5.3 Method Guidelines



\#### Method Size

\- \*\*Target\*\*: 20 lines or fewer

\- \*\*Maximum\*\*: 50 lines

\- \*\*If longer\*\*: Refactor into smaller methods



\#### Method Responsibility

```csharp

// ✅ Good: Single responsibility, clear purpose

protected async Task LoadUserDataAsync()

{

&nbsp;   IsLoading = true;

&nbsp;   try

&nbsp;   {

&nbsp;       Users = await UserService.GetUsersAsync();

&nbsp;   }

&nbsp;   finally

&nbsp;   {

&nbsp;       IsLoading = false;

&nbsp;   }

}



// ❌ Bad: Multiple responsibilities

protected async Task LoadEverythingAsync()

{

&nbsp;   // Loading users

&nbsp;   // Loading queues

&nbsp;   // Loading settings

&nbsp;   // Validating data

&nbsp;   // Updating UI

&nbsp;   // Logging

&nbsp;   // etc... (100+ lines)

}

```



\#### Parameter Count

\- \*\*Maximum\*\*: 5 parameters

\- \*\*If more needed\*\*: Create a parameter object or options class



```csharp

// ❌ Bad: Too many parameters

public void UpdateUser(string id, string name, string email, string phone, 

&nbsp;   string address, DateTime birthDate, bool isActive)

{

}



// ✅ Good: Parameter object

public void UpdateUser(UserUpdateRequest request)

{

}



public record UserUpdateRequest(

&nbsp;   string Id,

&nbsp;   string Name,

&nbsp;   string Email,

&nbsp;   string Phone,

&nbsp;   string Address,

&nbsp;   DateTime BirthDate,

&nbsp;   bool IsActive

);

```



\### 5.4 Properties and Fields



```csharp

// ✅ Good: Auto-properties for simple cases

public string Name { get; set; } = string.Empty;



// ✅ Good: Readonly for immutability

public string Id { get; init; }



// ✅ Good: Required for mandatory parameters

\[Parameter]

\[EditorRequired]

public required string UserId { get; set; }



// ✅ Good: Private setters when appropriate

public int Count { get; private set; }



// ✅ Good: Init-only for initialization

public DateTime CreatedAt { get; init; } = DateTime.UtcNow;



// ❌ Bad: Public fields

public string userName; // Use property instead

```



\### 5.5 Null Handling



```csharp

// ✅ Good: Null-conditional operator

var length = user?.Name?.Length ?? 0;



// ✅ Good: Null-coalescing assignment

\_userService ??= new UserService();



// ✅ Good: Pattern matching

if (user is { IsActive: true, Role: "Admin" })

{

&nbsp;   // Process admin user

}



// ✅ Good: Required and non-nullable

\[Parameter]

\[EditorRequired]

public required IUserService UserService { get; set; }



// ✅ Good: Initialize non-nullable strings

public string Name { get; set; } = string.Empty;



// ❌ Bad: Suppressing warnings without justification

public string GetName() => user!.Name; // Don't do this

```



\### 5.6 LINQ and Collections



```csharp

// ✅ Good: Method chaining for readability

var activeAdmins = users

&nbsp;   .Where(u => u.IsActive)

&nbsp;   .Where(u => u.Role == "Admin")

&nbsp;   .OrderBy(u => u.Name)

&nbsp;   .ToList();



// ✅ Good: List patterns (C# 11+)

if (items is \[var first, .., var last])

{

&nbsp;   // Process first and last

}



// ✅ Good: Use appropriate collection types

List<T>           // For general purpose, indexed access

IEnumerable<T>    // For deferred execution, query results

IReadOnlyList<T>  // For exposing collections without modification

HashSet<T>        // For unique items, fast lookup

Dictionary<K,V>   // For key-value pairs



// ❌ Bad: Multiple enumeration

var count = users.Count();

var first = users.First(); // Enumerates again

var any = users.Any();     // Enumerates again



// ✅ Good: Single enumeration

var userList = users.ToList();

var count = userList.Count;

var first = userList.First();

```



\### 5.7 Async/Await Guidelines



```csharp

// ✅ Good: Async suffix for async methods

public async Task<User> GetUserAsync(string id)

{

&nbsp;   return await \_userService.GetByIdAsync(id);

}



// ✅ Good: ConfigureAwait(false) in library code

public async Task ProcessAsync()

{

&nbsp;   await \_repository.SaveAsync().ConfigureAwait(false);

}



// ✅ Good: ValueTask for hot paths

public ValueTask<int> GetCachedCountAsync()

{

&nbsp;   if (\_cache.TryGetValue(key, out var value))

&nbsp;       return new ValueTask<int>(value);

&nbsp;   

&nbsp;   return new ValueTask<int>(LoadCountAsync());

}



// ❌ Bad: Async void (except event handlers)

public async void LoadData() { } // Don't do this



// ✅ Good: Async void only for event handlers

private async void OnButtonClick(MouseEventArgs e)

{

&nbsp;   await ProcessClickAsync();

}



// ❌ Bad: Blocking on async

var result = GetDataAsync().Result; // Can cause deadlocks



// ✅ Good: Await async calls

var result = await GetDataAsync();

```



\### 5.8 Exception Handling



```csharp

// ✅ Good: Specific exception types

try

{

&nbsp;   await SaveUserAsync(user);

}

catch (ValidationException ex)

{

&nbsp;   ShowValidationError(ex.Message);

}

catch (DatabaseException ex)

{

&nbsp;   Logger.LogError(ex, "Database error saving user");

&nbsp;   ShowError("Unable to save user. Please try again.");

}



// ✅ Good: Using statements for disposables

await using var connection = await CreateConnectionAsync();

await connection.ExecuteAsync(sql);



// ✅ Good: Exception filters

try

{

&nbsp;   await ProcessAsync();

}

catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)

{

&nbsp;   // Handle 404 specifically

}



// ❌ Bad: Catching and ignoring

try

{

&nbsp;   await SaveAsync();

}

catch { } // Never do this



// ❌ Bad: Catching too broadly

try

{

&nbsp;   await ProcessAsync();

}

catch (Exception ex)

{

&nbsp;   // Too broad, hides specific issues

}

```



\### 5.9 Comments and Documentation



```csharp

/// <summary>

/// Loads user data from the database and updates the component state.

/// </summary>

/// <param name="userId">The unique identifier of the user to load.</param>

/// <returns>A task representing the asynchronous operation.</returns>

/// <exception cref="UserNotFoundException">Thrown when user is not found.</exception>

public async Task LoadUserAsync(string userId)

{

&nbsp;   // Complex algorithm explanation when needed

&nbsp;   // Example: Using binary search because data is pre-sorted

&nbsp;   var index = BinarySearch(users, userId);

&nbsp;   

&nbsp;   // TODO: Add caching mechanism (JIRA-123)

&nbsp;   await \_repository.GetByIdAsync(userId);

}



// ✅ Good: XML documentation for public APIs

/// <summary>

/// Represents a message in the MSMQ queue.

/// </summary>

public class QueueMessage

{

&nbsp;   /// <summary>

&nbsp;   /// Gets or sets the unique identifier of the message.

&nbsp;   /// </summary>

&nbsp;   public string Id { get; set; } = string.Empty;

}



// ❌ Bad: Stating the obvious

// Set the name

user.Name = name; // Don't write comments like this



// ✅ Good: Explaining why, not what

// Using hash set for O(1) lookup performance with large datasets

var uniqueIds = new HashSet<string>();

```



---



\## 6. Razor Markup Standards



\### 6.1 General Markup Guidelines



```razor

@\* ✅ Good: Clear, well-structured markup \*@

<div class="user-card">

&nbsp;   <header class="user-card\_\_header">

&nbsp;       <h3>@User.Name</h3>

&nbsp;       <span class="user-card\_\_role">@User.Role</span>

&nbsp;   </header>

&nbsp;   

&nbsp;   <div class="user-card\_\_body">

&nbsp;       @if (ShowDetails)

&nbsp;       {

&nbsp;           <UserDetails User="@User" />

&nbsp;       }

&nbsp;   </div>

&nbsp;   

&nbsp;   <footer class="user-card\_\_actions">

&nbsp;       <button @onclick="OnEditClickAsync">Edit</button>

&nbsp;       <button @onclick="OnDeleteClickAsync">Delete</button>

&nbsp;   </footer>

</div>



@\* ❌ Bad: Deeply nested, hard to read \*@

<div><div><div><div>

&nbsp;   @if (condition1) { if (condition2) { if (condition3) {

&nbsp;       <span>@value</span>

&nbsp;   }}}

</div></div></div></div>

```



\### 6.2 Directives and Code Blocks



```razor

@\* ✅ Good: Directives at top \*@

@page "/users"

@using MyApp.Services

@inject IUserService UserService

@inherits UserListBase



@\* ✅ Good: Minimal code in markup \*@

@if (IsLoading)

{

&nbsp;   <LoadingSpinner />

}

else if (HasError)

{

&nbsp;   <ErrorDisplay Message="@ErrorMessage" />

}

else

{

&nbsp;   <UserGrid Users="@Users" OnUserSelected="HandleUserSelected" />

}



@\* ❌ Bad: Complex logic in markup \*@

@{

&nbsp;   var filteredUsers = Users

&nbsp;       .Where(u => u.IsActive)

&nbsp;       .Where(u => !string.IsNullOrEmpty(SearchTerm) 

&nbsp;           ? u.Name.Contains(SearchTerm) 

&nbsp;           : true)

&nbsp;       .OrderBy(u => u.Name)

&nbsp;       .Take(PageSize)

&nbsp;       .ToList();

}

@\* Move this to code-behind! \*@

```



\### 6.3 Conditional Rendering



```razor

@\* ✅ Good: Simple conditionals \*@

@if (IsVisible)

{

&nbsp;   <div>Content</div>

}



@\* ✅ Good: Ternary for simple cases \*@

<span class="@(IsActive ? "active" : "inactive")">Status</span>



@\* ✅ Good: Pattern matching \*@

@switch (UserRole)

{

&nbsp;   case "Admin":

&nbsp;       <AdminPanel />

&nbsp;       break;

&nbsp;   case "User":

&nbsp;       <UserPanel />

&nbsp;       break;

&nbsp;   default:

&nbsp;       <GuestPanel />

&nbsp;       break;

}



@\* ❌ Bad: Complex nested conditions in markup \*@

@\* Move to code-behind or separate component \*@

```



\### 6.4 Loops and Collections



```razor

@\* ✅ Good: Simple loops \*@

@foreach (var user in Users)

{

&nbsp;   <UserCard User="@user" />

}



@\* ✅ Good: Keys for optimization \*@

@foreach (var message in Messages)

{

&nbsp;   <MessageRow @key="message.Id" Message="@message" />

}



@\* ✅ Good: Empty state handling \*@

@if (Users.Any())

{

&nbsp;   @foreach (var user in Users)

&nbsp;   {

&nbsp;       <UserCard User="@user" />

&nbsp;   }

}

else

{

&nbsp;   <EmptyState Message="No users found" />

}

```



\### 6.5 Event Handlers



```razor

@\* ✅ Good: Simple event binding \*@

<button @onclick="HandleClickAsync">Click Me</button>



@\* ✅ Good: Event with parameters \*@

<button @onclick="() => HandleDeleteAsync(user.Id)">Delete</button>



@\* ✅ Good: Event with EventArgs \*@

<input @onchange="HandleInputChange" />



@\* ❌ Bad: Complex logic inline \*@

<button @onclick="async () => { 

&nbsp;   IsLoading = true;

&nbsp;   await Service.ProcessAsync();

&nbsp;   await RefreshAsync();

&nbsp;   IsLoading = false;

}">Process</button>

@\* Move to code-behind method! \*@

```



\### 6.6 Component Parameters



```razor

@\* ✅ Good: Clear parameter binding \*@

<DataGrid 

&nbsp;   Items="@Users"

&nbsp;   IsLoading="@IsLoading"

&nbsp;   OnRowClick="HandleRowClick"

&nbsp;   RowsPerPage="25"

&nbsp;   ShowPagination="true" />



@\* ✅ Good: Attribute splatting \*@

<input @attributes="AdditionalAttributes" />



@\* ✅ Good: Child content \*@

<Card>

&nbsp;   <Header>

&nbsp;       <h3>Title</h3>

&nbsp;   </Header>

&nbsp;   <Body>

&nbsp;       <p>Content</p>

&nbsp;   </Body>

</Card>

```



---



\## 7. State Management



\### 7.1 Component State



```csharp

// ✅ Good: Private state in code-behind

public partial class UserListBase : ComponentBase

{

&nbsp;   private List<User> \_users = new();

&nbsp;   private bool \_isLoading;

&nbsp;   private string \_searchTerm = string.Empty;

&nbsp;   

&nbsp;   protected IEnumerable<User> FilteredUsers =>

&nbsp;       \_users.Where(u => u.Name.Contains(\_searchTerm, 

&nbsp;           StringComparison.OrdinalIgnoreCase));

}

```



\### 7.2 Cascading Parameters



```csharp

// ✅ Good: Use for cross-cutting concerns

\[CascadingParameter]

public ApplicationState? AppState { get; set; }



\[CascadingParameter]

public Task<AuthenticationState>? AuthenticationState { get; set; }

```



\### 7.3 Application State



```csharp

// ✅ Good: Scoped service for state management

public class QueueMonitorState

{

&nbsp;   private readonly List<QueueConnection> \_connections = new();

&nbsp;   

&nbsp;   public event Action? OnStateChanged;

&nbsp;   

&nbsp;   public IReadOnlyList<QueueConnection> Connections => \_connections.AsReadOnly();

&nbsp;   

&nbsp;   public void AddConnection(QueueConnection connection)

&nbsp;   {

&nbsp;       \_connections.Add(connection);

&nbsp;       NotifyStateChanged();

&nbsp;   }

&nbsp;   

&nbsp;   private void NotifyStateChanged() => OnStateChanged?.Invoke();

}



// Registration in Program.cs

builder.Services.AddScoped<QueueMonitorState>();

```



---



\## 8. Dependency Injection



\### 8.1 Constructor Injection (Preferred)



```csharp

// ✅ Good: Constructor injection in services

public class UserService : IUserService

{

&nbsp;   private readonly IUserRepository \_repository;

&nbsp;   private readonly ILogger<UserService> \_logger;

&nbsp;   

&nbsp;   public UserService(

&nbsp;       IUserRepository repository,

&nbsp;       ILogger<UserService> logger)

&nbsp;   {

&nbsp;       \_repository = repository;

&nbsp;       \_logger = logger;

&nbsp;   }

}

```



\### 8.2 Property Injection in Components



```csharp

// ✅ Good: Property injection in Blazor components

public partial class UserListBase : ComponentBase

{

&nbsp;   \[Inject]

&nbsp;   private IUserService UserService { get; set; } = default!;

&nbsp;   

&nbsp;   \[Inject]

&nbsp;   private NavigationManager Navigation { get; set; } = default!;

&nbsp;   

&nbsp;   \[Inject]

&nbsp;   private ILogger<UserListBase> Logger { get; set; } = default!;

}

```



\### 8.3 Service Lifetime



```csharp

// Program.cs

builder.Services.AddScoped<IUserService, UserService>();        // Per-connection

builder.Services.AddSingleton<IConfigurationService, ConfigService>(); // App lifetime

builder.Services.AddTransient<IEmailService, EmailService>();   // Per-request

```



---



\## 9. Error Handling



\### 9.1 Component-Level Error Boundaries



```razor

<ErrorBoundary>

&nbsp;   <ChildContent>

&nbsp;       <UserList />

&nbsp;   </ChildContent>

&nbsp;   <ErrorContent Context="exception">

&nbsp;       <ErrorDisplay Exception="@exception" />

&nbsp;   </ErrorContent>

</ErrorBoundary>

```



\### 9.2 Try-Catch Patterns



```csharp

protected override async Task OnInitializedAsync()

{

&nbsp;   try

&nbsp;   {

&nbsp;       IsLoading = true;

&nbsp;       await LoadDataAsync();

&nbsp;   }

&nbsp;   catch (Exception ex)

&nbsp;   {

&nbsp;       Logger.LogError(ex, "Error loading user data");

&nbsp;       ErrorMessage = "Unable to load users. Please try again.";

&nbsp;       HasError = true;

&nbsp;   }

&nbsp;   finally

&nbsp;   {

&nbsp;       IsLoading = false;

&nbsp;   }

}

```



---



\## 10. Performance Guidelines



\### 10.1 Virtualization



```razor

@\* ✅ Good: Virtualize large lists \*@

<Virtualize Items="@Messages" Context="message">

&nbsp;   <MessageRow Message="@message" />

</Virtualize>

```



\### 10.2 Lazy Loading



```csharp

// ✅ Good: Lazy load expensive components

<div>

&nbsp;   @if (ShowDetailPanel)

&nbsp;   {

&nbsp;       <MessageDetailPanel Message="@SelectedMessage" />

&nbsp;   }

</div>

```



\### 10.3 Debouncing



```csharp

// ✅ Good: Debounce search input

private Timer? \_debounceTimer;



private void OnSearchInput(ChangeEventArgs e)

{

&nbsp;   \_debounceTimer?.Dispose();

&nbsp;   \_debounceTimer = new Timer(300);

&nbsp;   \_debounceTimer.Elapsed += async (sender, args) => await PerformSearchAsync(e.Value?.ToString());

&nbsp;   \_debounceTimer.AutoReset = false;

&nbsp;   \_debounceTimer.Start();

}

```



\### 10.4 Dispose Pattern



```csharp

public partial class UserListBase : ComponentBase, IDisposable

{

&nbsp;   private Timer? \_refreshTimer;

&nbsp;   

&nbsp;   public void Dispose()

&nbsp;   {

&nbsp;       \_refreshTimer?.Dispose();

&nbsp;       \_httpClient?.Dispose();

&nbsp;   }

}

```



---



\## 11. Testing Standards



\### 11.1 Unit Tests



```csharp

// ✅ Good: Test code-behind logic

public class UserListBaseTests

{

&nbsp;   \[Fact]

&nbsp;   public async Task LoadDataAsync\_ShouldPopulateUsers()

&nbsp;   {

&nbsp;       // Arrange

&nbsp;       var mockService = new Mock<IUserService>();

&nbsp;       mockService.Setup(s => s.GetUsersAsync())

&nbsp;           .ReturnsAsync(new List<User> { new User { Id = "1" } });

&nbsp;       

&nbsp;       var component = new UserListBase

&nbsp;       {

&nbsp;           UserService = mockService.Object

&nbsp;       };

&nbsp;       

&nbsp;       // Act

&nbsp;       await component.LoadDataAsync();

&nbsp;       

&nbsp;       // Assert

&nbsp;       Assert.Single(component.Users);

&nbsp;   }

}

```



\### 11.2 Component Tests (bUnit)



```csharp

\[Fact]

public void UserCard\_ShouldRenderUserName()

{

&nbsp;   // Arrange

&nbsp;   using var ctx = new TestContext();

&nbsp;   var user = new User { Name = "John Doe" };

&nbsp;   

&nbsp;   // Act

&nbsp;   var cut = ctx.RenderComponent<UserCard>(parameters => 

&nbsp;       parameters.Add(p => p.User, user));

&nbsp;   

&nbsp;   // Assert

&nbsp;   cut.Find("h3").TextContent.Should().Be("John Doe");

}

```



---



\## 12. Naming Conventions



\### 12.1 Files and Folders



```

Components/

├── Shared/

│   ├── Buttons/

│   │   ├── PrimaryButton.razor           # PascalCase

│   │   ├── PrimaryButton.razor.cs        # Matches component

│   │   └── PrimaryButton.razor.css       # Matches component

│   └── Forms/

│       └── FormField.razor

├── Features/

│   └── Users/

│       ├── UserList.razor                # Singular for feature

│       └── UserCard.razor

└── Pages/

&nbsp;   ├── Index.razor                       # Routable components

&nbsp;   └── UserManagement.razor

```



\### 12.2 CSS Classes (BEM Convention)



```css

/\* ✅ Good: BEM naming \*/

.user-card { }

.user-card\_\_header { }

.user-card\_\_title { }

.user-card\_\_actions { }

.user-card--highlighted { }



/\* ❌ Bad: Unclear naming \*/

.card1 { }

.userCard { }

.uc-h { }

```



---



\## 13. Code Review Checklist



\### 13.1 Architecture Review



\- \[ ] C# code is separated into code-behind files (.razor.cs)

\- \[ ] No `@code` blocks in .razor files

\- \[ ] Components follow single responsibility principle

\- \[ ] Large components have been broken into smaller, reusable ones

\- \[ ] Business logic is in service classes, not components

\- \[ ] Dependencies are properly injected



\### 13.2 File Size Review



\- \[ ] No C# files exceed 1000 lines (HARD LIMIT)

\- \[ ] No Razor files exceed 500 lines (HARD LIMIT)

\- \[ ] C# files target 250 lines or less

\- \[ ] Razor files target 150 lines or less

\- \[ ] Methods are under 50 lines

\- \[ ] Classes are under 300 lines



\### 13.3 Code Quality Review



\- \[ ] All public APIs have XML documentation comments

\- \[ ] Complex logic has explanatory comments

\- \[ ] No compiler warnings

\- \[ ] No code analysis warnings (without justified suppressions)

\- \[ ] Code follows SOLID principles

\- \[ ] No code duplication (DRY principle)

\- \[ ] Meaningful variable and method names (self-documenting)



\### 13.4 Blazor-Specific Review



\- \[ ] Component parameters use `\[Parameter]` attribute

\- \[ ] Required parameters use `\[EditorRequired]` attribute

\- \[ ] Event callbacks use `EventCallback<T>` type

\- \[ ] Async methods have `Async` suffix

\- \[ ] Components implement `IDisposable` when needed

\- \[ ] State changes call `StateHasChanged()` when necessary

\- \[ ] Large lists use virtualization

\- \[ ] Components use appropriate lifecycle methods



\### 13.5 Performance Review



\- \[ ] Async/await used correctly (no `.Result` or `.Wait()`)

\- \[ ] No unnecessary re-renders

\- \[ ] Expensive operations are cached or memoized

\- \[ ] Large lists use `@key` directive

\- \[ ] Components dispose of resources properly

\- \[ ] Database queries are optimized

\- \[ ] No N+1 query problems



\### 13.6 Security Review



\- \[ ] User input is validated

\- \[ ] SQL injection vulnerabilities prevented (parameterized queries)

\- \[ ] XSS vulnerabilities prevented (proper encoding)

\- \[ ] Sensitive data is not logged

\- \[ ] Authentication/authorization is enforced

\- \[ ] CSRF protection is enabled



\### 13.7 Error Handling Review



\- \[ ] All async operations have try-catch blocks

\- \[ ] Errors are logged appropriately

\- \[ ] User-friendly error messages are shown

\- \[ ] Error boundaries are used for critical components

\- \[ ] Exceptions are not swallowed silently

\- \[ ] Specific exception types are caught (not generic `Exception`)



\### 13.8 Testing Review



\- \[ ] Unit tests exist for business logic

\- \[ ] Component tests exist for complex components

\- \[ ] Tests follow AAA pattern (Arrange, Act, Assert)

\- \[ ] Tests are independent and can run in any order

\- \[ ] Test names clearly describe what is being tested

\- \[ ] Edge cases are tested



---



\## 14. Common Anti-Patterns to Avoid



\### 14.1 The God Component



```csharp

// ❌ BAD: 2000-line component doing everything

public partial class Dashboard : ComponentBase

{

&nbsp;   // User management

&nbsp;   // Queue management

&nbsp;   // Message processing

&nbsp;   // Reports generation

&nbsp;   // Settings management

&nbsp;   // Notifications

&nbsp;   // etc...

}



// ✅ GOOD: Focused components with clear responsibilities

public partial class Dashboard : ComponentBase

{

&nbsp;   // Only dashboard layout and coordination

}



// Separate components:

// - UserManagementWidget.razor

// - QueueStatusWidget.razor

// - MessageStatsWidget.razor

// - RecentActivityWidget.razor

```



\### 14.2 Logic in Markup



```razor

@\* ❌ BAD: Complex logic in razor file \*@

@code {

&nbsp;   private async Task LoadDataAsync()

&nbsp;   {

&nbsp;       // 200 lines of complex logic here

&nbsp;   }

&nbsp;   

&nbsp;   private void ProcessMessage(Message msg)

&nbsp;   {

&nbsp;       // 150 lines of business logic

&nbsp;   }

}



@\* ✅ GOOD: All logic in code-behind \*@

@\* (Razor file only contains markup) \*@

```



\### 14.3 Tight Coupling



```csharp

// ❌ BAD: Direct instantiation

public class MessageProcessor

{

&nbsp;   private readonly MsmqService \_msmqService = new MsmqService();

&nbsp;   

&nbsp;   public void Process()

&nbsp;   {

&nbsp;       \_msmqService.GetMessages(); // Tightly coupled

&nbsp;   }

}



// ✅ GOOD: Dependency injection

public class MessageProcessor

{

&nbsp;   private readonly IMsmqService \_msmqService;

&nbsp;   

&nbsp;   public MessageProcessor(IMsmqService msmqService)

&nbsp;   {

&nbsp;       \_msmqService = msmqService;

&nbsp;   }

&nbsp;   

&nbsp;   public void Process()

&nbsp;   {

&nbsp;       \_msmqService.GetMessages();

&nbsp;   }

}

```



\### 14.4 Magic Numbers and Strings



```csharp

// ❌ BAD: Magic numbers and strings

if (user.Role == "admin" \&\& user.AccessLevel > 5)

{

&nbsp;   await Task.Delay(3000);

}



// ✅ GOOD: Named constants

private const string AdminRole = "admin";

private const int SuperUserAccessLevel = 5;

private const int RefreshDelayMs = 3000;



if (user.Role == AdminRole \&\& user.AccessLevel > SuperUserAccessLevel)

{

&nbsp;   await Task.Delay(RefreshDelayMs);

}



// ✅ BETTER: Enum for roles

public enum UserRole

{

&nbsp;   Guest,

&nbsp;   User,

&nbsp;   PowerUser,

&nbsp;   Admin

}

```



\### 14.5 Ignoring Disposal



```csharp

// ❌ BAD: Not disposing resources

public partial class DataGrid : ComponentBase

{

&nbsp;   private HttpClient \_httpClient = new HttpClient();

&nbsp;   private Timer \_timer = new Timer(1000);

&nbsp;   

&nbsp;   // No disposal! Memory leak!

}



// ✅ GOOD: Proper disposal

public partial class DataGrid : ComponentBase, IDisposable, IAsyncDisposable

{

&nbsp;   private HttpClient? \_httpClient;

&nbsp;   private Timer? \_timer;

&nbsp;   

&nbsp;   public void Dispose()

&nbsp;   {

&nbsp;       \_timer?.Dispose();

&nbsp;       \_httpClient?.Dispose();

&nbsp;   }

&nbsp;   

&nbsp;   public async ValueTask DisposeAsync()

&nbsp;   {

&nbsp;       if (\_timer != null)

&nbsp;       {

&nbsp;           await \_timer.DisposeAsync();

&nbsp;       }

&nbsp;       

&nbsp;       \_httpClient?.Dispose();

&nbsp;   }

}

```



\### 14.6 Async Void



```csharp

// ❌ BAD: Async void (can't be awaited, swallows exceptions)

public async void LoadDataAsync()

{

&nbsp;   await \_service.GetDataAsync();

}



// ✅ GOOD: Async Task

public async Task LoadDataAsync()

{

&nbsp;   await \_service.GetDataAsync();

}



// ✅ ACCEPTABLE: Only for event handlers

private async void OnButtonClick(MouseEventArgs e)

{

&nbsp;   try

&nbsp;   {

&nbsp;       await HandleClickAsync();

&nbsp;   }

&nbsp;   catch (Exception ex)

&nbsp;   {

&nbsp;       Logger.LogError(ex, "Error handling button click");

&nbsp;   }

}

```



\### 14.7 String Concatenation in Loops



```csharp

// ❌ BAD: String concatenation in loop

string result = "";

foreach (var item in items)

{

&nbsp;   result += item.ToString(); // Creates new string each iteration

}



// ✅ GOOD: StringBuilder

var builder = new StringBuilder();

foreach (var item in items)

{

&nbsp;   builder.Append(item.ToString());

}

string result = builder.ToString();



// ✅ BETTER: LINQ

string result = string.Join("", items.Select(i => i.ToString()));

```



\### 14.8 Premature Optimization



```csharp

// ❌ BAD: Over-optimizing before measuring

public class UserCache

{

&nbsp;   // Complex caching mechanism with LRU, weak references,

&nbsp;   // background cleanup threads, etc.

&nbsp;   // 500 lines of code for caching 10 users

}



// ✅ GOOD: Start simple, optimize when needed

public class UserCache

{

&nbsp;   private readonly Dictionary<string, User> \_cache = new();

&nbsp;   

&nbsp;   public User? Get(string id) => \_cache.GetValueOrDefault(id);

&nbsp;   public void Set(string id, User user) => \_cache\[id] = user;

}



// Optimize later based on actual performance data

```



---



\## 15. Clean Code Principles Applied to Blazor



\### 15.1 Meaningful Names



```csharp

// ❌ BAD: Unclear names

public class Mgr

{

&nbsp;   public async Task<List<Msg>> GetMsgs()

&nbsp;   {

&nbsp;       var d = new List<Msg>();

&nbsp;       var r = await \_svc.Get();

&nbsp;       foreach (var m in r)

&nbsp;       {

&nbsp;           d.Add(m);

&nbsp;       }

&nbsp;       return d;

&nbsp;   }

}



// ✅ GOOD: Self-documenting names

public class MessageManager

{

&nbsp;   public async Task<List<Message>> GetMessagesAsync()

&nbsp;   {

&nbsp;       var messages = new List<Message>();

&nbsp;       var retrievedMessages = await \_messageService.GetAllAsync();

&nbsp;       

&nbsp;       foreach (var message in retrievedMessages)

&nbsp;       {

&nbsp;           messages.Add(message);

&nbsp;       }

&nbsp;       

&nbsp;       return messages;

&nbsp;   }

}



// ✅ BETTER: Even clearer with LINQ

public class MessageManager

{

&nbsp;   public async Task<List<Message>> GetMessagesAsync()

&nbsp;   {

&nbsp;       var retrievedMessages = await \_messageService.GetAllAsync();

&nbsp;       return retrievedMessages.ToList();

&nbsp;   }

}

```



\### 15.2 Functions Should Do One Thing



```csharp

// ❌ BAD: Function doing multiple things

public async Task ProcessUserAsync(User user)

{

&nbsp;   // Validate user

&nbsp;   if (string.IsNullOrEmpty(user.Email)) throw new Exception();

&nbsp;   

&nbsp;   // Save to database

&nbsp;   await \_db.SaveAsync(user);

&nbsp;   

&nbsp;   // Send email

&nbsp;   await \_emailService.SendWelcomeEmailAsync(user);

&nbsp;   

&nbsp;   // Log action

&nbsp;   \_logger.LogInformation("User processed");

&nbsp;   

&nbsp;   // Update cache

&nbsp;   \_cache.Set(user.Id, user);

&nbsp;   

&nbsp;   // Notify other systems

&nbsp;   await \_notificationService.NotifyAsync(user);

}



// ✅ GOOD: Separate functions for separate concerns

public async Task ProcessUserAsync(User user)

{

&nbsp;   ValidateUser(user);

&nbsp;   await SaveUserAsync(user);

&nbsp;   await SendWelcomeEmailAsync(user);

&nbsp;   LogUserProcessed(user);

&nbsp;   UpdateUserCache(user);

&nbsp;   await NotifySystemsAsync(user);

}



private void ValidateUser(User user)

{

&nbsp;   if (string.IsNullOrEmpty(user.Email))

&nbsp;       throw new ValidationException("Email is required");

}



private async Task SaveUserAsync(User user)

{

&nbsp;   await \_db.SaveAsync(user);

}



// ... etc.

```



\### 15.3 DRY (Don't Repeat Yourself)



```csharp

// ❌ BAD: Repeated code

public async Task LoadActiveUsersAsync()

{

&nbsp;   IsLoading = true;

&nbsp;   try

&nbsp;   {

&nbsp;       Users = await \_userService.GetActiveUsersAsync();

&nbsp;   }

&nbsp;   catch (Exception ex)

&nbsp;   {

&nbsp;       Logger.LogError(ex, "Error loading active users");

&nbsp;       ErrorMessage = "Failed to load users";

&nbsp;   }

&nbsp;   finally

&nbsp;   {

&nbsp;       IsLoading = false;

&nbsp;   }

}



public async Task LoadInactiveUsersAsync()

{

&nbsp;   IsLoading = true;

&nbsp;   try

&nbsp;   {

&nbsp;       Users = await \_userService.GetInactiveUsersAsync();

&nbsp;   }

&nbsp;   catch (Exception ex)

&nbsp;   {

&nbsp;       Logger.LogError(ex, "Error loading inactive users");

&nbsp;       ErrorMessage = "Failed to load users";

&nbsp;   }

&nbsp;   finally

&nbsp;   {

&nbsp;       IsLoading = false;

&nbsp;   }

}



// ✅ GOOD: Extracted common pattern

public async Task LoadActiveUsersAsync()

{

&nbsp;   await ExecuteWithLoadingAsync(

&nbsp;       () => \_userService.GetActiveUsersAsync(),

&nbsp;       "Error loading active users");

}



public async Task LoadInactiveUsersAsync()

{

&nbsp;   await ExecuteWithLoadingAsync(

&nbsp;       () => \_userService.GetInactiveUsersAsync(),

&nbsp;       "Error loading inactive users");

}



private async Task ExecuteWithLoadingAsync(

&nbsp;   Func<Task<List<User>>> operation,

&nbsp;   string errorMessage)

{

&nbsp;   IsLoading = true;

&nbsp;   try

&nbsp;   {

&nbsp;       Users = await operation();

&nbsp;   }

&nbsp;   catch (Exception ex)

&nbsp;   {

&nbsp;       Logger.LogError(ex, errorMessage);

&nbsp;       ErrorMessage = "Failed to load users";

&nbsp;   }

&nbsp;   finally

&nbsp;   {

&nbsp;       IsLoading = false;

&nbsp;   }

}

```



\### 15.4 Prefer Composition Over Inheritance



```csharp

// ❌ BAD: Deep inheritance hierarchy

public class Component : ComponentBase { }

public class DataComponent : Component { }

public class ListComponent : DataComponent { }

public class UserListComponent : ListComponent { }

public class AdminUserListComponent : UserListComponent { }



// ✅ GOOD: Composition

public class AdminUserListComponent : ComponentBase

{

&nbsp;   \[Inject]

&nbsp;   private IDataService DataService { get; set; } = default!;

&nbsp;   

&nbsp;   \[Inject]

&nbsp;   private IListRenderer ListRenderer { get; set; } = default!;

&nbsp;   

&nbsp;   \[Inject]

&nbsp;   private IUserFilter UserFilter { get; set; } = default!;

}

```



\### 15.5 Command-Query Separation



```csharp

// ❌ BAD: Method does both command and query

public User CreateAndReturnUser(string name)

{

&nbsp;   var user = new User { Name = name };

&nbsp;   \_users.Add(user); // Command: modifies state

&nbsp;   return user;      // Query: returns value

}



// ✅ GOOD: Separate command and query

public void CreateUser(User user)

{

&nbsp;   \_users.Add(user); // Command only

}



public User? GetUserById(string id)

{

&nbsp;   return \_users.FirstOrDefault(u => u.Id == id); // Query only

}

```



\### 15.6 Fail Fast



```csharp

// ❌ BAD: Nested validation

public async Task ProcessMessageAsync(Message message)

{

&nbsp;   if (message != null)

&nbsp;   {

&nbsp;       if (!string.IsNullOrEmpty(message.Body))

&nbsp;       {

&nbsp;           if (message.Priority > 0)

&nbsp;           {

&nbsp;               // Process message (deeply nested)

&nbsp;               await \_service.ProcessAsync(message);

&nbsp;           }

&nbsp;       }

&nbsp;   }

}



// ✅ GOOD: Guard clauses (fail fast)

public async Task ProcessMessageAsync(Message message)

{

&nbsp;   if (message == null)

&nbsp;       throw new ArgumentNullException(nameof(message));

&nbsp;   

&nbsp;   if (string.IsNullOrEmpty(message.Body))

&nbsp;       throw new ArgumentException("Message body cannot be empty", nameof(message));

&nbsp;   

&nbsp;   if (message.Priority <= 0)

&nbsp;       throw new ArgumentException("Message priority must be positive", nameof(message));

&nbsp;   

&nbsp;   await \_service.ProcessAsync(message);

}

```



---



\## 16. Refactoring Guidelines



\### 16.1 When to Refactor



Refactor when you encounter:



1\. \*\*Duplicate code\*\* appearing in multiple places

2\. \*\*Long methods\*\* (>50 lines) or \*\*long classes\*\* (>300 lines)

3\. \*\*Large parameter lists\*\* (>5 parameters)

4\. \*\*Complex conditionals\*\* (deeply nested if/else)

5\. \*\*Comments explaining what code does\*\* (code should be self-explanatory)

6\. \*\*Shotgun surgery\*\* (one change requires changes in many places)

7\. \*\*Feature envy\*\* (method uses more features of another class than its own)



\### 16.2 Refactoring Techniques



\#### Extract Method



```csharp

// Before

public async Task ProcessOrderAsync(Order order)

{

&nbsp;   // Validate order

&nbsp;   if (order == null) throw new ArgumentNullException();

&nbsp;   if (order.Items.Count == 0) throw new InvalidOperationException();

&nbsp;   

&nbsp;   // Calculate total

&nbsp;   decimal total = 0;

&nbsp;   foreach (var item in order.Items)

&nbsp;   {

&nbsp;       total += item.Price \* item.Quantity;

&nbsp;   }

&nbsp;   order.Total = total;

&nbsp;   

&nbsp;   // Apply discount

&nbsp;   if (order.Customer.IsVip)

&nbsp;   {

&nbsp;       order.Total \*= 0.9m;

&nbsp;   }

&nbsp;   

&nbsp;   // Save order

&nbsp;   await \_repository.SaveAsync(order);

}



// After

public async Task ProcessOrderAsync(Order order)

{

&nbsp;   ValidateOrder(order);

&nbsp;   CalculateOrderTotal(order);

&nbsp;   ApplyDiscounts(order);

&nbsp;   await SaveOrderAsync(order);

}



private void ValidateOrder(Order order)

{

&nbsp;   if (order == null)

&nbsp;       throw new ArgumentNullException(nameof(order));

&nbsp;   

&nbsp;   if (order.Items.Count == 0)

&nbsp;       throw new InvalidOperationException("Order must have items");

}



private void CalculateOrderTotal(Order order)

{

&nbsp;   order.Total = order.Items.Sum(item => item.Price \* item.Quantity);

}



private void ApplyDiscounts(Order order)

{

&nbsp;   if (order.Customer.IsVip)

&nbsp;   {

&nbsp;       order.Total \*= 0.9m;

&nbsp;   }

}



private async Task SaveOrderAsync(Order order)

{

&nbsp;   await \_repository.SaveAsync(order);

}

```



\#### Extract Component



```razor

@\* Before: Large component \*@

<div class="user-management">

&nbsp;   <div class="header">

&nbsp;       <h1>User Management</h1>

&nbsp;       <button @onclick="AddUser">Add User</button>

&nbsp;   </div>

&nbsp;   

&nbsp;   <div class="filters">

&nbsp;       <input @bind="SearchTerm" placeholder="Search..." />

&nbsp;       <select @bind="RoleFilter">

&nbsp;           <option value="">All Roles</option>

&nbsp;           <option value="Admin">Admin</option>

&nbsp;           <option value="User">User</option>

&nbsp;       </select>

&nbsp;   </div>

&nbsp;   

&nbsp;   <div class="user-list">

&nbsp;       @foreach (var user in FilteredUsers)

&nbsp;       {

&nbsp;           <div class="user-card">

&nbsp;               <h3>@user.Name</h3>

&nbsp;               <p>@user.Email</p>

&nbsp;               <span>@user.Role</span>

&nbsp;               <button @onclick="() => EditUser(user)">Edit</button>

&nbsp;               <button @onclick="() => DeleteUser(user)">Delete</button>

&nbsp;           </div>

&nbsp;       }

&nbsp;   </div>

</div>



@\* After: Extracted components \*@

<div class="user-management">

&nbsp;   <UserManagementHeader OnAddUser="AddUser" />

&nbsp;   <UserFilters @bind-SearchTerm="SearchTerm" @bind-RoleFilter="RoleFilter" />

&nbsp;   <UserList Users="@FilteredUsers" OnEdit="EditUser" OnDelete="DeleteUser" />

</div>

```



\#### Replace Conditional with Polymorphism



```csharp

// Before

public decimal CalculateShipping(Order order)

{

&nbsp;   if (order.ShippingMethod == "Standard")

&nbsp;   {

&nbsp;       return order.Weight \* 0.5m;

&nbsp;   }

&nbsp;   else if (order.ShippingMethod == "Express")

&nbsp;   {

&nbsp;       return order.Weight \* 1.5m + 10;

&nbsp;   }

&nbsp;   else if (order.ShippingMethod == "Overnight")

&nbsp;   {

&nbsp;       return order.Weight \* 3m + 25;

&nbsp;   }

&nbsp;   return 0;

}



// After

public interface IShippingCalculator

{

&nbsp;   decimal Calculate(Order order);

}



public class StandardShipping : IShippingCalculator

{

&nbsp;   public decimal Calculate(Order order) => order.Weight \* 0.5m;

}



public class ExpressShipping : IShippingCalculator

{

&nbsp;   public decimal Calculate(Order order) => order.Weight \* 1.5m + 10;

}



public class OvernightShipping : IShippingCalculator

{

&nbsp;   public decimal Calculate(Order order) => order.Weight \* 3m + 25;

}



// Usage

public decimal CalculateShipping(Order order, IShippingCalculator calculator)

{

&nbsp;   return calculator.Calculate(order);

}

```



---



\## 17. Documentation Standards



\### 17.1 XML Documentation



```csharp

/// <summary>

/// Retrieves messages from the specified MSMQ queue.

/// </summary>

/// <param name="queuePath">The full path to the MSMQ queue (e.g., ".\\private$\\myqueue").</param>

/// <param name="maxMessages">The maximum number of messages to retrieve. Defaults to 100.</param>

/// <param name="cancellationToken">Token to cancel the operation.</param>

/// <returns>A collection of messages from the queue.</returns>

/// <exception cref="ArgumentException">Thrown when queuePath is null or empty.</exception>

/// <exception cref="QueueNotFoundException">Thrown when the specified queue does not exist.</exception>

/// <exception cref="UnauthorizedAccessException">Thrown when the user lacks permissions to access the queue.</exception>

/// <remarks>

/// This method will only retrieve messages without removing them from the queue.

/// For queue operations that modify state, use <see cref="DequeueMessagesAsync"/>.

/// </remarks>

/// <example>

/// <code>

/// var messages = await queueService.GetMessagesAsync(@".\\private$\\orders", maxMessages: 50);

/// </code>

/// </example>

public async Task<IEnumerable<QueueMessage>> GetMessagesAsync(

&nbsp;   string queuePath,

&nbsp;   int maxMessages = 100,

&nbsp;   CancellationToken cancellationToken = default)

{

&nbsp;   // Implementation

}

```



\### 17.2 README Files



Every feature folder should contain a README.md:



```markdown

\# Queue Management Components



\## Overview

Components for displaying and managing MSMQ queues and messages.



\## Components



\### QueueTreeView

Displays hierarchical view of queues.



\*\*Parameters:\*\*

\- `Queues` (List<Queue>) - List of queues to display

\- `OnQueueSelected` (EventCallback<Queue>) - Fired when queue is selected



\*\*Usage:\*\*

```razor

<QueueTreeView Queues="@availableQueues" OnQueueSelected="HandleQueueSelection" />

```



\### MessageList

Grid display of messages in a queue.



\*\*Parameters:\*\*

\- `Messages` (List<Message>) - Messages to display

\- `OnMessageSelected` (EventCallback<Message>) - Fired when message is clicked



\## Dependencies

\- MsmqService

\- QueueMonitorState



\## Related Documentation

\- \[MSMQ Overview](docs/msmq-overview.md)

\- \[Component Architecture](docs/architecture.md)

```



---



\## 18. Version Control Standards



\### 18.1 Commit Messages



Follow conventional commits format:



```

<type>(<scope>): <subject>



<body>



<footer>

```



\*\*Types:\*\*

\- `feat`: New feature

\- `fix`: Bug fix

\- `refactor`: Code refactoring

\- `docs`: Documentation

\- `style`: Formatting, missing semicolons, etc.

\- `test`: Adding tests

\- `chore`: Maintenance tasks



\*\*Examples:\*\*

```

feat(queue): add message export functionality



\- Add export to JSON format

\- Add export to XML format

\- Include message metadata in export



Closes #123

```



```

refactor(message-list): extract filter logic to service



Moved filtering logic from component to MessageFilterService

to improve testability and reusability.



Breaking change: MessageList now requires IMessageFilterService injection

```



\### 18.2 Branch Naming



```

feature/queue-message-export

bugfix/memory-leak-in-timer

refactor/split-large-component

docs/update-coding-standards

```



---



\## 19. Tools and Extensions



\### 19.1 Recommended VS Extensions



\- \*\*ReSharper\*\* or \*\*Rider\*\* - Code quality and refactoring

\- \*\*SonarLint\*\* - Code quality and security analysis

\- \*\*CodeMaid\*\* - Code cleanup and formatting

\- \*\*Roslynator\*\* - Additional code analyzers

\- \*\*Blazor Snippets\*\* - Code snippets for Blazor



\### 19.2 EditorConfig



Include `.editorconfig` in solution root:



```ini

root = true



\[\*]

charset = utf-8

end\_of\_line = crlf

trim\_trailing\_whitespace = true

insert\_final\_newline = true



\[\*.cs]

indent\_style = space

indent\_size = 4



\# Naming conventions

dotnet\_naming\_rule.interfaces\_should\_be\_pascal\_case\_prefixed\_with\_i.severity = warning

dotnet\_naming\_rule.interfaces\_should\_be\_pascal\_case\_prefixed\_with\_i.symbols = interface

dotnet\_naming\_rule.interfaces\_should\_be\_pascal\_case\_prefixed\_with\_i.style = begins\_with\_i



\# Code style rules

csharp\_prefer\_braces = true:warning

csharp\_prefer\_simple\_using\_statement = true:suggestion

dotnet\_sort\_system\_directives\_first = true



\[\*.razor]

indent\_style = space

indent\_size = 4

```



\### 19.3 Code Analysis Rules



Enable in project file:



```xml

<PropertyGroup>

&nbsp; <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

&nbsp; <EnableNETAnalyzers>true</EnableNETAnalyzers>

&nbsp; <AnalysisLevel>latest</AnalysisLevel>

&nbsp; <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

</PropertyGroup>

```



---



\## 20. Enforcement and Compliance



\### 20.1 Pre-Commit Hooks



Use Git hooks to enforce standards:



```bash

\#!/bin/bash

\# .git/hooks/pre-commit



\# Check file sizes

find . -name "\*.cs" -not -path "\*/obj/\*" -not -path "\*/bin/\*" | while read file; do

&nbsp;   lines=$(wc -l < "$file")

&nbsp;   if \[ $lines -gt 1000 ]; then

&nbsp;       echo "ERROR: $file exceeds 1000 lines ($lines lines)"

&nbsp;       exit 1

&nbsp;   fi

done

```



\### 20.2 CI/CD Integration



Add to build pipeline:



```yaml

\- name: Check File Sizes

&nbsp; run: |

&nbsp;   ./scripts/check-file-sizes.ps1

&nbsp;   

\- name: Run Code Analysis

&nbsp; run: |

&nbsp;   dotnet build --no-restore /p:TreatWarningsAsErrors=true

&nbsp;   

\- name: Run Tests

&nbsp; run: |

&nbsp;   dotnet test --no-build --verbosity normal

```



\### 20.3 Code Review Process



1\. \*\*Self-Review\*\*: Author reviews their own PR against this checklist

2\. \*\*Automated Checks\*\*: CI/CD validates compliance

3\. \*\*Peer Review\*\*: At least one team member reviews

4\. \*\*Approval\*\*: Required before merge



---



\## 21. Conclusion



These coding standards ensure:



✅ \*\*Maintainability\*\* - Easy to understand and modify  

✅ \*\*Testability\*\* - Code can be effectively tested  

✅ \*\*Scalability\*\* - Architecture supports growth  

✅ \*\*Consistency\*\* - Uniform code across team  

✅ \*\*Quality\*\* - High standards enforced  

✅ \*\*Performance\*\* - Optimized patterns used  



\### Questions or Clarifications



Contact the architecture team or raise an issue in the project repository.



---



\*\*Document Version\*\*: 1.0  

\*\*Last Review Date\*\*: October 18, 2025  

\*\*Next Review Date\*\*: January 18, 2026  

\*\*Owner\*\*: Development Team Lead

