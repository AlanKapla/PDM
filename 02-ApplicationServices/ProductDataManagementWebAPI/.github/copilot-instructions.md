# 🏗️ Product Data Management Web API – Coding Standards

> **Stack:** .NET 8 Web API | CQRS | Multi-tenancy | Repository Pattern  
> **Workspace:** `02-ApplicationServices/ProductDataManagementWebAPI/`

---

## 📖 Quick Reference Guide

| Topic | Key Rule | Details |
|-------|----------|---------|
| **CQRS Interfaces** | Commands → `IRequestCommand<T>` <br> Queries → `IRequestQuery<T>` | ❌ NEVER use `IRequest<T>` directly |
| **SaveChanges** | Automatic via `TransactionBehavior` | ⚠️ Call manually ONLY when you need FK Id |
| **Multi-tenancy** | ALWAYS filter by `TenantId` | Check in every Handler + Query |
| **Exceptions** | Use `ApiException` hierarchy | ❌ NO standard .NET exceptions in Handlers |
| **Exception Language** | English ONLY | ❌ NO Polish messages |
| **Validation** | Input → Validator <br> Business logic → Handler | Move existence checks to Validator when possible |
| **Soft Delete** | Filter `!IsDeleted` everywhere | Never use physical `Delete()` |
| **Async** | Always use `async/await` | ❌ NO `.Result`, `.Wait()` |
| **DateTime** | Always UTC | Use `DateTime.UtcNow` |
| **Comments** | Explain WHY, not WHAT | Code should be self-documenting |

---

## 📁 Project Structure

```
src/
├── Business/          # Interfaces, Web models (DTO), services, helpers, exceptions
├── CQRS/             # Command/Query handlers, validators (FluentValidation)
├── Entities/         # Domain entities, DbContext, EF Core configurations
├── Repositiories/    # Repository pattern implementation
└── WebApi/           # Controllers, middleware, extensions, authorization
```

---

## 🚨 CRITICAL RULES – What Copilot MUST Check

### 🔴 ALWAYS Flag These Issues

| ❌ Anti-Pattern | ✅ Correct Pattern |
|----------------|-------------------|
| `public record MyCommand : IRequest<Guid>` | `public record MyCommand : IRequestCommand<Guid>` |
| `throw new Exception("Error")` | `throw new ValidationApiException("Error message")` |
| `throw new ValidationApiException("Błąd walidacji")` | `throw new ValidationApiException("Validation error")` |
| No `TenantId` check in Handler | `if (entity.TenantId != currentUser.ActiveTenantId) throw new ForbiddenApiException(...)` |
| Query without `!IsDeleted` filter | `x => x.ProjectId == id && !x.IsDeleted` |
| Using `.Result` or `.Wait()` | `await someRepo.GetAsync(...)` |
| `DateTime.Now` | `DateTime.UtcNow` |
| Manual `SaveChangesAsync()` in simple Command | Let `TransactionBehavior` handle it automatically |

### 🟡 Review & Suggest Improvements

- Commands building complex DTO projections → Move to Query
- Queries modifying state → Move to Command
- Existence check in Handler when object is not used for logic → Move to Validator with `MustAsync`
- Duplicated validation/mapping logic → Extract to Helper/Service
- Controllers with business logic → Move to Handler
- Missing XML documentation on public API endpoints

---

## 🎯 Architecture Patterns

### 1. 🔐 Multi-tenancy (MANDATORY)

**Every entity related to a tenant MUST have `TenantId` property.**

```csharp
// ✅ GOOD - Always validate tenant isolation
public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken ct)
{
    Project? project = await projectRepo.GetFirstBySearch(
        p => p.Id == request.ProjectId && p.TenantId == request.TenantId
    );
    
    if (project == null)
        throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
    
    // Validate tenant isolation
    if (project.TenantId != currentUser.ActiveTenantId)
        throw new ForbiddenApiException("Cannot access project from another tenant");
    
    // ... business logic
}
```

**Rules:**
- ✅ Filter by `TenantId` in EVERY database query
- ✅ Validate `entity.TenantId == currentUser.ActiveTenantId` before modifications
- ❌ NEVER return or modify data from different tenant

