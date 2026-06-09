# API Fix 02 — DTO + WebModel

## Cel
Dodanie `ParentId` i `SubCatalogs` do `ProjectFilePackageDto` i `ProjectFilePackageWeb`.

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Pliki do zmiany

### 1. `src/Business/Interfaces/DTO/ProjectFilePackageDto.cs`

Stan obecny:
```csharp
public record ProjectFilePackageDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid OwnerId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public bool IsDeleted { get; init; }
}
```

Dodać:
```csharp
public Guid? ParentId { get; init; }
```

### 2. `src/Business/Interfaces/WebModels/Files/ProjectFilePackageWeb.cs`

Stan obecny:
```csharp
public sealed record ProjectFilePackageWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required Guid OwnerId { get; init; }
    public required string OwnerName { get; init; }
    public List<ProjectFileWeb> Files { get; init; } = new();
    public required int TotalFiles { get; init; }
}
```

Dodać dwa pola:
```csharp
public Guid? ParentId { get; init; }
public List<ProjectFilePackageWeb> SubCatalogs { get; init; } = new();
```

## Weryfikacja
```
dotnet build src/Business/Business.Interfaces/Business.Interfaces.csproj
```
Build musi przejść bez błędów.
