# Audyt domeny CQRS — Files

Ścieżka: `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Files/`

## BLOK 1 — INWENTARYZACJA

### Pliki współdzielone domeny
| Plik | Typ | Ścieżka |
|------|-----|---------|
| `FileUploadItem` | DTO uploadu | `Files/FileUploadItem.cs` |
| `ProjectFileDto` | **PUSTY plik** | `Files/ProjectFileDto.cs` |

Brak klas bazowych (`FilesCommandBase`, `FilesQueryBase`).

### Commands (7)
| Plik | Typ | Ścieżka |
|------|-----|---------|
| `AddFileVersionCommentCommand` | Command | `AddFileVersionComment/AddFileVersionCommentCommand.cs` |
| `AddFileVersionCommentCommandHandler` | Handler | `AddFileVersionComment/AddFileVersionCommentCommandHandler.cs` |
| `AddFileVersionCommentCommandValidator` | Validator | `AddFileVersionComment/AddFileVersionCommentCommandValidator.cs` |
| `CreatePackageAndUploadFilesCommand` | Command | `CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommand.cs` |
| `CreatePackageAndUploadFilesCommandHandler` | Handler | `CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandHandler.cs` |
| `CreatePackageAndUploadFilesCommandValidator` | Validator | `CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandValidator.cs` |
| `DeleteProjectFileCommand` | Command | `DeleteProjectFile/DeleteProjectFileCommand.cs` |
| `DeleteProjectFileCommandHandler` | Handler | `DeleteProjectFile/DeleteProjectFileCommandHandler.cs` |
| `DeleteProjectFileCommandValidator` | Validator | `DeleteProjectFile/DeleteProjectFileCommandValidator.cs` |
| `SharePackagesCommand` | Command | `SharePackages/ShareProjectFilesCommand.cs` |
| `SharePackagesCommandHandler` | Handler | `SharePackages/SharePackagesCommandHandler.cs` |
| `SharePackagesCommandValidator` | Validator | `SharePackages/SharePackagesCommandValidator.cs` |
| `UpdateFileShareCommand` | Command | `UpdateFileShare/UpdateFileShareCommand.cs` |
| `UpdateFileShareCommandHandler` | Handler | `UpdateFileShare/UpdateFileShareCommandHandler.cs` |
| `UpdateFileShareCommandValidator` | Validator | `UpdateFileShare/UpdateFileShareCommandValidator.cs` |
| `UploadProjectFilesCommand` | Command | `UploadProjectFiles/UploadProjectFilesCommand.cs` |
| `UploadProjectFilesCommandHandler` | Handler | `UploadProjectFiles/UploadProjectFilesCommandHandler.cs` |
| `UploadProjectFilesCommandValidator` | Validator | `UploadProjectFiles/UploadProjectFilesCommandValidator.cs` |
| `UploadProjectFileVersionCommand` | Command | `UploadProjectFileVersion/UploadProjectFileVersionCommand.cs` |
| `UploadProjectFileVersionCommandHandler` | Handler | `UploadProjectFileVersion/UploadProjectFileVersionCommandHandler.cs` |
| `UploadProjectFileVersionCommandValidator` | Validator | `UploadProjectFileVersion/UploadProjectFileVersionCommandValidator.cs` |

### Queries (4)
| Plik | Typ | Ścieżka |
|------|-----|---------|
| `GetFileVersionsQuery` | Query | `GetFileVersions/GetFileVersionsQuery.cs` |
| `GetFileVersionsQueryHandler` | Handler | `GetFileVersions/GetFileVersionsQueryHandler.cs` |
| `GetFileVersionsQueryValidator` | Validator | `GetFileVersions/GetFileVersionsQueryValidator.cs` |
| `GetPackageFilesQuery` | Query | `GetPackageFiles/GetPackageFilesQuery.cs` |
| `GetPackageFilesQueryHandler` | Handler | `GetPackageFiles/GetPackageFilesQueryHandler.cs` |
| `GetProjectFilePackagesQueryValidator` (klasa) w pliku `GetPackageFilesQueryValidator.cs` | Validator | `GetPackageFiles/GetPackageFilesQueryValidator.cs` |
| `GetProjectFilePackagesQuery` | Query | `GetProjectFilePackages/GetProjectFilePackagesQuery.cs` |
| `GetProjectFilePackagesQueryHandler` | Handler | `GetProjectFilePackages/GetProjectFilePackagesQueryHandler.cs` |
| `GetProjectFilePackagesQueryValidator` | Validator | `GetProjectFilePackages/GetProjectFilePackagesQueryValidator.cs` |
| `GetVersionCommentsQuery` | Query | `GetVersionComments/GetVersionCommentsQuery.cs` |
| `GetVersionCommentsQueryHandler` | Handler | `GetVersionComments/GetVersionCommentsQueryHandler.cs` |
| `GetVersionCommentsQueryValidator` | Validator | `GetVersionComments/GetVersionCommentsQueryValidator.cs` |