---

### 2. 📨 CQRS Pattern

#### Interface Inheritance (CRITICAL)

```csharp
// ✅ GOOD - Use project-specific interfaces
public record CreateProjectCommand : IRequestCommand<Guid> { }
public record GetProjectDetailsQuery : IRequestQuery<ProjectDetailsWeb> { }

// ❌ BAD - Direct MediatR interface usage
public record CreateProjectCommand : IRequest<Guid> { } // NEVER DO THIS!
```

#### Commands – Change State

**Purpose:** Modify data, return simple results (`Unit`, `Guid`, or simple result DTO)

**Commands CAN read data ONLY for:**
- Business validation
- Loading entity for modification

**Commands MUST NOT:**
- Build complex DTO projections for UI (use Query instead)

```csharp
// ✅ GOOD - Command returns simple ID
public record CreateProjectCommand : IRequestCommand<Guid>
{
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken ct)
{
    Project project = new Project
    {
        TenantId = request.TenantId,
        Name = request.Name,
        CreatedByUserId = currentUser.Id
    };
    
    await projectRepo.Insert(project);
    // SaveChanges is called automatically by TransactionBehavior
    return project.Id;
}
```

#### Queries – Read Only

**Purpose:** Fetch data, return Web models (DTOs) tailored for UI/API needs

**Queries MUST:**
- Be read-only (ZERO state modification)
- Return Web models (`*Web` suffix)
- Contain projection and mapping logic

**Queries MUST NOT:**
- Call `SaveChangesAsync()`
- Modify any entity

```csharp
// ✅ GOOD - Query returns Web model
public record GetProjectDetailsQuery : IRequestQuery<ProjectDetailsWeb>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
}

public async Task<ProjectDetailsWeb> Handle(GetProjectDetailsQuery request, CancellationToken ct)
{
    Project? project = await projectRepo.GetFirstBySearch(
        p => p.Id == request.ProjectId && p.TenantId == request.TenantId && !p.IsDeleted,
        include => include.Include(p => p.Members).Include(p => p.Groups)
    );
    
    if (project == null)
        throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
    
    // Map to Web model
    return new ProjectDetailsWeb
    {
        Id = project.Id,
        Name = project.Name,
        Members = project.Members.Select(m => new ProjectMemberWeb { ... }).ToList()
    };
}
```

---

### 3. 💾 SaveChanges Rules (CRITICAL)

**DEFAULT BEHAVIOR:** `SaveChangesAsync()` is **AUTOMATICALLY** called by `TransactionBehavior` for ALL `IRequestCommand`.

#### When NOT to call SaveChangesAsync (STANDARD CASE)

```csharp
// ✅ GOOD - TransactionBehavior handles SaveChanges
public async Task<Unit> Handle(DeleteProjectFileCommand request, CancellationToken ct)
{
    ProjectFile? file = await fileRepo.GetFirstBySearch(...);
    
    file.IsDeleted = true;
    file.DeletedAt = DateTime.UtcNow;
    await fileRepo.Update(file);
    
    // DON'T call SaveChanges - TransactionBehavior does it automatically
    return Unit.Value;
}
```

#### When TO call SaveChangesAsync (EXCEPTION CASES)

**Call manually ONLY when:**
1. You need the generated `Id` from database
2. That `Id` is required as Foreign Key in related entity

```csharp
// ✅ GOOD - Manual SaveChanges when FK Id is needed
public async Task<Guid> Handle(UploadProjectFilesCommand request, CancellationToken ct)
{
    ProjectFile projectFile = new ProjectFile { ... };
    await projectFileRepo.Insert(projectFile);
    
    // MUST save NOW because we need projectFile.Id for the version
    await projectFileRepo.SaveChangesAsync(ct);
    
    ProjectFileVersion firstVersion = new ProjectFileVersion
    {
        ProjectFileId = projectFile.Id, // ← Requires saved Id
        VersionNumber = 1,
        // ...
    };
    await projectFileVersionRepo.Insert(firstVersion);
    
    // TransactionBehavior will call SaveChanges again at the end
    return projectFile.Id;
}
```

