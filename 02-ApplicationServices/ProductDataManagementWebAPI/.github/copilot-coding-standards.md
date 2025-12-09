# 🏗️ Standardy kodowania – Product Data Management Web API

> **Kontekst projektu:** .NET 8 Web API z architekturą CQRS, multi-tenancy i pattern Repository  
> **Workspace:** `02-ApplicationServices/ProductDataManagementWebAPI/`

## 📁 Struktura projektu

```
src/
├── Business/          # Interfejsy, web modele (DTO), serwisy, helpery, wyjątki
├── CQRS/             # Command/Query handlers, validators
├── Entities/         # Encje domenowe, DbContext, konfiguracje EF Core
├── Repositiories/    # Repository pattern implementacja
└── WebApi/           # Controllers, middleware, extensions, autoryzacja
```

---

## 📋 Architektura i wzorce projektowe

### 1. **Multi-tenancy**
- **Każda encja domenowa** związana z danym najemcą (tenant) MUSI posiadać właściwość `TenantId` typu `Guid`
- **Walidacja izolacji tenant** jest OBOWIĄZKOWA w każdym Command/Query Handler:
  ```csharp
  if (entity.TenantId != currentUser.ActiveTenantId)
  {
      throw new ForbiddenApiException("Access denied to this tenant's resources");
  }
  ```
- **Wszystkie zapytania** do bazy danych MUSZĄ filtrować po `TenantId`:
  ```csharp
  await repo.GetBySearch(x => x.TenantId == request.TenantId && x.Id == request.Id)
  ```
- **NIE WOLNO** zwracać danych z innego tenanta ani zezwalać na modyfikacje zasobów spoza aktywnego tenanta użytkownika

### 2. **CQRS (Command Query Responsibility Segregation)**

#### Command/Query Interface Inheritance
- **Commands** MUSZĄ dziedziczyć po `IRequestCommand<TResponse>`
- **Queries** MUSZĄ dziedziczyć po `IRequestQuery<TResponse>`
- **NIE WOLNO** używać bezpośrednio `IRequest<T>` z MediatR

Przykład:
```csharp
// GOOD - Command
public record CreateProjectCommand : IRequestCommand<Guid>
{
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
}

// GOOD - Query
public record GetProjectDetailsQuery : IRequestQuery<ProjectDetailsWeb>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
}

// BAD - bezpośrednie użycie IRequest
public record CreateProjectCommand : IRequest<Guid> // ❌ Błąd!
```

#### Commands (zmieniają stan)
- Powinny zwracać **proste wyniki**: `Unit`, `Guid` (ID nowo utworzonego obiektu), lub prostą strukturę wynikową
- Mogą odczytywać dane TYLKO w celu:
  - Walidacji biznesowej
  - Pobrania encji do modyfikacji
- **NIE WOLNO** budować rozbudowanych projekcji DTO/Web modeli w Command – do tego służą Query
- Przykład:
  ```csharp
  public record CreateProjectCommand : IRequestCommand<Guid> { ... }
  
  public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken ct)
  {
      Project project = new Project { ... };
      await projectRepo.Insert(project);
      // SaveChanges jest automatycznie wywołane przez TransactionBehavior
      return project.Id; // Zwracamy tylko ID
  }
  ```

#### Queries (odczyt danych)
- Służą **wyłącznie do odczytu** – ZERO modyfikacji stanu
- Zwracają **Web modele (DTO)** dopasowane do potrzeb UI/API
- Powinny zawierać logikę projekcji i mapowania encji na DTO
- **NIE WOLNO** wywoływać `SaveChangesAsync()` w Query
- Przykład:
  ```csharp
  public record GetProjectDetailsQuery : IRequestQuery<ProjectDetailsWeb> { ... }
  
  public async Task<ProjectDetailsWeb> Handle(GetProjectDetailsQuery request, CancellationToken ct)
  {
      Project? project = await projectRepo.GetFirstBySearch(
          p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
          include => include.Include(p => p.Members)
      );
      
      if (project == null)
          throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
      
      return new ProjectDetailsWeb
      {
          Id = project.Id,
          Name = project.Name,
          // ... mapping
      };
  }
  ```

#### SaveChanges – zasady wywoływania
**KRYTYCZNA ZASADA:** `SaveChangesAsync()` jest **AUTOMATYCZNIE** wywoływane przez `TransactionBehavior` dla wszystkich `IRequestCommand`.