### Web modele (6) — `Business/Interfaces/WebModels/Files/`
| Plik | Typ |
|------|-----|
| `ProjectFileWeb` | record |
| `ProjectFilePackageWeb` | record |
| `ProjectFileVersionWeb` | record |
| `ProjectFileVersionCommentWeb` | record |
| `SharedProjectFileWeb` | record |
| `SharedProjectFilePackageWeb` | record |

## BLOK 2 — COMMANDS I QUERIES — STRUKTURA

### 2.1 Positional parameters vs explicit properties
Wzorzec docelowy: `public sealed record X : IRequestCommand<Y> { public required Guid TenantId { get; init; } }`.

| Command/Query | Styl | Zgodność |
|---------------|------|----------|
| `AddFileVersionCommentCommand` | properties `{ get; init; }` (bez `required`) | ⚠ częściowo |
| `CreatePackageAndUploadFilesCommand` | properties `{ get; init; }` (bez `required`) | ⚠ częściowo |
| `DeleteProjectFileCommand` | **positional record** | ❌ |
| `SharePackagesCommand` | properties `{ get; init; }` (bez `required`) | ⚠ częściowo |
| `UpdateFileShareCommand` | properties `{ get; init; }` (bez `required`) | ⚠ częściowo |
| `UploadProjectFilesCommand` | properties `{ get; init; }` (bez `required`) | ⚠ częściowo |
| `UploadProjectFileVersionCommand` | properties `{ get; init; }` (bez `required`) | ⚠ częściowo |
| `GetFileVersionsQuery` | **positional record** | ❌ |
| `GetPackageFilesQuery` | **positional record** | ❌ |
| `GetProjectFilePackagesQuery` | **positional record** | ❌ |
| `GetVersionCommentsQuery` | **positional record** | ❌ |

Żadne Command/Query nie używa modyfikatora `required`.

### 2.2 Sealed
| Command/Query | sealed |
|---------------|--------|
| `AddFileVersionCommentCommand` | ❌ |
| `CreatePackageAndUploadFilesCommand` | ❌ |
| `DeleteProjectFileCommand` | ✔ |
| `SharePackagesCommand` | ❌ |
| `UpdateFileShareCommand` | ❌ |
| `UploadProjectFilesCommand` | ❌ |
| `UploadProjectFileVersionCommand` | ❌ |
| `GetFileVersionsQuery` | ✔ |
| `GetPackageFilesQuery` | ✔ |
| `GetProjectFilePackagesQuery` | ✔ |
| `GetVersionCommentsQuery` | ✔ |

### 2.3 Interfejsy i autoryzacja
| Command/Query | Interfejs | IAuthorizableRequest | PermissionCode | Uwagi |
|---------------|-----------|----------------------|----------------|-------|
| `AddFileVersionCommentCommand` | `IRequestCommand<Unit>` | ✔ | `ProjectResourcesWrite` | komentarz pod uprawnieniem WRITE — może blokować read-shared userów |
| `CreatePackageAndUploadFilesCommand` | `IRequestCommand<Unit>` | ✔ | `ProjectResourcesWrite` | OK |
| `DeleteProjectFileCommand` | `IRequestCommand<Unit>` | ✔ | `ProjectResourcesWrite` | OK |
| `SharePackagesCommand` | `IRequestCommand<Unit>` | ✔ | `ProjectResourcesShare` | OK |
| `UpdateFileShareCommand` | `IRequestCommand<Unit>` | ✔ | `ProjectResourcesWrite` | powinno być `ProjectResourcesShare` (operacja udostępniania) |
| `UploadProjectFilesCommand` | `IRequestCommand<Unit>` | ✔ | `ProjectResourcesWrite` | OK |
| `UploadProjectFileVersionCommand` | `IRequestCommand<Unit>` | ✔ | `ProjectResourcesWriteShared` | **niespójność** — nowa wersja vs. nowy plik mają różne kody |
| `GetFileVersionsQuery` | `IRequestQuery<...>` | ✔ + `GetResourceScope()` | `ProjectView` | OK |
| `GetPackageFilesQuery` | `IRequestQuery<...>` | ✔ + `GetResourceScope()` | `ProjectView` | OK |
| `GetProjectFilePackagesQuery` | `IRequestQuery<...>` | ✔ + `GetResourceScope()` | `ProjectView` | OK |
| `GetVersionCommentsQuery` | `IRequestQuery<...>` | ✔ + `GetResourceScope()` | `ProjectView` | OK |

Wszystkie poprawnie implementują `GetResource()` z `TenantId`/`ProjectId`.

### 2.4 Wspólne pola — kandydaci do klasy bazowej
| Pole wspólne | Występuje w | Kandydat |
|--------------|-------------|----------|
| `TenantId`, `ProjectId` | wszystkie 11 | `FilesRequestBase` (lub globalne `ProjectScopedRequestBase`) |
| `FileId` | 4 (Add, Delete, GetFileVersions, GetVersionComments, UpdateFileShare, UploadProjectFileVersion) | `FileScopedRequestBase` |
| `Files: List<FileUploadItem>` | `CreatePackageAndUploadFiles`, `UploadProjectFiles` | `FileBatchUploadCommandBase` |
| `Scope: ResourceScope` | wszystkie 4 Queries | `ScopedQueryBase` |