**How TransactionBehavior Works:**
```csharp
// Automatic transaction wrapping for all IRequestCommand
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
    if (request is IRequestCommand<TResponse>)
    {
        var strategy = appDbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await appDbContext.Database.BeginTransactionAsync(ct);
            var response = await next(); // Execute Handler
            await appDbContext.SaveChangesAsync(ct); // Automatic SaveChanges
            await transaction.CommitAsync(ct);
            return response;
        });
    }
    return await next(ct);
}
```

**Rules:**
- ✅ DON'T call `SaveChangesAsync()` in standard cases
- ✅ Call `SaveChangesAsync()` ONLY when you need Id for FK
- ❌ NEVER call `SaveChangesAsync()` in Query

---

### 4. 🚫 Exception Handling – ApiException Hierarchy

**Use ONLY `ApiException` hierarchy in Handlers.**

#### Exception Types

```csharp
// Validation errors (business logic, not input validation)
throw new ValidationApiException("File extension mismatch. Expected: .pdf, received: .docx");

// Resource not found
throw new NotFoundApiException(
    objectType: nameof(ProjectFile),
    objectId: request.FileId.ToString(),
    message: "File does not exist or has been deleted" // optional
);

// Access denied (multi-tenancy, role check)
throw new ForbiddenApiException("Cannot access project from another tenant");

// Authentication failure
throw new UnauthorizedApiException("Invalid email or password");

// Resource conflict (duplicate, uniqueness violation)
throw new ConflictApiException("User with this email already exists");
```

#### Rules

- ✅ Use `ApiException` hierarchy exclusively
- ✅ **Write ALL exception messages in ENGLISH**
- ❌ NEVER use `throw new Exception()`
- ❌ NEVER use standard .NET exceptions (`ArgumentException`, `InvalidOperationException`) in Handlers
- ❌ NEVER write exception messages in Polish

```csharp
// ✅ GOOD
throw new ValidationApiException("File size cannot exceed 50 MB");

// ❌ BAD - Polish message
throw new ValidationApiException("Rozmiar pliku nie może przekraczać 50 MB");

// ❌ BAD - Standard exception
throw new ArgumentException("Invalid file size");
```

---

### 5. ✅ Validation – Validators vs Handlers

#### Division of Responsibility

| Validator (FluentValidation) | Handler |
|------------------------------|---------|
| Input structure & format | Business logic requiring DB access |
| Required fields & constraints | Checking related entity existence |
| File size, string length, email format | Multi-tenant isolation verification |
| Simple business rules (no DB) | Domain-specific rules (e.g., file extension matching) |

#### FluentValidation Validator Example

```csharp
public class UploadProjectFileVersionCommandValidator : AbstractValidator<UploadProjectFileVersionCommand>
{
    public UploadProjectFileVersionCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required");
        
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(f => f.Length > 0).WithMessage("File cannot be empty")
            .Must(f => f.Length <= 52428800).WithMessage("File size cannot exceed 50 MB");
        
        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Comment cannot exceed 2000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}
```

#### Handler Validation Example

```csharp
public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken ct)
{
    // Validate entity existence (requires DB access)
    ProjectFile? projectFile = await projectFileRepo.GetFirstBySearch(
        pf => pf.Id == request.FileId && 
              pf.TenantId == request.TenantId && 
              !pf.IsDeleted
    );
    
    if (projectFile == null)
        throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
    
    // Validate business logic (file extension matching)
    string originalExtension = Path.GetExtension(projectFile.FileName).ToLowerInvariant();
    string newFileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
    
    if (originalExtension != newFileExtension)
    {
        throw new ValidationApiException(
            $"Extension mismatch. Expected: {originalExtension}, received: {newFileExtension}");
    }
    
    // ... business logic
}
```

#### CRITICAL: Move Existence Checks to Validator

**If you fetch an object from DB ONLY to check if it exists** (without using its properties), **MOVE that validation to Validator**.

