# Files — Fix 01: Bezpieczeństwo (CRITICAL)

Cel: zlikwidować ryzyko cross-tenant data leak i wzmocnić defense-in-depth.
Zakres minimalny — tylko punktowe poprawki w handlerach + RequiredId w validatorach.

## Reguły
- Zakaz `var` — explicit types.
- `is null` / `is not null` zamiast `== null` / `!= null` (tylko w dotykanych liniach).
- Zachować dotychczasowy kontrakt API (sygnatury Commands/Queries/Web modeli — bez zmian).

## Zmiany

### 1) AddFileVersionCommentCommandHandler — cross-tenant leak (K1)
Plik: `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Files/AddFileVersionComment/AddFileVersionCommentCommandHandler.cs`

Zmień predykat pobierania `ProjectFileVersion`:
- Z: `pfv => pfv.Id == request.VersionId`
- Na: dodać `&& pfv.TenantId == request.TenantId && pfv.ProjectId == request.ProjectId`
  (jeśli `ProjectFileVersion` nie ma bezpośrednio `TenantId`/`ProjectId`, użyj nawigacji `pfv.ProjectFile.TenantId == ... && pfv.ProjectFile.ProjectId == ...` z `Include(pfv => pfv.ProjectFile)` lub zmień kolejność: najpierw pobrać `ProjectFile` z filtrem TenantId/ProjectId, potem `ProjectFileVersion` filtrowane po `ProjectFileId == file.Id` — wybierz wariant minimalnie inwazyjny).

Sprawdź w kodzie aktualne pole klucza i zachowaj najprostszą poprawną formę.

### 2) DeleteProjectFileCommandHandler — defense in depth (K2)
Plik: `02-ApplicationServices/.../CQRS/Files/DeleteProjectFile/DeleteProjectFileCommandHandler.cs`

W zapytaniu pobierającym wersje pliku:
- Z: `v => v.ProjectFileId == file.Id`
- Na: dodać filtr po `TenantId` i `ProjectId` (analogicznie do wzorca powyżej).

### 3) UpdateFileShareCommandHandler — defense in depth (K3)
Plik: `02-ApplicationServices/.../CQRS/Files/UpdateFileShare/UpdateFileShareCommandHandler.cs`

W zapytaniu `sharedProjectFileRepo.GetBySearch(spf => spf.ProjectFilePackageId == packageId)`:
- Dodać `&& spf.TenantId == request.TenantId && spf.ProjectId == request.ProjectId` (lub przez nawigację `spf.ProjectFilePackage.TenantId == ...`).

### 4) Read-only repozytoria (K4)
Zmień `IRepository<T>` → `IReadRepository<T>` tam gdzie nie ma `Insert/Update/Delete/SaveChanges`:
- `AddFileVersionCommentCommandHandler`: `IRepository<ProjectFile>` → `IReadRepository<ProjectFile>`, `IRepository<ProjectFileVersion>` → `IReadRepository<ProjectFileVersion>` (jeśli tylko czyta).
- `UpdateFileShareCommandHandler`: `IRepository<User>` → `IReadRepository<User>` (jeśli tylko czyta).
- W `DeleteProjectFileCommandHandler` zostaw `IRepository<ProjectFileVersion>` (potrzebny `ExecuteDeleteAsync`/`Delete`).

Najpierw zweryfikuj dla każdego repo czy faktycznie tylko odczyt — jeśli jest jakikolwiek write, NIE zmieniaj.

### 5) RequiredId we WSZYSTKICH validatorach Files (K5)
Pliki (11):
- `AddFileVersionComment/AddFileVersionCommentCommandValidator.cs`
- `CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandValidator.cs`
- `DeleteProjectFile/DeleteProjectFileCommandValidator.cs`
- `SharePackages/SharePackagesCommandValidator.cs`
- `UpdateFileShare/UpdateFileShareCommandValidator.cs`
- `UploadProjectFiles/UploadProjectFilesCommandValidator.cs`
- `UploadProjectFileVersion/UploadProjectFileVersionCommandValidator.cs`
- `GetFileVersions/GetFileVersionsQueryValidator.cs`
- `GetPackageFiles/GetPackageFilesQueryValidator.cs`
- `GetProjectFilePackages/GetProjectFilePackagesQueryValidator.cs`
- `GetVersionComments/GetVersionCommentsQueryValidator.cs`

W każdym dodać na początku konstruktora:
```csharp
RuleFor(x => x.TenantId).RequiredId();
RuleFor(x => x.ProjectId).RequiredId();
```
Dodaj `using` z namespace gdzie jest `CommonValidationExtensions` (sprawdź w innych validatorach projektu, np. CostEstimates lub Projects).

NIE zmieniaj jeszcze pozostałych reguł, NIE zmieniaj sealed/required/positional — to jest w kolejnych promptach.

## Po wykonaniu
Zbuduj solution: `cd 02-ApplicationServices/ProductDataManagementWebAPI && dotnet build`
Zwróć raport: status buildu, lista zmodyfikowanych plików, wszelkie blokery.
