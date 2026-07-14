# tenant-simplify-api-fix-02 — WebModels: RoleCode → IsAdmin

## Cel
Zastąp `RoleCode: string` przez `IsAdmin: bool` we wszystkich WebModelach dotyczących tenanta.

## Skill
Przeczytaj `.opencode/skills/api/skill-api-cqrs.md` przed implementacją.

## Pliki do modyfikacji

### 1. `src/Business/Interfaces/WebModels/Tenants/TenantMemberWeb.cs`

**Zamień:**
```csharp
public sealed record TenantMemberWeb(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string RoleCode,
    bool IsActive,
    DateTime JoinedAt
);
```

**Na:**
```csharp
public sealed record TenantMemberWeb(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    bool IsAdmin,
    bool IsActive,
    DateTime JoinedAt
);
```

### 2. `src/Business/Interfaces/WebModels/Tenants/TenantDetailsWeb.cs`

**Zamień:**
```csharp
public sealed record TenantDetailsWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
    public required string RoleCode { get; init; }
    public List<TenantMemberWeb> Members { get; init; } = new();
    public List<TenantInvitationWeb> Invitations { get; init; } = new();
}
```

**Na:**
```csharp
public sealed record TenantDetailsWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsAdmin { get; init; }
    public List<TenantMemberWeb> Members { get; init; } = new();
    public List<TenantInvitationWeb> Invitations { get; init; } = new();
}
```

### 3. `src/Business/Interfaces/WebModels/Tenants/UserTenantWeb.cs`

**Zamień:**
```csharp
public sealed record UserTenantWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
    public required string RoleCode { get; init; }
    public required bool IsActiveTenant { get; init; }
}
```

**Na:**
```csharp
public sealed record UserTenantWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsAdmin { get; init; }
    public required bool IsActiveTenant { get; init; }
}
```

### 4. `src/Business/Interfaces/WebModels/Tenants/TenantBasicWeb.cs`

**Zamień:**
```csharp
public sealed record TenantBasicWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
    public required string RoleCode { get; init; }
}
```

**Na:**
```csharp
public sealed record TenantBasicWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsAdmin { get; init; }
}
```

### 5. `src/Business/Interfaces/WebModels/Users/UserDetailsWeb.cs`

Zastąp `ActiveTenantPermissions` przez `IsActiveTenantAdmin`:

**Stare:**
```csharp
public sealed record UserDetailsWeb(
    Guid Id, 
    string FirstName, 
    string LastName, 
    string Email, 
    Guid? ActiveTenantId,
    HashSet<string> ActiveTenantPermissions,
    string? PhoneNumber,
    ...
);
```

**Nowe:**
```csharp
public sealed record UserDetailsWeb(
    Guid Id, 
    string FirstName, 
    string LastName, 
    string Email, 
    Guid? ActiveTenantId,
    
    /// <summary>
    /// Whether the user is admin in the active tenant. False if no active tenant.
    /// </summary>
    bool IsActiveTenantAdmin,

    string? PhoneNumber,
    string? CompanyName,
    string? TaxId,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country
);
```

Usuń `using Entities.Enums;` jeśli nie jest używane przez inną właściwość.

## Build check
```
dotnet build src/Business/Business.csproj
```

WAŻNE: Błędy w CQRS odwołujące się do `RoleCode` w konstruktorach `TenantMemberWeb` lub właściwości `UserTenantWeb.RoleCode` będą naprawione w fix-05.