**NIE WYWOŁUJ `SaveChangesAsync()` w Handler CHYBA ŻE:**
- Musisz zapisać encję, aby uzyskać `Id` z bazy (auto-generated)
- To `Id` jest **wymagane** do zapisania innych powiązanych encji (Foreign Key)

```csharp
// PRZYKŁAD 1: BEZ SaveChanges (standard - obsługuje TransactionBehavior)
public async Task<Unit> Handle(DeleteProjectFileCommand request, CancellationToken ct)
{
    ProjectFile? projectFile = await projectFileRepo.GetFirstBySearch(...);
    
    projectFile.IsDeleted = true;
    projectFile.DeletedAt = DateTime.UtcNow;
    await projectFileRepo.Update(projectFile);
    
    // NIE WYWOŁUJ SaveChanges - zrobi to TransactionBehavior
    return Unit.Value;
}

// PRZYKŁAD 2: BEZ SaveChanges - pojedyncza operacja
public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken ct)
{
    ProjectFileVersionComment comment = new ProjectFileVersionComment { ... };
    await commentRepo.Insert(comment);
    
    // NIE WYWOŁUJ SaveChanges - zrobi to TransactionBehavior
    return Unit.Value;
}

// PRZYKŁAD 3: Z SaveChanges - gdy potrzebujemy Id dla FK
public async Task<Guid> Handle(UploadProjectFilesCommand request, CancellationToken ct)
{
    ProjectFile projectFile = new ProjectFile { ... };
    await projectFileRepo.Insert(projectFile);
    
    // MUSIMY zapisać TERAZ, bo potrzebujemy projectFile.Id dla wersji
    await projectFileRepo.SaveChangesAsync(ct);
    
    ProjectFileVersion firstVersion = new ProjectFileVersion
    {
        ProjectFileId = projectFile.Id, // ← To wymaga zapisanego Id
        // ...
    };
    await projectFileVersionRepo.Insert(firstVersion);
    
    // TransactionBehavior wywoła SaveChanges na końcu ponownie
    return projectFile.Id;
}

// PRZYKŁAD 4: Z SaveChanges - aktualizacja FK po zapisaniu
public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken ct)
{
    ProjectFile? projectFile = await projectFileRepo.GetFirstBySearch(...);
    
    ProjectFileVersion version = new ProjectFileVersion { ... };
    await projectFileVersionRepo.Insert(version);
    
    // MUSIMY zapisać TERAZ, aby uzyskać version.Id
    await projectFileVersionRepo.SaveChangesAsync(ct);
    
    projectFile.CurrentVersionId = version.Id; // ← Wymaga zapisanego version.Id
    await projectFileRepo.Update(projectFile);
    
    // TransactionBehavior wywoła SaveChanges na końcu ponownie
    return Unit.Value;
}
```

**Zasady:**
- ✅ **NIE WYWOŁUJ** `SaveChangesAsync()` w standardowych przypadkach – `TransactionBehavior` robi to automatycznie
- ✅ Wywołuj `SaveChangesAsync()` **TYLKO** gdy potrzebujesz `Id` dla Foreign Key w kolejnej encji
- ❌ **NIE WOLNO** wywoływać `SaveChangesAsync()` w Query
- ⚠️ `TransactionBehavior` opakowuje wszystkie `IRequestCommand` w transakcję i automatycznie wywołuje `SaveChanges` + `Commit`

**TransactionBehavior - jak działa:**
```csharp
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
    if (request is IRequestCommand<TResponse>)
    {
        var strategy = appDbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await appDbContext.Database.BeginTransactionAsync(ct);
            var response = await next(); // Wykonanie Handler
            await appDbContext.SaveChangesAsync(ct); // Automatyczne SaveChanges
            await transaction.CommitAsync(ct);
            return response;
        });
    }
    return await next(ct);
}
```

### 3. **Obsługa wyjątków – ApiException i pochodne**

#### Hierarchia wyjątków
Projekt definiuje bazową klasę `ApiException` z następującymi pochodnymi:

