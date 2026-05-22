# Files — Fix 03: Konwencja + porządki + decyzje produktowe

Cel: ujednolicić styl — sealed record { required init; }, sealed handlery/validatory, użycie klas bazowych z fix-02, użycie nowych extension methods walidacyjnych, zakaz var, is null, naprawa permission codes, Forbidden zamiast NotFound przy braku uprawnień, sprzątanie usingów i dead injection.

## Wymagania wstępne
- fix-01 i fix-02 zakończone (klasy bazowe i extensions istnieją).

## Decyzje człowieka (zaaplikuj):
- `UpdateFileShareCommand.PermissionCode` → `PermissionCodes.ProjectResourcesShare` (zamiast `Write`).
- `UploadProjectFileVersionCommand.PermissionCode` → **bez zmian** (zostaje `WriteShared` — intencjonalne).
- `AddFileVersionCommentCommand.PermissionCode` → zmień na `PermissionCodes.ProjectView` (lub odpowiednik READ_SHARED — sprawdź w `PermissionCodes`, najbliższy „read shared" kod).
- W handlerach wymień `NotFoundApiException` rzucany jako wynik nieudanej **autoryzacji** (kontroli dostępu po pobraniu zasobu) na `ForbiddenApiException`. NotFound zostaje TYLKO gdy zasób faktycznie nie istnieje w bazie.

## Zakres zmian

### 1) Commands/Queries — sealed record + required init
Pliki (11) — wszystkie Commands i Queries w `CQRS/Files/`.

Konwersja:
- Każdy `record` → `public sealed record`.
- Positional record (`DeleteProjectFileCommand`, wszystkie 4 Queries) → property style.
- Każda właściwość będąca polem identyfikującym/danymi wejściowymi → `public required Guid X { get; init; }` itd.
- Dziedzicz po klasach z fix-02 tam gdzie pasuje:
  - `ProjectScopedFilesRequestBase` (lub globalny ekwiwalent) — gdy tylko `TenantId` + `ProjectId`.
  - `FileScopedRequestBase` — gdy + `FileId`: `AddFileVersionCommentCommand`, `DeleteProjectFileCommand`, `UploadProjectFileVersionCommand`, `GetFileVersionsQuery`, `GetVersionCommentsQuery`.
  - `PackageScopedRequestBase` — gdy + `PackageId`: `GetPackageFilesQuery`, `UploadProjectFilesCommand` (sprawdź czy ma `ProjectFilePackageId` → analogicznie).
- `PermissionCode` jako `public override string PermissionCode => PermissionCodes.X;`
- `GetResource()` z bazy — nadpisuj tylko gdy bazowy wariant nie wystarcza (np. `GetResourceScope()` dla Queries).

Pamiętaj o decyzjach permission codes powyżej.

### 2) Web modele — sealed record + required init
Pliki (6) w `Business/Interfaces/WebModels/Files/`:
- `ProjectFileWeb`, `ProjectFilePackageWeb`, `ProjectFileVersionWeb`, `ProjectFileVersionCommentWeb`, `SharedProjectFileWeb`, `SharedProjectFilePackageWeb`.

Każdy: `public sealed record X { public required T Y { get; init; } ... }`.

Po zmianie sprawdź wszystkie miejsca tworzenia tych modeli (w handlerach Files i ewentualnie w innych domenach) — przejście na `required` może wymagać dopisania brakujących pól.

### 3) Validatory — sealed + użycie nowych extensions
Pliki (11):
- Wszystkie → `public sealed class XValidator : AbstractValidator<X>`.
- Naprawić nazwę klasy w `GetPackageFiles/GetPackageFilesQueryValidator.cs`: aktualnie `GetProjectFilePackagesQueryValidator` → zmień na `GetPackageFilesQueryValidator`.
- Usuń **wszystkie nieużywane wstrzyknięcia DI**:
  - `AddFileVersionCommentCommandValidator`: usuń `IRepository<ProjectFile>`, `IRepository<ProjectFileVersion>`, `ICurrentUser`.
  - `CreatePackageAndUploadFilesCommandValidator`: usuń `IReadRepository<Project>`.
  - `DeleteProjectFileCommandValidator`: usuń wszystkie 4 zależności.
- Wymień ręczne `BeValidExtension`/`BeValidContentType`/`Must(distinct)` na extensions z fix-02:
  - `AllowedFileExtension(...)`, `AllowedContentType(...)`, `MaxFileSize(...)`.
  - `UniqueIds()` w `SharePackagesCommandValidator` (dla `PackageIds` i `SharedWithUserIds`).
  - `ValidScope()` w 4 Query validators (zamiast `IsInEnum()`).
- Usuń nieużywane `using` (Entities.Models.Chats/Costs/Notifications/Roles/Tenants/Users/WorkSchedules, Microsoft.EntityFrameworkCore tam gdzie nie potrzebne, duplikaty namespace).

### 4) Handlery — sealed + zakaz var + is null + Forbidden + porządki
Pliki (11) — wszystkie Handlery w `CQRS/Files/`.

- Każdy handler: `public sealed class XHandler : IRequestHandler<...>`.
- Wszystkie `var` → typ explicit.
- `== null` → `is null`, `!= null` → `is not null`.
- `Files.Any()` → `Files.Count > 0` (gdzie kolekcja).
- Usuń nieużywane `using`.
- Tam gdzie handler rzuca `NotFoundApiException` jako wynik **nieudanej kontroli uprawnień** (np. po `IsTenantOrProjectAdminAsync || IsOwner || HasShareAccess`) — zmień na `ForbiddenApiException`. NotFound zostaje gdy `GetFirstBySearch` zwróci null.
- Popraw błędną numerację komentarzy (`// 4.`, `// 5.` powtórzone) w `DeleteProjectFile`, `AddFileVersionComment`.
- Pusty plik `Files/ProjectFileDto.cs` → usuń.
- Nadmiarowe `}` zamykające w `UploadProjectFilesCommand.cs` i `CreatePackageAndUploadFilesCommand.cs` (jeśli formatowanie jest złe) → popraw.

NIE refaktoruj jeszcze logiki autoryzacji do pipeline'u (to fix-04), NIE refaktoruj atomowości uploadów (fix-05), NIE rozbijaj `UpdateFileShareCommandHandler` (fix-06), NIE wydzielaj mapperów (fix-07).

## Po wykonaniu
Zbuduj solution. Zwróć raport: status buildu, lista zmodyfikowanych/usuniętych plików, blokery.
Wymień miejsca w innych domenach które wymagały dostosowania (np. konstrukcje WebModeli z `required` w innych handlerach).