```csharp
// ❌ BAD - Existence check in Handler when object is not used
public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken ct)
{
    // We fetch ONLY to check existence - should be in Validator!
    ProjectFileVersion? version = await versionRepo.GetFirstBySearch(
        v => v.Id == request.VersionId && v.TenantId == request.TenantId && !v.IsDeleted
    );
    
    if (version == null)
        throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
    
    // We create comment WITHOUT using the fetched version object
    var comment = new FileVersionComment
    {
        ProjectFileVersionId = request.VersionId, // Using Id from request, not from fetched object
        Comment = request.Comment
    };
    
    await commentRepo.Insert(comment);
    return Unit.Value;
}

// ✅ GOOD - Existence check moved to Validator
public class AddFileVersionCommentCommandValidator : AbstractValidator<AddFileVersionCommentCommand>
{
    private readonly IRepository<ProjectFileVersion> versionRepo;
    
    public AddFileVersionCommentCommandValidator(IRepository<ProjectFileVersion> versionRepo)
    {
        this.versionRepo = versionRepo;
        
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("VersionId is required")
            .MustAsync(async (command, versionId, ct) => await VersionExists(command, versionId, ct))
            .WithMessage("File version does not exist or has been deleted");
    }
    
    private async Task<bool> VersionExists(AddFileVersionCommentCommand command, Guid versionId, CancellationToken ct)
    {
        return await versionRepo.GetFirstBySearch(
            v => v.Id == versionId && v.TenantId == command.TenantId && !v.IsDeleted
        ) != null;
    }
}

// Handler is now simpler - knows version exists thanks to Validator
public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken ct)
{
    var comment = new FileVersionComment
    {
        ProjectFileVersionId = request.VersionId,
        Comment = request.Comment,
        CreatedByUserId = currentUser.Id,
        TenantId = request.TenantId
    };
    
    await commentRepo.Insert(comment);
    return Unit.Value;
}
```

**When NOT to move to Validator:**

```csharp
// ✅ GOOD - Stays in Handler because we USE properties of fetched object
public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken ct)
{
    ProjectFile? projectFile = await projectFileRepo.GetFirstBySearch(
        pf => pf.Id == request.FileId && pf.TenantId == request.TenantId && !pf.IsDeleted
    );
    
    if (projectFile == null)
        throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
    
    // We USE properties of fetched object - must stay in Handler
    string originalExtension = Path.GetExtension(projectFile.FileName).ToLowerInvariant();
    
    var version = new ProjectFileVersion
    {
        ProjectFileId = projectFile.Id,
        VersionNumber = projectFile.CurrentVersionNumber + 1, // ← Using properties
        // ...
    };
}
```

**Summary:**
- ✅ Move to Validator: checking **if exists** only (NotFound check)
- ❌ Stay in Handler: using **properties/navigation properties** for business logic
- ❌ Stay in Handler: checking **complex business conditions** requiring access to multiple related entities

---

### 6. 🔒 Authorization – Policy-based

**Use Policy-based authorization instead of `[Authorize(Roles = "...")]`**

#### Policy Definition

```csharp
// WebApi/Constants/Policies.cs
public static class Policies
{
    public const string TenantMember = "TenantMember";
    public const string TenantAdmin = "TenantAdmin";
    public const string ProjectMember = "ProjectMember";
    public const string SystemAdmin = "SystemAdmin";
}
```

#### Policy Registration

```csharp
// ServiceCollectionExtensions.cs
services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.ProjectMember, policy =>
        policy.Requirements.Add(new ProjectAccessRequirement()));
    
    options.AddPolicy(Policies.TenantAdmin, policy =>
        policy.RequireClaim(ClaimNames.ActiveTenantRole, TenantRole.Admin.ToString()));
});
```

#### Usage in Controllers

```csharp
[HttpPost("versions")]
[Authorize(Policy = Policies.ProjectMember)]
public async Task<IActionResult> UploadFileVersion(...)
```

#### Custom Authorization Handler