```csharp
// Bazowa klasa
public class ApiException(ApiExceptionReason reason, string? message, 
    string? objectType = null, string? objectId = null) : Exception(message)

// Dedykowane klasy pochodne
public class ValidationApiException(string message) 
    : ApiException(ApiExceptionReason.ValidationError, message)

public class NotFoundApiException(string objectType, string objectId, string? message = null)
    : ApiException(ApiExceptionReason.NotFound, message ?? $"{objectType} with ID {objectId} not found", objectType, objectId)

public class UnauthorizedApiException(string? message = null)
    : ApiException(ApiExceptionReason.Unauthorized, message ?? "Unauthorized")

public class ForbiddenApiException(string? message = null)
    : ApiException(ApiExceptionReason.Forbidden, message ?? "Access denied")

public class ConflictApiException(string? message = null)
    : ApiException(ApiExceptionReason.Conflict, message ?? "Resource conflict")
```

#### Kiedy używać których wyjątków

- **`ValidationApiException`** – błędy walidacji logiki biznesowej (NIE walidacji wejścia – to jest w Validator)
  ```csharp
  if (originalExtension != newFileExtension)
  {
      throw new ValidationApiException(
          $"The new version must have the same extension. Expected: {originalExtension}, received: {newFileExtension}");
  }
  ```

- **`NotFoundApiException`** – zasób nie istnieje lub został usunięty
  ```csharp
  if (projectFile == null)
  {
      throw new NotFoundApiException(
          objectType: nameof(ProjectFile),
          objectId: request.FileId.ToString(),
          message: $"File with ID {request.FileId} does not exist or has been deleted");
  }
  ```

- **`ForbiddenApiException`** – użytkownik nie ma uprawnień do zasobu (multi-tenancy, brak roli, itp.)
  ```csharp
  if (project.TenantId != currentUser.ActiveTenantId)
  {
      throw new ForbiddenApiException("Cannot access project from another tenant");
  }
  ```

- **`UnauthorizedApiException`** – brak autentykacji lub nieprawidłowe dane logowania
  ```csharp
  if (!passwordHasher.VerifyPassword(password, user.PasswordHash))
  {
      throw new UnauthorizedApiException("Invalid email or password");
  }
  ```

- **`ConflictApiException`** – konflikt zasobów (duplikacja, naruszenie unikalności)
  ```csharp
  if (existingUser != null)
  {
      throw new ConflictApiException($"User with email {request.Email} already exists");
  }
  ```

#### Zasady ogólne
- **NIE UŻYWAJ** `throw new Exception()` ani standardowych wyjątków .NET (`ArgumentException`, `InvalidOperationException`) w handlerach
- Middleware `ApiExceptionMiddleware` automatycznie konwertuje `ApiException` na odpowiedni status HTTP
- Wszystkie wyjątki domenowe/biznesowe MUSZĄ dziedziczyć po `ApiException`
- **WSZYSTKIE WYJĄTKI MUSZĄ BYĆ PISANE WYŁĄCZNIE W JĘZYKU ANGIELSKIM**
  ```csharp
  // GOOD ✅
  throw new ValidationApiException("File extension mismatch. Expected .pdf, received .docx");
  
  // BAD ❌
  throw new ValidationApiException("Rozszerzenie pliku nie pasuje. Oczekiwano .pdf, otrzymano .docx");
  ```

### 4. **DRY (Don't Repeat Yourself)**

#### Kiedy wydzielać kod współny

- **Helper classes** – metody narzędziowe używane w wielu miejscach
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

- **Serwisy domenowe** – logika biznesowa współdzielona między wieloma handlerami
  ```csharp
  public interface INotificationService
  {
      Task SendProjectInvitationAsync(Guid userId, Guid projectId, CancellationToken ct);
  }
  ```

- **Walidatory współdzielone** – wspólne reguły walidacji
  ```csharp
  public static class CommonValidators
  {
      public static IRuleBuilderOptions<T, Guid> MustBeValidTenant<T>(
          this IRuleBuilder<T, Guid> ruleBuilder)
      {
          return ruleBuilder
              .NotEmpty().WithMessage("TenantId is required")
              .Must(id => id != Guid.Empty).WithMessage("Invalid TenantId");
      }
  }
  ```

#### Czego NIE duplikować
- Logika walidacji – przenieś do Validator
- Mapowanie encji → DTO – stwórz metody rozszerzające lub AutoMapper profile
- Zapytania do bazy – wydziel do Repository methods lub specification pattern
- Logika autoryzacji – użyj Policy Handlers

