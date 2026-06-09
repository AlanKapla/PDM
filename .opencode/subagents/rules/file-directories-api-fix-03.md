# API Fix 03 — CreatePackageAndUploadFiles: ParentId + walidacja

## Cel
Dodanie `ParentId` do komendy tworzenia paczki i aktualizacja handlera oraz walidatora.

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skill
Przeczytaj: `.github/skills/api/skill-api-cqrs.md`
Przeczytaj: `.github/skills/api/skill-api-validators.md`

## Pliki do zmiany

### 1. `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommand.cs`

Dodać opcjonalne pole `ParentId`:
```csharp
public Guid? ParentId { get; init; }
```

### 2. `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandHandler.cs`

Znaleźć metodę `BuildPackage()` (lub analogiczną logikę tworzenia `ProjectFilePackage`) i dodać ustawienie `ParentId`:
```csharp
ParentId = request.ParentId
```

### 3. `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandValidator.cs`

Dwie zmiany:

**a) Walidacja unikalności nazwy z uwzględnieniem ParentId:**

Obecna walidacja sprawdza `(TenantId, ProjectId, OwnerId, Name)` — bez `ParentId`. Trzeba dodać warunek `p.ParentId == command.ParentId` do predykatu sprawdzania duplikatów.

Logika: katalog `A/SubDir` i katalog `B/SubDir` mogą istnieć jednocześnie — unikalność jest per katalog nadrzędny.

Dla katalogów głównych (`ParentId == null`): sprawdzaj `p.ParentId == null`.

**b) Walidacja istnienia `ParentId`:**

Jeśli `ParentId` jest podane (nie null), sprawdzić że:
1. Katalog nadrzędny istnieje w tej samej `(TenantId, ProjectId)`
2. Nie jest soft-deleted

Użyć `IReadRepository<ProjectFilePackage>` (dodać do zależności validatora jeśli nie istnieje):
```csharp
RuleFor(c => c.ParentId)
    .MustAsync(async (command, parentId, ct) =>
    {
        if (parentId is null) return true;
        var parent = await packageRepository.GetFirstBySearch(
            p => p.Id == parentId.Value &&
                 p.TenantId == command.TenantId &&
                 p.ProjectId == command.ProjectId);
        return parent is not null;
    })
    .WithMessage("Parent directory not found or does not belong to this project.")
    .When(c => c.ParentId.HasValue);
```

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
```
Build musi przejść bez błędów.