## BLOK 3 — WALIDATORY

### 3.1 Pokrycie walidatorami
| Command/Query | Validator | Brakujące reguły |
|---------------|-----------|------------------|
| `AddFileVersionCommentCommand` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `CreatePackageAndUploadFilesCommand` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `DeleteProjectFileCommand` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `SharePackagesCommand` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId`; brak walidacji elementów `PackageIds` ≠ Empty Guid |
| `UpdateFileShareCommand` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `UploadProjectFilesCommand` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `UploadProjectFileVersionCommand` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `GetFileVersionsQuery` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `GetPackageFilesQuery` | ✔ (klasa źle nazwana — `GetProjectFilePackagesQueryValidator`) | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `GetProjectFilePackagesQuery` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |
| `GetVersionCommentsQuery` | ✔ | brak `RequiredId` dla `TenantId`, `ProjectId` |

Pokrycie walidatorami: **100 %** (11/11). Pokrycie walidacją `TenantId`/`ProjectId`: **0 %**.

### 3.2 Reguły szczegółowe — użycie CommonValidationExtensions
**Żaden** validator domeny Files nie używa `RequiredId()`, `NonNegativeOrder()`, `UniqueIds()`, `NotCurrentUser()`. Wszystkie reguły zapisane „ręcznie".

| Validator | Pole | Obecna reguła | Brakująca / docelowa | Uzasadnienie |
|-----------|------|---------------|----------------------|--------------|
| `AddFileVersionCommentCommandValidator` | `FileId`, `VersionId` | `NotEmpty().WithMessage(...)` | `RequiredId()` | spójność |
| `CreatePackageAndUploadFilesCommandValidator` | brak walidacji `TenantId/ProjectId` | — | `RequiredId()` | bezpieczeństwo, spójność |
| `DeleteProjectFileCommandValidator` | `FileId` | `NotEmpty()` | `RequiredId()` | spójność |
| `SharePackagesCommandValidator` | `PackageIds` | `Must(distinct)` | `UniqueIds()` | duplikacja logiki |
| `SharePackagesCommandValidator` | `SharedWithUserIds` | `Must(distinct)` | `UniqueIds()` + `NotCurrentUser` per element (alternatywa do skip-w-handlerze) | duplikacja logiki |
| `GetFileVersionsQueryValidator` | `FileId` | `NotEmpty()` | `RequiredId()` | spójność |
| `GetPackageFilesQueryValidator` | `PackageId` | `NotEmpty()` | `RequiredId()` | spójność |
| `GetVersionCommentsQueryValidator` | `FileId`, `VersionId` | `NotEmpty()` | `RequiredId()` | spójność |
| `UploadProjectFilesCommandValidator` | `ProjectFilePackageId` | `NotEmpty()` | `RequiredId()` | spójność |
| `UploadProjectFileVersionCommandValidator` | `FileId` | `NotEmpty()` | `RequiredId()` | spójność |

### 3.3 Spójność — nieużywane usingi, dead injection, sealed
| Plik | Problem |
|------|---------|
| `AddFileVersionCommentCommandValidator` | Wstrzykuje `IRepository<ProjectFile>`, `IRepository<ProjectFileVersion>`, `ICurrentUser` — **żadne nie jest używane**. Niepotrzebne usingi: `Entities.Models.Chats/Costs/Notifications/Roles/Tenants/Users/WorkSchedules`, `Microsoft.EntityFrameworkCore`. Brak `sealed`. |
| `CreatePackageAndUploadFilesCommandValidator` | Wstrzykuje `IReadRepository<Project>` — **nieużywane**. Dziesiątki nieużywanych usingów. Brak `sealed`. |
| `DeleteProjectFileCommandValidator` | Wstrzykuje 4 zależności (`IReadRepository<Project>`, `IRepository<ProjectFile>`, `IRepository<ProjectMember>`, `ICurrentUser`) — **wszystkie nieużywane**. Brak `sealed`. |
| `UpdateFileShareCommandValidator` | `MustAsync` w pętli `foreach (userId)` wykonuje **N kolejnych queries** zamiast jednej `Where(u => userIds.Contains(u.Id))`. Brak `sealed`. |
| `SharePackagesCommandValidator` | Brak `sealed`. Komunikaty po angielsku — OK. |
| `GetPackageFilesQueryValidator` | **Klasa nazywa się `GetProjectFilePackagesQueryValidator`** mimo że waliduje `GetPackageFilesQuery` — kolizja nazw, mylące. Brak `sealed`. |
| `GetProjectFilePackagesQueryValidator` | Niepotrzebny `using CQRS.Files.GetProjectFilePackages;` (duplikat namespace). Brak `sealed`. |
| Wszystkie | Mieszanka komunikatów EN/PL (większość EN, kilka komentarzy PL). Brak `sealed`. |

### 3.4 Wspólne reguły walidacji
| Reguła | Validatory | Kandydat do extension |
|--------|------------|----------------------|
| Walidacja rozszerzeń pliku (`BeValidExtension`) | `CreatePackageAndUploadFiles`, `UploadProjectFiles`, `UploadProjectFileVersion` | `IRuleBuilder<T,string>.AllowedFileExtension()` |
| Walidacja MIME (`BeValidContentType`) | `CreatePackageAndUploadFiles`, `UploadProjectFiles` | `IRuleBuilder<T,string>.AllowedContentType()` |
| Walidacja rozmiaru pliku | jak wyżej + `UploadProjectFileVersion` | `IRuleBuilder<T,long>.MaxFileSize()` |
| Walidacja `DisplayName` (długość) | `CreatePackageAndUploadFiles`, `UploadProjectFiles` | `IRuleBuilder<T,string>.MaxDisplayNameLength()` |
| Limit liczby plików per upload | `CreatePackageAndUploadFiles`, `UploadProjectFiles` | shared rule |
| `Scope: IsInEnum()` | wszystkie 4 Queries | `IRuleBuilder<T,ResourceScope>.ValidScope()` |
| Lista `Guid` distinct | `SharePackages` (×2) | `UniqueIds()` (już istnieje) |

## BLOK 4 — HANDLERY

### 4.1 Struktura
| Handler | sealed | Explicit types (brak `var`) | Uwagi |
|---------|--------|----------------------------|-------|
| `AddFileVersionCommentCommandHandler` | ❌ | ❌ (`var fileVersion`, `var file`) | + dead usingi (10 namespace'ów Entities.Models.*) |
| `CreatePackageAndUploadFilesCommandHandler` | ❌ | ❌ (`var allProjectFiles`, `var allProjectFileVersions`, `var allComments`) | dead usingi |
| `DeleteProjectFileCommandHandler` | ❌ | ❌ (`var versions`, `var versionsList`, `var version`) | dead usingi |
| `SharePackagesCommandHandler` | ❌ | ❌ (~12 wystąpień `var`) | dead usingi |
| `UpdateFileShareCommandHandler` | ❌ | ❌ (~25 wystąpień `var`) | dead usingi |
| `UploadProjectFilesCommandHandler` | ❌ | ✔ (poza `try-catch`) | dead usingi |
| `UploadProjectFileVersionCommandHandler` | ❌ | częściowo (`var projectFiles`) | dead usingi |
| `GetFileVersionsQueryHandler` | ❌ | ✔ | dead usingi |
| `GetPackageFilesQueryHandler` | ❌ | ✔ | dead usingi |
| `GetProjectFilePackagesQueryHandler` | ❌ | ✔ | OK |
| `GetVersionCommentsQueryHandler` | ❌ | ✔ | dead usingi |

### 4.2 Logika biznesowa — atomowość metod
| Handler | Linie ~ | Za dużo logiki | Co wydzielić |
|---------|---------|----------------|--------------|
| `AddFileVersionCommentCommandHandler` | 75 | tak — wszystko w `Handle` | `GetAndValidateFileAsync`, `GetAndValidateVersionAsync`, `EnsureUserCanCommentAsync`, `BuildComment` |
| `CreatePackageAndUploadFilesCommandHandler` | ~210 | **bardzo** — orkiestruje 7 kroków, ręcznie zarządza transakcją (3 × `SaveChangesAsync`) | `CreatePackage`, `CreateFileShells`, `UploadVersionAsync`, `BuildBlobPath`, `LinkCurrentVersions`, `InvalidateCachesAsync` |
| `DeleteProjectFileCommandHandler` | ~110 | tak | `GetAndValidateFileAsync`, `EnsureCanDeleteAsync`, `DeleteVersionsAndBlobsAsync`, `InvalidateCachesAsync` |
| `SharePackagesCommandHandler` | ~165 | tak — pętla z `ExecuteDelete` per user | `LoadAndAuthorizePackagesAsync`, `ResolveTargetUsers`, `SharePackageWithUserAsync` (już wydzielona ✔) |
| `UpdateFileShareCommandHandler` | **~430** | **ekstremalnie** — autoryzacja, ładowanie, dyf list, batch insert/delete, notyfikacje | wydzielić do `IFileShareService` (logika dyfu), notyfikacje do osobnej klasy |
| `UploadProjectFilesCommandHandler` | ~180 | tak — `SaveChangesAsync` w pętli (N+1 zapisów) | `UploadSingleFileAsync`, `BuildBlobPath`, `BuildVersion`, `BuildComment` |
| `UploadProjectFileVersionCommandHandler` | ~190 | tak | `GetAndValidateFileAsync`, `EnsureCanUploadVersionAsync`, `BuildNewVersion`, `UploadAndPersistAsync`, `RollbackBlobAsync` |
| `GetFileVersionsQueryHandler` | 80 | OK — `MapToVersionWeb` wydzielony | — |
| `GetPackageFilesQueryHandler` | ~150 | częściowo — duże `Handle`, mapowanie wydzielone | wydzielić budowanie `currentVersionWeb` |
| `GetProjectFilePackagesQueryHandler` | 80 | OK | inline `Add(new ProjectFilePackageWeb { ... })` można wynieść do `MapToPackageWeb` |
| `GetVersionCommentsQueryHandler` | 70 | OK — mapowanie inline (dyskusyjne) | wydzielić `MapToCommentWeb` |

### 4.3 SOLID i DRY
| Handler | Podobny do | Wspólna logika | Kandydat do wydzielenia |
|---------|------------|----------------|-------------------------|
| `UploadProjectFiles*` | `CreatePackageAndUploadFiles*`, `UploadProjectFileVersion*` | budowanie `ProjectFileVersion`, blob path, upload, komentarz | `IFileVersionUploadService` |
| `Delete*` / `Update*` / `Add*` | wszystkie | ręczne `IsTenantOrProjectAdminAsync || IsOwner || HasShareAccess` | `IFileAccessGuard` lub przeniesienie do `AuthorizationBehavior` przez nowy interfejs `IAssignedAuthorizableRequest` |
| `Get*` × 4 | wszystkie | mapowanie `ProjectFileVersionWeb`, pobieranie SAS URI, `userDict` | `IFileVersionWebMapper` |
| Wszystkie Commands write | wszystkie | invalidacja cache (`InvalidateProjectFilesCacheAsync` + Versions + Comments + FileAccess) | `IFileCacheInvalidator` z metodą `InvalidateAfterMutation(...)` |
| Mapowanie `User → FullName` | wszystkie Get* | `userDict.TryGetValue(...) ? user.FullName : string.Empty` | helper `ResolveUserName` |

### 4.4 Obsługa błędów
Sprawdzono wzorce:

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| `AddFileVersionCommentCommandHandler` | Po nieudanej autoryzacji rzuca `NotFoundApiException(nameof(ProjectFileVersion), ...)` zamiast `ForbiddenApiException` (kod sygnalizuje ukrywanie istnienia, ale komentarz mówi „authorization check") | mylący raport błędu |
| `DeleteProjectFileCommandHandler` | Jw. — `NotFound` zamiast `Forbidden` przy braku uprawnień | spójność / DX |
| `UpdateFileShareCommandHandler` | Jw. dla braku uprawnień. Brak `try/catch` wokół `notificationSender.EnqueueAsync` — błąd wysyłki przerywa transakcję, mimo że dane już zapisane | utrata stanu logicznego |
| `UploadProjectFilesCommandHandler` | Jw. dla braku uprawnień. `try/catch` wokół loop logu nie cofa zapisanych blobów po błędzie pojedynczego pliku → orphan blobs / orphan ProjectFile bez Version | spójność danych |
| `UploadProjectFileVersionCommandHandler` | Rzuca `ValidationApiException` gdy rozszerzenie nowej wersji ≠ oryginalnemu — to powinno być w validatorze (nie w handlerze) | naruszenie warstw |
| `CreatePackageAndUploadFilesCommandHandler` | Komentarz w kodzie: „handler is self-contained and must not rely on TransactionBehavior's SaveChangesAsync" — **ręcznie woła `SaveChangesAsync` 3 ×**, łamiąc kontrakt `TransactionBehavior` | brak atomowości (fail po 1. save → orphan package + files bez wersji) |
| `SharePackagesCommandHandler` | `ForbiddenApiException` użyty poprawnie ✔ | — |
| Wszystkie | Używają `?? throw new NotFoundApiException(nameof(X), id.ToString())` po `GetFirstBySearch` ✔ | — |
| `AddFileVersionComment`, `Delete`, `Update`, `Upload*`, `Share`, `CreatePackage` | Używają `== null` i `!= null` zamiast `is null` / `is not null` | konwencja projektu |

### 4.5 Zapytania do DB
| Handler | Problem | Ryzyko |
|---------|---------|--------|
| `AddFileVersionCommentCommandHandler` | **`projectFileVersionRepo.GetFirstBySearch(pfv => pfv.Id == request.VersionId)` — BEZ filtru `TenantId`/`ProjectId`** (dopiero później sprawdza `fileVersion.ProjectFileId != file.Id`) | **CRITICAL — leak cross-tenant przed kontrolą** |
| `AddFileVersionCommentCommandHandler` | `IRepository<ProjectFile>`, `IRepository<ProjectFileVersion>` używane tylko do odczytu | powinien być `IReadRepository<>` |
| `DeleteProjectFileCommandHandler` | `projectFileVersionRepo.GetBySearch(v => v.ProjectFileId == file.Id)` bez `TenantId`/`ProjectId` (tu plik już zwalidowany, ale reguła łamana) | spójność/defense in depth |
| `UpdateFileShareCommandHandler` | `userRepo` używane tylko do odczytu (`GetFirstBySearch`, `GetBySearch`) | powinien być `IReadRepository<User>` |
| `UpdateFileShareCommandHandler` | `sharedProjectFileRepo.GetBySearch(spf => spf.ProjectFilePackageId == packageId)` bez `TenantId`/`ProjectId` | defense in depth |
| `UpdateFileShareCommandValidator` | Pętla `foreach (userId) { await GetFirstBySearch(...) }` — N+1 query | wydajność |
| `SharePackagesCommandHandler` | Insert per użytkownik per paczka (M×N rekordów), `ExecuteDeleteAsync` w środku pętli | wydajność, zmieszane strategie write |
| `UploadProjectFilesCommandHandler` | `SaveChangesAsync` w pętli per plik → N round-tripów + brak atomowości | wydajność i spójność |
| `UploadProjectFileVersionCommandHandler` | `Include(pf => pf.Package).Include(pf => pf.Versions).Include(pf => pf.SharedWith)` — pobiera **wszystkie wersje** tylko żeby policzyć max | nadmiar danych — można `MAX(VersionNumber)` z projekcji |
| `CreatePackageAndUploadFilesCommandHandler` | 3 × `SaveChangesAsync`, brak transakcji obejmującej blob + DB | brak atomowości |
| `SharePackagesCommandValidator` | brak walidacji że `PackageIds` ≠ `Empty` per element | edge case |
| `GetPackageFilesQueryHandler` | OK — używa serwisów cache | — |
| `GetProjectFilePackagesQueryHandler` | OK | — |
| `GetVersionCommentsQueryHandler` | OK | — |
| `GetFileVersionsQueryHandler` | OK | — |

## BLOK 5 — WEB MODELE

### 5.1 Sealed record z explicit properties
| WebModel | sealed | record | properties z `init` | Pola tylko domenowe (bez EF) |
|----------|--------|--------|---------------------|------------------------------|
| `ProjectFileWeb` | ❌ | ✔ | ✔ (bez `required`) | ✔ |
| `ProjectFilePackageWeb` | ❌ | ✔ | ✔ | ✔ |
| `ProjectFileVersionWeb` | ❌ | ✔ | ✔ | ✔ |
| `ProjectFileVersionCommentWeb` | ❌ | ✔ | ✔ | ✔ |
| `SharedProjectFileWeb` | ❌ | ✔ | ✔ | ✔ |
| `SharedProjectFilePackageWeb` | ❌ | ✔ | ✔ | ✔ |

Żaden web model nie używa `sealed` ani `required`. Brak pól technicznych EF — OK.

### 5.2 Duplikacje
| Duplikowane pola | W modelach | Kandydat |
|------------------|------------|----------|
| `Id, OwnerId, OwnerName, CreatedAt` | `ProjectFileWeb`, `ProjectFilePackageWeb` | `OwnedResourceWeb` base record |
| `CurrentVersion, Versions, TotalVersions` | `ProjectFileWeb`, `SharedProjectFileWeb` | `VersionedFileWeb` base |
| `FileName, DisplayName, PackageName, ContentType, FileSizeBytes` | `ProjectFileWeb` (FileName/DisplayName/PackageName), `SharedProjectFileWeb` (wszystkie) | częściowy |

## BLOK 6 — PROBLEMY I REKOMENDACJE

### Krytyczne (bezpieczeństwo, autoryzacja, brak filtrowania po TenantId)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|--------------|
| K1 | `GetFirstBySearch(pfv => pfv.Id == request.VersionId)` — bez `TenantId`/`ProjectId` | `AddFileVersionComment/AddFileVersionCommentCommandHandler.cs` | Cross-tenant data leak (dostęp do wersji z obcego tenantu zanim padnie późniejszy check) | Dodać `&& pfv.TenantId == request.TenantId && pfv.ProjectId == request.ProjectId` |
| K2 | `projectFileVersionRepo.GetBySearch(v => v.ProjectFileId == file.Id)` bez `TenantId`/`ProjectId` | `DeleteProjectFile/DeleteProjectFileCommandHandler.cs` | Defense in depth — w razie regresji walidacji `file` możliwy delete blobów obcego tenantu | Dodać filtr po `TenantId`/`ProjectId` |
| K3 | `sharedProjectFileRepo.GetBySearch(spf => spf.ProjectFilePackageId == packageId)` bez `TenantId`/`ProjectId` | `UpdateFileShare/UpdateFileShareCommandHandler.cs` | jw. | Dodać filtr |
| K4 | `IRepository<>` (write) tam gdzie tylko odczyt → drobna ścieżka do nieumyślnego writeu | `AddFileVersionComment` (ProjectFile, ProjectFileVersion), `UpdateFileShare` (User), `Delete*` (ProjectFileVersion — write potrzebny) | mniejsze | Zmienić na `IReadRepository<>` |
| K5 | Brak walidacji `RequiredId` dla `TenantId` i `ProjectId` we **wszystkich** validatorach Files | wszystkie 11 validatorów | Możliwy `Guid.Empty` przepuszczany do warstwy DB | Wszędzie dodać `RuleFor(x => x.TenantId).RequiredId();` `RuleFor(x => x.ProjectId).RequiredId();` |
| K6 | `CreatePackageAndUploadFilesCommandHandler` — 3 × ręczne `SaveChangesAsync`, blob upload pomiędzy zapisami | `CreatePackageAndUploadFiles/...Handler.cs` | Brak atomowości: po fail w środku zostaje pakiet + ProjectFile bez wersji + osierocone bloby | Refaktor: jeden zapis na końcu, blob compensation lub delegacja do `IUnitOfWork` |
| K7 | `UploadProjectFilesCommandHandler` — `SaveChangesAsync` w pętli + try/catch tylko loguje | `UploadProjectFiles/...Handler.cs` | Częściowy upload bez rollbacka, orphan ProjectFile | jw. + cleanup blob |
| K8 | Autoryzacja w handlerach (`IsTenantOrProjectAdminAsync || IsOwner || HasShareAccess`) duplikowana ręcznie zamiast użycia pipeline | wszystkie write handlery (`Add`, `Delete`, `Update`, `Upload*`, `Share`) | Łatwa regresja przy nowym handlerze; autoryzacja rozproszona | Wprowadzić `IAssignedAuthorizableRequest` / `FileResourceAuthorizationBehavior` dla zasobu file/package |

### Wysokie (architektura, brak walidatorów, użycie var, niesealed)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|--------------|
| W1 | 4 z 7 Commands i 4 z 4 Queries pomijają `sealed` lub używają positional records (4 Queries + `DeleteProjectFile`) | wszystkie z BLOK 2.2 | Niespójność | Ujednolicić na `public sealed record X : IRequestCommand<Y> { public required Guid TenantId { get; init; } ... }` |
| W2 | Żadne Command/Query nie używa `required` | wszystkie 11 | Możliwe powstanie obiektu bez kluczowych pól | Dodać `required` do wszystkich `TenantId`, `ProjectId`, `FileId`, `PackageId`, `VersionId`, `Files`, `Comment`, `PackageName`, `PackageIds`, `SharedWithUserIds`, `Scope` |
| W3 | Wszystkie 11 handlerów nie są `sealed` | wszystkie | brak intencji „nieprzeznaczony do dziedziczenia" | Dodać `sealed` |
| W4 | Wszystkie 11 validatorów nie są `sealed` | wszystkie | jw. | Dodać `sealed` |
| W5 | 5 handlerów intensywnie używa `var` | `Add`, `Create*`, `Delete`, `Share`, `Update`, `UploadFileVersion` | Łamanie konwencji projektu (zakaz `var`) | Zamienić na typy explicite |
| W6 | Validatory nie używają `RequiredId()` / `UniqueIds()` (0/11) | wszystkie | DRY, spójność z resztą codebase | Wprowadzić `CommonValidationExtensions` we wszystkich plikach |
| W7 | `AddFileVersionCommentCommandValidator`, `CreatePackageAndUploadFilesCommandValidator`, `DeleteProjectFileCommandValidator` wstrzykują **nieużywane** zależności | jw. | Dezinformacja, narzut DI | Usunąć dead injection |
| W8 | `UpdateFileShareCommandValidator` — N+1 queries w `MustAsync` foreach | `UpdateFileShare/...Validator.cs` | wydajność | Pojedyncza query + sprawdzenie `Count` |
| W9 | `UpdateFileShareCommandHandler` ~430 linii, mieszanka logiki dyfu, zapisu, notyfikacji | `UpdateFileShare/...Handler.cs` | trudność utrzymania, testowania | Wydzielić `IFileShareDiffService` + osobny komponent notyfikacji |
| W10 | **Niespójność permission code**: `UploadProjectFiles` → `ProjectResourcesWrite`, `UploadProjectFileVersion` → `ProjectResourcesWriteShared`. `UpdateFileShare` używa `ProjectResourcesWrite` zamiast `ProjectResourcesShare` | `Upload*Command.cs`, `UpdateFileShareCommand.cs` | błędna macierz uprawnień | Ujednolicić — udostępnianie ⇒ `Share`, dodanie wersji ⇒ `Write` lub specjalny kod |
| W11 | Walidacja zgodności rozszerzenia (`new == old`) w handlerze, nie w validatorze | `UploadProjectFileVersionCommandHandler.cs` | mieszanie warstw | Przenieść do validatora (z `MustAsync` po pobraniu starego pliku albo nie weryfikować) |
| W12 | `GetPackageFilesQueryValidator.cs` zawiera klasę o nazwie `GetProjectFilePackagesQueryValidator` (kolizja) | `GetPackageFiles/GetPackageFilesQueryValidator.cs` | mylące dla rejestracji DI / search | Zmienić nazwę klasy na `GetPackageFilesQueryValidator` |
| W13 | Brak klas bazowych dla wspólnego zestawu pól `TenantId`/`ProjectId`/`FileId`/`PackageId` | wszystkie | duplikacja | Wprowadzić `ProjectScopedRequestBase`, `FileScopedRequestBase`, `PackageScopedRequestBase` |
| W14 | Web modele nie są `sealed` i nie używają `required` | 6 plików w `WebModels/Files/` | Niespójność z wzorcem | Dodać `sealed`, `required` |

### Normalne (DRY, mapowania, czytelność)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|--------------|
| N1 | Pusty plik `ProjectFileDto.cs` | `Files/ProjectFileDto.cs` | bałagan | Usunąć |
| N2 | „Płytkie" `}` zamykające namespace + dodatkowy pusty blok w `UploadProjectFilesCommand.cs` i `CreatePackageAndUploadFilesCommand.cs` (nadmiarowy `}`) | jw. | czytelność | Sformatować |
| N3 | `== null` / `!= null` zamiast `is null` / `is not null` | wszystkie handlery | spójność konwencji | Zamienić |
| N4 | Mieszanka komunikatów / komentarzy PL/EN | wszystkie | spójność | Jeden język (EN dla komunikatów walidacji, PL/EN dla komentarzy konsekwentnie) |
| N5 | Powtarzane mapowanie `ProjectFileVersionDto → ProjectFileVersionWeb` w 2 plikach (`GetFileVersionsQueryHandler.MapToVersionWeb`, `GetPackageFilesQueryHandler.MapToProjectFileWeb`) | jw. | DRY | Wydzielić `IFileVersionWebMapper` |
| N6 | Powtarzane mapowanie `User → FullName` z `userDict.TryGetValue` w 4 handlerach | wszystkie Get* | DRY | helper `ResolveUserName(userDict, id)` |
| N7 | Inline tworzenie `ProjectFilePackageWeb` w `GetProjectFilePackagesQueryHandler` i `ProjectFileVersionCommentWeb` w `GetVersionCommentsQueryHandler` (brak `MapTo...`) | jw. | spójność | Wydzielić prywatne `MapToPackageWeb`, `MapToCommentWeb` |
| N8 | Dziesiątki nieużywanych `using Entities.Models.{Chats,Costs,Notifications,Roles,Tenants,Users,WorkSchedules}` w handlerach i validatorach | większość plików | szum | Sprzątać `using` |
| N9 | Komentarze typu `// 5. ...` z błędną numeracją (np. `DeleteProjectFileCommandHandler` ma „4." i potem „5." dwa razy) | `Delete*Handler.cs`, `Add*Handler.cs` | czytelność | Poprawić |
| N10 | `Files.Any()` zamiast `Files.Count > 0` | `CreatePackage*Handler`, `Update*Handler` | drobne | Zamienić |
| N11 | `BeValidExtension`/`BeValidContentType` zduplikowane między `UploadProjectFilesCommandValidator` a `CreatePackageAndUploadFilesCommandValidator` | jw. | DRY | extension methods (BLOK 3.4) |
| N12 | `GetProjectFilePackagesQueryValidator.cs` ma niepotrzebny `using CQRS.Files.GetProjectFilePackages;` (ten sam namespace) | jw. | drobne | Usunąć |
| N13 | `UpdateFileShareCommandHandler` — `wasGranted`/`denyWasRemoved`/`wasRevoked`/`allowWasRemoved` można czytelniej opakować w extension method | jw. | czytelność | helpery |

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Liczba plików ogółem | 35 |
| Liczba Commands | 7 |
| Liczba Queries | 4 |
| Liczba Walidatorów | 11 |
| Liczba Handlerów | 11 |
| Liczba Web modeli | 6 |
| Klasy bazowe domeny | 0 |
| Pokrycie walidatorami | 100 % (11/11) |
| Pokrycie walidacją `TenantId`/`ProjectId` (`RequiredId`) | 0 % (0/11) |
| Commands/Queries `sealed` | 5/11 (~45 %) |
| Commands/Queries z positional params | 5/11 (~45 %) — wszystkie 4 Queries + `DeleteProjectFileCommand` |
| Commands/Queries używające `required` | 0/11 (0 %) |
| Handlery `sealed` | 0/11 (0 %) |
| Handlery z `var` | 7/11 (~64 %) |
| Validatory `sealed` | 0/11 (0 %) |
| Validatory używające `CommonValidationExtensions` | 0/11 (0 %) |
| Web modele `sealed` | 0/6 (0 %) |
| Web modele z `required` | 0/6 (0 %) |
| Problemy krytyczne | 8 |
| Problemy wysokie | 14 |
| Problemy normalne | 13 |

### Pytania domenowe wymagające decyzji człowieka

1. **PermissionCode dla operacji udostępniania**: czy `UpdateFileShareCommand` powinien używać `ProjectResourcesShare` (jak `SharePackagesCommand`) zamiast obecnego `ProjectResourcesWrite`?
2. **PermissionCode dla nowej wersji pliku**: dlaczego `UploadProjectFileVersion` używa `ProjectResourcesWriteShared` a `UploadProjectFiles`/`CreatePackageAndUploadFiles` używają `ProjectResourcesWrite`? Czy intencjonalnie nowe wersje są udostępniane szerzej?
3. **NotFoundApiException vs ForbiddenApiException** przy braku uprawnień (handlery: Add, Delete, UpdateFileShare, Upload*) — czy ukrywanie istnienia jest intencjonalne (security through obscurity), czy lepiej `Forbidden`?
4. **Atomowość uploadów (CreatePackageAndUploadFiles, UploadProjectFiles)** — czy dopuszczalny jest częściowy stan (orphan blob/file) przy błędzie pojedynczego pliku, czy operacja ma być all-or-nothing?
5. **AddFileVersionComment** wymaga `ProjectResourcesWrite` — czy użytkownicy z dostępem `READ_SHARED` powinni móc komentować?