### 5. **Komentarze – zasada minimalizmu**

#### ❌ NIE pisz komentarzy do:
- Oczywistych operacji:
  ```csharp
  // BAD
  // Pobierz użytkownika z bazy
  User? user = await userRepo.GetFirstBySearch(u => u.Id == userId);
  ```

- Kodu który sam się tłumaczy:
  ```csharp
  // BAD
  // Sprawdź czy plik istnieje
  if (projectFile == null)
  ```

#### ✅ Pisz komentarze gdy:
- Wyjaśniasz **dlaczego** coś jest zrobione w określony sposób:
  ```csharp
  // GOOD
  // CurrentVersionId jest ustawiane AFTER SaveChanges, bo wersja musi być najpierw zapisana w bazie
  // aby uzyskać prawidłowe Id i uniknąć naruszeń FK
  projectFile.CurrentVersionId = versionId;
  ```

- Dokumentujesz nietrywale decyzje biznesowe:
  ```csharp
  // GOOD
  // Rozszerzenie pliku musi być identyczne z oryginałem ze względu na wymagania
  // systemu wersjonowania i renderowania podglądu
  if (originalExtension != newFileExtension)
  {
      throw new ValidationApiException(...);
  }
  ```

- Ostrzegasz przed pułapkami:
  ```csharp
  // GOOD
  // UWAGA: właściwości ICurrentUser nie są async, więc jednorazowo pobieramy dane
  // blokując i keszujemy w obiekcie (scoped per request) – minimalizuje to koszty
  User? user = userRepo.GetFirstBySearch(u => u.Id == userId).GetAwaiter().GetResult();
  ```

#### XML Documentation Comments
Używaj XML comments dla **publicznych API endpoints** w kontrolerach:
```csharp
/// <summary>
/// Upload a new version of an existing project file
/// </summary>
/// <param name="tenantId">Tenant identifier</param>
/// <param name="projectId">Project identifier</param>
/// <returns>NoContent on success</returns>
[HttpPost("versions")]
[Authorize(Policy = Policies.ProjectMember)]
public async Task<IActionResult> UploadFileVersion(...)
```

### 6. **Walidacja – Validators vs Handlers**

#### FluentValidation Validators – odpowiedzialne za:
- ✅ Walidację **struktury i formatu** danych wejściowych
- ✅ Sprawdzanie **wymaganych pól** i ich ograniczeń
- ✅ Walidację **rozmiaru plików, długości stringów, formatów email, itp.**
- ✅ Proste reguły biznesowe **bez dostępu do bazy danych**

Przykład:
```csharp
public class UploadProjectFileVersionCommandValidator : AbstractValidator<UploadProjectFileVersionCommand>
{
    public UploadProjectFileVersionCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required");
        
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required");
        
        RuleFor(x => x.FileId)
            .NotEmpty().WithMessage("FileId is required");
        
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

#### Handlers – odpowiedzialne za:
- ✅ Walidację **logiki biznesowej wymagającej dostępu do bazy**
- ✅ Sprawdzanie **istnienia powiązanych encji**
- ✅ Weryfikację **uprawnień i izolacji multi-tenant**
- ✅ Reguły **specyficzne dla domeny** (np. zgodność rozszerzeń plików)

Przykład:
```csharp
public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken ct)
{
    // Walidacja istnienia pliku (wymaga dostępu do DB)
    ProjectFile? projectFile = await projectFileRepo.GetFirstBySearch(
        pf => pf.Id == request.FileId && 
              pf.TenantId == request.TenantId && 
              !pf.IsDeleted);
    
    if (projectFile == null)
    {
        throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
    }
    
    // Walidacja logiki biznesowej (zgodność rozszerzeń)
    string originalExtension = Path.GetExtension(projectFile.FileName).ToLowerInvariant();
    string newFileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
    
    if (originalExtension != newFileExtension)
    {
        throw new ValidationApiException(
            $"Extension mismatch. Expected: {originalExtension}, received: {newFileExtension}");
    }
    
    // ... reszta logiki
}
```

#### Zasada podziału
> **Jeśli walidacja wymaga dostępu do bazy danych lub zewnętrznych serwisów → Handler**  
> **Jeśli to prosta reguła na danych wejściowych → Validator**

#### KRYTYCZNA ZASADA: Przenoszenie walidacji istnienia do Validator

**Jeśli w Handler pobierasz obiekt z bazy WYŁĄCZNIE w celu sprawdzenia czy istnieje** (bez żadnej dodatkowej logiki biznesowej), taka walidacja **POWINNA być przeniesiona do Validator**.

```csharp
// ❌ BAD - walidacja istnienia w Handler (gdy to jedyne użycie obiektu)
public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken ct)
{
    // Pobieramy tylko po to, żeby sprawdzić czy istnieje - to powinno być w Validator!
    ProjectFileVersion? version = await versionRepo.GetFirstBySearch(
        v => v.Id == request.VersionId && 
             v.TenantId == request.TenantId &&
             !v.IsDeleted
    );
    
    if (version == null)
    {
        throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
    }
    
    // Tworzymy komentarz bez użycia pobranej wersji
    var comment = new FileVersionComment
    {
        ProjectFileVersionId = request.VersionId, // Używamy Id z request, nie z pobranego obiektu
        Comment = request.Comment,
        // ...
    };
    
    await commentRepo.Insert(comment);
    await commentRepo.SaveChangesAsync(ct);
    return Unit.Value;
}