```csharp
public class ProjectAccessRequirement : IAuthorizationRequirement { }

public class ProjectAccessHandler : AuthorizationHandler<ProjectAccessRequirement>
{
    private readonly ICurrentUser currentUser;
    private readonly IHttpContextAccessor httpContext;
    
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectAccessRequirement requirement)
    {
        Guid projectId = GetProjectIdFromRoute();
        
        bool hasAccess = currentUser.Projects?.Any(p => p.ProjectId == projectId) ?? false;
        
        if (hasAccess)
            context.Succeed(requirement);
        
        return Task.CompletedTask;
    }
}
```

**Rules:**
- ❌ NEVER use `[Authorize(Roles = "Admin")]`
- ✅ ALWAYS use `[Authorize(Policy = Policies.XYZ)]`
- Controller checks high-level resource access (project, tenant)
- Handler additionally verifies multi-tenant isolation and detailed permissions

---

## 🏗️ Code Organization

### CQRS Folder Structure

Each feature in `CQRS/` has dedicated folder:

```
CQRS/
└── Files/
    └── UploadProjectFileVersion/
        ├── UploadProjectFileVersionCommand.cs           # Request model (record)
        ├── UploadProjectFileVersionCommandHandler.cs    # Business logic
        └── UploadProjectFileVersionCommandValidator.cs  # FluentValidation rules
```

### Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Command | `{Feature}{Action}Command` | `UploadProjectFileVersionCommand` |
| Query | `{Feature}{Action}Query` | `GetProjectDetailsQuery` |
| Handler | `{Feature}{Action}CommandHandler` | `UploadProjectFileVersionCommandHandler` |
| Validator | `{Feature}{Action}CommandValidator` | `UploadProjectFileVersionCommandValidator` |
| Web Model (DTO) | `{Entity}Web` | `ProjectFileWeb`, `ProjectDetailsWeb` |

### Command/Query as record

Use `record` for immutability:

```csharp
public record UploadProjectFileVersionCommand : IRequestCommand<Unit>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid FileId { get; init; }
    public IFormFile File { get; init; } = null!;
    public string? Comment { get; init; }
}
```

### Web Models (DTOs)

Defined in `Business/Interfaces/WebModels/`:

```
Business/Interfaces/WebModels/
├── Files/
│   ├── ProjectFileWeb.cs
│   ├── ProjectFileVersionWeb.cs
│   └── SharedProjectFileWeb.cs
├── Projects/
│   └── ProjectDetailsWeb.cs
└── Users/
    └── UserWeb.cs
```

**Rules:**
- Suffix `Web` for all DTOs returned by API
- Should contain only data needed for UI/client
- ❌ NEVER expose domain entities directly

---

## 🗄️ Entity Framework Core – Repository Pattern

### Repository Interface

```csharp
public interface IRepository<T> where T : class
{
    Task Insert(T entity);
    Task Update(T entity);
    Task Delete(T entity);
    Task<T?> GetFirstBySearch(Expression<Func<T, bool>> predicate, 
        params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
    Task<IEnumerable<T>> GetBySearch(Expression<Func<T, bool>> predicate, 
        params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### Usage Rules

#### Always use `.Include()` for navigation properties

```csharp
var project = await projectRepo.GetFirstBySearch(
    p => p.Id == projectId,
    include => include
        .Include(p => p.Members)
        .Include(p => p.Groups)
);
```

#### Soft Delete – NEVER physical delete

```csharp
// ✅ GOOD - Soft delete
projectFile.IsDeleted = true;
projectFile.DeletedAt = DateTime.UtcNow;
await projectFileRepo.Update(projectFile);

// ❌ BAD - Physical delete
await projectFileRepo.Delete(projectFile);
```

#### Always filter soft-deleted entities

```csharp
var files = await projectFileRepo.GetBySearch(
    f => f.ProjectId == projectId && !f.IsDeleted // ← ALWAYS check !IsDeleted
);
```

---

## 🛠️ Helpers & Services

### Helpers (Static Utilities)

Located in `Business/Interfaces/Helpers/`:

```csharp
// FileHelper.cs
public static class FileHelper
{
    public static string NormalizePackageNameForBlobPath(string packageName)
    {
        return packageName.Replace(" ", "_").Replace("/", "-");
    }
    