// ✅ GOOD - walidacja istnienia przeniesiona do Validator
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
            v => v.Id == versionId && 
                 v.TenantId == command.TenantId &&
                 !v.IsDeleted
        ) != null;
    }
}

// Handler jest teraz prostszy - wie że wersja istnieje dzięki Validator
public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken ct)
{
    // Nie musimy sprawdzać istnienia - Validator to już zrobił
    var comment = new FileVersionComment
    {
        ProjectFileVersionId = request.VersionId,
        Comment = request.Comment,
        CreatedByUserId = currentUser.Id,
        TenantId = request.TenantId
    };
    
    await commentRepo.Insert(comment);
    await commentRepo.SaveChangesAsync(ct);
    return Unit.Value;
}
```

**Kiedy NIE przenosić do Validator:**

```csharp
// ✅ GOOD - zostaje w Handler, bo używamy properties/navigation properties pobranego obiektu
public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken ct)
{
    ProjectFile? projectFile = await projectFileRepo.GetFirstBySearch(
        pf => pf.Id == request.FileId && 
              pf.TenantId == request.TenantId && 
              !pf.IsDeleted
    );
    
    if (projectFile == null)
    {
        throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
    }
    
    // UŻYWAMY properties pobranego obiektu - musi zostać w Handler
    string originalExtension = Path.GetExtension(projectFile.FileName).ToLowerInvariant();
    string newFileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
    
    if (originalExtension != newFileExtension)
    {
        throw new ValidationApiException($"Extension mismatch. Expected: {originalExtension}");
    }
    
    // Dalej używamy projectFile do logiki biznesowej
    var version = new ProjectFileVersion
    {
        ProjectFileId = projectFile.Id,
        VersionNumber = projectFile.CurrentVersionNumber + 1, // ← Używamy properties
        // ...
    };
    
    // ...
}
```

**Podsumowanie zasady:**
- ✅ Przenoszenie do Validator: sprawdzamy tylko **czy istnieje** (NotFound check)
- ❌ Zostaje w Handler: używamy **properties/navigation properties** pobranego obiektu do logiki biznesowej
- ❌ Zostaje w Handler: sprawdzamy **złożone warunki biznesowe** wymagające dostępu do wielu powiązanych encji

### 7. **Autoryzacja – Policy-based**

#### Struktura autoryzacji
Projekt używa **Policy-based authorization** zamiast atrybutów `[Authorize(Roles = "...")]`

#### Definicja polityk
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

#### Rejestracja polityk
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

#### Użycie w kontrolerach
```csharp
[HttpPost("versions")]
[Authorize(Policy = Policies.ProjectMember)]
public async Task<IActionResult> UploadFileVersion(...)
```

#### Implementacja własnych RequirementHandlers
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
        
        bool hasAccess = currentUser.Projects?
            .Any(p => p.ProjectId == projectId) ?? false;
        
        if (hasAccess)
            context.Succeed(requirement);
        
        return Task.CompletedTask;
    }
}
```

#### Zasady
- **NIE używaj** `[Authorize(Roles = "Admin")]` – zawsze przez Policy
- **Kontroler** sprawdza tylko czy użytkownik ma dostęp do zasobu wysokiego poziomu (projekt, tenant)
- **Handler** dodatkowo weryfikuje izolację multi-tenant i szczegółowe uprawnienia

### 8. **Organizacja kodu CQRS**

#### Struktura folderów dla feature
Każda funkcjonalność (feature) w `CQRS/` ma dedykowany folder z plikami:

```
CQRS/
└── Files/
    └── UploadProjectFileVersion/
        ├── UploadProjectFileVersionCommand.cs           # Request model (record)
        ├── UploadProjectFileVersionCommandHandler.cs    # Logika biznesowa
        └── UploadProjectFileVersionCommandValidator.cs  # FluentValidation rules
```

#### Konwencja nazewnictwa
- **Command/Query:** `{Feature}{Action}Command` lub `{Feature}{Action}Query`
  - Przykład: `UploadProjectFileVersionCommand`, `GetProjectDetailsQuery`
- **Handler:** `{Feature}{Action}CommandHandler` lub `{Feature}{Action}QueryHandler`
- **Validator:** `{Feature}{Action}CommandValidator` lub `{Feature}{Action}QueryValidator`

#### Command/Query jako record
Używaj `record` dla immutability:
```csharp
public record UploadProjectFileVersionCommand : IRequest<Unit>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid FileId { get; init; }
    public IFormFile File { get; init; } = null!;
    public string? Comment { get; init; }
}
```

#### Web modele (DTO)
Definiowane w `Business/Interfaces/WebModels/`:
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

**Zasady:**
- Suffix `Web` dla wszystkich DTO zwracanych przez API
- Powinny zawierać tylko dane potrzebne dla UI/klienta
- NIE eksponuj encji domenowych bezpośrednio (np. `ProjectFile` → `ProjectFileWeb`)

### 9. **Entity Framework Core – Repository Pattern**

#### Struktura Repository
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

#### Zasady używania
- **Zawsze używaj includes** dla navigation properties:
  ```csharp
  var project = await projectRepo.GetFirstBySearch(
      p => p.Id == projectId,
      include => include
          .Include(p => p.Members)
          .Include(p => p.Groups)
  );
  ```

- **SaveChanges** wywoływany **RĘCZNIE** po wszystkich operacjach:
  ```csharp
  await projectFileRepo.Insert(projectFile);
  await projectFileVersionRepo.Insert(firstVersion);
  await projectFileRepo.SaveChangesAsync(ct); // Dopiero teraz commitujemy
  ```

- **Soft delete** – ustawiaj `IsDeleted = true`, nie używaj `Delete()`:
  ```csharp
  projectFile.IsDeleted = true;
  projectFile.DeletedAt = DateTime.UtcNow;
  await projectFileRepo.Update(projectFile);
  ```

- **Filtruj soft-deleted** w każdym zapytaniu:
  ```csharp
  var files = await projectFileRepo.GetBySearch(
      f => f.ProjectId == projectId && !f.IsDeleted
  );
  ```

### 10. **Helpery i Serwisy**

#### Business/Interfaces/Helpers/
Klasy statyczne z metodami narzędziowymi:
```csharp
// FileHelper.cs
public static class FileHelper
{
    public static string NormalizePackageNameForBlobPath(string packageName)
    {
        return packageName.Replace(" ", "_").Replace("/", "-");
    }
}
```