    public static bool IsAllowedExtension(string fileName, string[] allowedExtensions)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext);
    }
}
```

### Services (Domain/Infrastructure Logic)

Interface: `Business/Interfaces/Services/`  
Implementation: `Business/Implementation/Services/`

```csharp
// Interface
public interface IBlobStorageService
{
    Task UploadAsync(string container, string path, Stream stream, 
        string contentType, CancellationToken ct);
    Task DeleteAsync(string container, string path, CancellationToken ct);
}

// Implementation
public class BlobStorageService : IBlobStorageService
{
    // ... implementation
}
```

**DI Registration:**

```csharp
// WebApi/Extensions/ServiceCollectionExtensions.cs
services.AddScoped<IBlobStorageService, BlobStorageService>();
```

---

## ⚡ Best Practices

### Async/Await

- ✅ ALL IO operations MUST be async (database, API, file system, blob storage)
- ✅ ALWAYS pass `CancellationToken`
- ❌ NEVER use `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` in Handlers
  - Exception: `CurrentUser` (scoped per request, cached)

```csharp
// ✅ GOOD
var user = await userRepo.GetFirstBySearch(u => u.Id == id, ct);

// ❌ BAD
var user = userRepo.GetFirstBySearch(u => u.Id == id).Result;
```

### DateTime

- ✅ ALWAYS use `DateTime.UtcNow`
- ❌ NEVER use `DateTime.Now`

```csharp
// ✅ GOOD
CreatedAt = DateTime.UtcNow

// ❌ BAD
CreatedAt = DateTime.Now
```

### Null-Safety

- ✅ Use `?` for nullable types
- ✅ Check null before usage

```csharp
public string? Comment { get; init; }

if (projectFile == null)
    throw new NotFoundApiException(...);

// Now safe to use projectFile
```

### MediatR Unit

When Command returns nothing, use `Unit`:

```csharp
public record DeleteProjectCommand : IRequestCommand<Unit> { }

public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken ct)
{
    // ... logic
    return Unit.Value;
}
```

### ICurrentUser – User Context

Interface provides info about authenticated user:

```csharp
public interface ICurrentUser
{
    Guid Id { get; }                        // User ID from JWT claims
    string FirstName { get; }               // Lazy-loaded from DB
    string LastName { get; }                // Lazy-loaded from DB
    string Email { get; }                   // From JWT claims
    Guid? ActiveTenantId { get; }           // From TenantPreferencesProfile (DB)
    TenantRole? ActiveTenantRole { get; }   // From JWT claims
    SystemRole SystemRole { get; }          // From JWT claims
    bool IsAuthenticated { get; }
}
```

**Usage in Handlers:**

```csharp
public async Task<Unit> Handle(Command request, CancellationToken ct)
{
    Guid userId = currentUser.Id;
    
    if (currentUser.ActiveTenantId != request.TenantId)
        throw new ForbiddenApiException("Cannot access this tenant's resources");
    
    var entity = new SomeEntity
    {
        CreatedByUserId = currentUser.Id,
        TenantId = request.TenantId
    };
}
```

**Note:** `CurrentUser` is **scoped per request** and caches DB data, so it's safe to access properties multiple times.

---

## 💬 Comments – Minimalism Rule

### ❌ DON'T write comments for:

```csharp
// ❌ BAD - Obvious operation
// Get user from database
User? user = await userRepo.GetFirstBySearch(u => u.Id == userId);

// ❌ BAD - Self-explanatory code
// Check if file exists
if (projectFile == null)
```

### ✅ DO write comments when:

```csharp
// ✅ GOOD - Explains WHY
// CurrentVersionId is set AFTER SaveChanges because version must be saved first
// to get valid Id and avoid FK violations
projectFile.CurrentVersionId = versionId;

// ✅ GOOD - Documents non-trivial business decision
// File extension must match original due to versioning system
// and preview rendering requirements
if (originalExtension != newFileExtension)
{
    throw new ValidationApiException(...);
}