#### Business/Interfaces/Services/ & Business/Implementation/Services/
Serwisy z logiką domenową/infrastrukturową:
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
    // ... implementacja
}
```

**Rejestracja w DI:**
```csharp
// WebApi/Extensions/ServiceCollectionExtensions.cs
services.AddScoped<IBlobStorageService, BlobStorageService>();
```

### 11. **Asynchroniczność**

#### Zasady async/await
- **Wszystkie operacje IO** muszą być async: baza danych, API, file system, blob storage
- **NIE UŻYWAJ** `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` w handlerach
  - Wyjątek: `CurrentUser` (scoped per request, cached)
  
- **Przekazuj CancellationToken**:
  ```csharp
  public async Task<Unit> Handle(Command request, CancellationToken ct)
  {
      await repo.SaveChangesAsync(ct);
      await blobService.UploadAsync(container, path, stream, contentType, ct);
  }
  ```

- **NIE MIX** synchronicznego i asynchronicznego kodu:
  ```csharp
  // BAD
  var user = userRepo.GetFirstBySearch(u => u.Id == id).Result;
  
  // GOOD
  var user = await userRepo.GetFirstBySearch(u => u.Id == id);
  ```

### 12. **Logowanie**

#### Kiedy logować

- ✅ **Operacje krytyczne** (utworzenie zasobu, upload plików, modyfikacja danych):
  ```csharp
  logger.LogInformation(
      "Created new version {VersionNumber} for file {FileId} in project {ProjectId}",
      versionNumber, fileId, projectId);
  ```

- ✅ **Błędy i wyjątki**:
  ```csharp
  catch (Exception ex)
  {
      logger.LogError(ex, 
          "Failed to upload file {FileId} to project {ProjectId}",
          fileId, projectId);
      throw;
  }
  ```

- ✅ **Ostrzeżenia** (operacje potencjalnie problematyczne):
  ```csharp
  logger.LogWarning(
      "Failed to cleanup blob {BlobPath} after upload failure",
      blobPath);
  ```

#### Czego NIE logować
- ❌ Każdego kroku w handlerze
- ❌ Danych wrażliwych (hasła, tokeny, PII)
- ❌ Całych obiektów (tylko istotne właściwości)

#### Format logów
- Używaj **structured logging** (parametry, nie interpolacja):
  ```csharp
  // GOOD
  logger.LogInformation("User {UserId} uploaded file {FileName}", userId, fileName);
  
  // BAD
  logger.LogInformation($"User {userId} uploaded file {fileName}");
  ```

### 13. **Kontrolery – REST API**

#### Routing i konwencje
- **Route template**: `api/tenants/{tenantId}/projects/{projectId}/[controller]`
- **Akcje HTTP**: `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`
- **Status codes**:
  - `200 OK` – GET zwraca dane
  - `201 Created` – POST tworzy zasób (zwróć `CreatedAtAction` z ID)
  - `204 NoContent` – POST/PUT/DELETE bez body w odpowiedzi
  - `400 BadRequest` – błąd walidacji
  - `404 NotFound` – zasób nie istnieje
  - `403 Forbidden` – brak uprawnień

#### Przykład kontrolera
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

**Zasady:**
- Kontroler jest **cienką warstwą** – tylko routing i autoryzacja
- Logika biznesowa w **Handlerze**
- Parametry route (`tenantId`, `projectId`) ustawiaj w kontrolerze:
  ```csharp
  command = command with { TenantId = tenantId, ProjectId = projectId };
  ```

### 14. **Inne ważne zasady**

#### BaseEntity i Guid
- Wszystkie encje dziedziczą po `BaseEntity` który automatycznie generuje `Id`:
  ```csharp
  public class BaseEntity
  {
      public Guid Id { get; set; } = Guid.NewGuid();
  }
  ```
- **NIE TWÓRZ** ręcznie `Guid.NewGuid()` dla encji

#### DateTime
- **Zawsze używaj UTC**:
  ```csharp
  CreatedAt = DateTime.UtcNow
  ```
- **NIE używaj** `DateTime.Now`

#### Navigation Properties
- Inicjalizuj kolekcje w klasach encji:
  ```csharp
  public class Project : BaseEntity
  {
      public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
  }
  ```

#### MediatR Unit
- Gdy Command nic nie zwraca, używaj `Unit` zamiast `Task`:
  ```csharp
  public record DeleteProjectCommand : IRequest<Unit> { ... }
  
  public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken ct)
  {
      // ...
      return Unit.Value;
  }
  ```

#### Null-safety
- Używaj `?` dla nullable types:
  ```csharp
  public string? Comment { get; init; }
  ```
- Sprawdzaj null przed użyciem:
  ```csharp
  if (projectFile == null)
      throw new NotFoundApiException(...);
  
  // Teraz bezpiecznie używamy projectFile
  ```

### 15. **ICurrentUser – kontekst użytkownika**

#### Interfejs i właściwości
`ICurrentUser` dostarcza informacji o zalogowanym użytkowniku:
```csharp
public interface ICurrentUser
{
    Guid Id { get; }                        // User ID z JWT claims
    string FirstName { get; }               // Lazy-loaded z DB
    string LastName { get; }                // Lazy-loaded z DB
    string Email { get; }                   // Z JWT claims
    Guid? ActiveTenantId { get; }           // Z TenantPreferencesProfile (DB)
    TenantRole? ActiveTenantRole { get; }   // Z JWT claims
    SystemRole SystemRole { get; }          // Z JWT claims
    bool IsAuthenticated { get; }
}
```

#### Użycie w handlerach
```csharp
public class UploadProjectFileVersionCommandHandler : IRequestHandler<...>
{
    private readonly ICurrentUser currentUser;
    
    public async Task<Unit> Handle(Command request, CancellationToken ct)
    {
        // Pobierz ID zalogowanego użytkownika
        Guid userId = currentUser.Id;
        
        // Sprawdź aktywnego tenanta
        if (currentUser.ActiveTenantId != request.TenantId)
        {
            throw new ForbiddenApiException("Cannot access this tenant's resources");
        }
        
        // Użyj w encji
        var version = new ProjectFileVersion
        {
            CreatedByUserId = currentUser.Id,
            // ...
        };
    }
}
```

#### Rejestracja
```csharp
// ServiceCollectionExtensions.cs
services.AddScoped<ICurrentUser, CurrentUser>();
```

**Uwaga:** `CurrentUser` jest **scoped per request** i cachuje dane z DB, więc bezpieczne jest wielokrotne odwoływanie się do jego właściwości.

---

## 🚦 Checklist przed commitem

### ✅ CQRS & Architektura
- [ ] Command dziedziczy po `IRequestCommand<T>`, Query po `IRequestQuery<T>` (NIE bezpośrednio `IRequest<T>`)
- [ ] Command zwraca `Unit` lub `Guid`, Query zwraca Web model (DTO)
- [ ] Handler nie buduje rozbudowanych projekcji w Command
- [ ] Query mapuje encje na `*Web` modele przed zwróceniem
- [ ] Nazewnictwo: `{Feature}{Action}Command/Query` + `Handler` + `Validator`
- [ ] `SaveChangesAsync()` wywoływane OBLIGATORYJNIE na końcu każdego Command Handler
- [ ] `SaveChangesAsync()` wcześniej TYLKO gdy potrzebne Id dla Foreign Key w kolejnej encji

### ✅ Multi-tenancy & Bezpieczeństwo
- [ ] Walidacja `TenantId` we wszystkich handlerach
- [ ] Zapytania filtrują po `TenantId` i `!IsDeleted`
- [ ] Kontroler używa Policy-based authorization (`[Authorize(Policy = ...)]`)
- [ ] Parametry route (`tenantId`, `projectId`) ustawiane w kontrolerze

### ✅ Wyjątki & Walidacja
- [ ] Używam dedykowanych klas wyjątków (`NotFoundApiException`, `ValidationApiException`, etc.)
- [ ] **WSZYSTKIE wyjątki napisane PO ANGIELSKU** (nie po polsku!)
- [ ] Walidacja wejścia w `Validator`, walidacja biznesowa w `Handler`
- [ ] Jeśli pobieranie z bazy TYLKO dla sprawdzenia istnienia → przenieś do `Validator` (MustAsync)
- [ ] Jeśli używam properties/navigation properties pobranego obiektu → zostaje w `Handler`
- [ ] Brak `throw new Exception()` ani standardowych wyjątków .NET w handlerach

### ✅ Async & Repository
- [ ] Wszystkie operacje async/await używają `CancellationToken`
- [ ] Brak `.Result`, `.Wait()` w kodzie asynchronicznym
- [ ] Navigation properties ładowane przez `.Include()` w repository

### ✅ Kod & Czytelność
- [ ] Brak nadmiarowych komentarzy – kod jest samodokumentujący się
- [ ] Duplikowany kod wydzielony do Helper/Service
- [ ] Używam `DateTime.UtcNow` zamiast `DateTime.Now`
- [ ] Logowanie zawiera structured parameters, nie interpolację stringów
- [ ] XML documentation comments dla public API endpoints

### ✅ Null-safety & Immutability
- [ ] Używam `?` dla nullable types
- [ ] Command/Query zdefiniowane jako `record` z `init`
- [ ] Sprawdzam `null` przed użyciem encji z DB (lub delegowane do Validator gdy to tylko existence check)