// ✅ GOOD - Warns about pitfalls
// WARNING: ICurrentUser properties are not async, so we fetch data once
// with blocking call and cache in object (scoped per request) - minimizes cost
User? user = userRepo.GetFirstBySearch(u => u.Id == userId).GetAwaiter().GetResult();
```

### XML Documentation Comments

Use for **public API endpoints** in controllers:

```csharp
/// <summary>
/// Upload a new version of an existing project file
/// </summary>
/// <param name="tenantId">Tenant identifier</param>
/// <param name="projectId">Project identifier</param>
/// <param name="fileId">File identifier</param>
/// <returns>NoContent on success</returns>
[HttpPost("versions")]
[Authorize(Policy = Policies.ProjectMember)]
public async Task<IActionResult> UploadFileVersion(...)
```

---

## 🎮 Controllers – REST API

### Routing & Conventions

- **Route template**: `api/tenants/{tenantId}/projects/{projectId}/[controller]`
- **HTTP methods**: `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`
- **Status codes**:
  - `200 OK` – GET returns data
  - `201 Created` – POST creates resource (return `CreatedAtAction` with ID)
  - `204 NoContent` – POST/PUT/DELETE without response body
  - `400 BadRequest` – Validation error
  - `404 NotFound` – Resource not found
  - `403 Forbidden` – Access denied

### Controller Example

```csharp
[Route("api/tenants/{tenantId}/projects/{projectId}/[controller]")]
[ApiController]
public class FileController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>
    /// Upload a new version of an existing project file
    /// </summary>
    [HttpPost("versions")]
    [Authorize(Policy = Policies.ProjectMember)]
    [RequestSizeLimit(52428800)] // 50 MB
    public async Task<IActionResult> UploadFileVersion(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        [FromForm] UploadProjectFileVersionCommand command)
    {
        command = command with { TenantId = tenantId, ProjectId = projectId };
        await Send(command);
        return NoContent();
    }
}
```

**Rules:**
- Controller is **thin layer** – only routing and authorization
- Business logic in **Handler**
- Set route parameters (`tenantId`, `projectId`) in controller:
  ```csharp
  command = command with { TenantId = tenantId, ProjectId = projectId };
  ```

---

## 🤖 What Copilot Should Flag & Suggest

### 🔴 Always Flag (Block/Error Level)

1. **Wrong CQRS interface inheritance**
   - Issue: `public record MyCommand : IRequest<Guid>`
   - Fix: `public record MyCommand : IRequestCommand<Guid>`

2. **Polish exception messages**
   - Issue: `throw new ValidationApiException("Błąd walidacji")`
   - Fix: `throw new ValidationApiException("Validation error")`

3. **Missing TenantId validation**
   - Issue: No `TenantId` check in Handler
   - Fix: Add `if (entity.TenantId != currentUser.ActiveTenantId) throw new ForbiddenApiException(...)`

4. **Missing !IsDeleted filter**
   - Issue: Query without soft-delete check
   - Fix: Add `&& !x.IsDeleted` to predicate

5. **Blocking async calls**
   - Issue: `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`
   - Fix: `await someRepo.GetAsync(...)`

6. **Using DateTime.Now**
   - Issue: `CreatedAt = DateTime.Now`
   - Fix: `CreatedAt = DateTime.UtcNow`

7. **Standard exceptions in Handlers**
   - Issue: `throw new Exception("Error")`
   - Fix: `throw new ValidationApiException("Error message")`

8. **Manual SaveChangesAsync in simple Command**
   - Issue: SaveChanges called in Handler where TransactionBehavior should handle it
   - Fix: Remove manual SaveChangesAsync call, let TransactionBehavior handle it

### 🟡 Suggest Improvements (Warning Level)

1. **Complex DTO projections in Command**
   - Suggest: Move to Query

2. **State modification in Query**
   - Suggest: Move to Command

3. **Existence check in Handler when object not used**
   - Suggest: Move to Validator with `MustAsync`

4. **Duplicated validation/mapping logic**
   - Suggest: Extract to Helper/Service

5. **Business logic in Controller**
   - Suggest: Move to Handler

6. **Missing XML documentation on public API endpoints**
   - Suggest: Add summary and param tags

7. **Manual SaveChangesAsync in simple Command**
   - Suggest: Remove (TransactionBehavior handles it)

8. **Missing includes for navigation properties**
   - Suggest: Add `.Include()` for related entities
