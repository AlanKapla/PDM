# Files — podsumowanie refaktoru

Domena: `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Files/`
Audyt: [files-audit.md](files-audit.md)
Build końcowy: **0 błędów** (po każdym z 7 promptów).

## Wykonane prompty

| # | Plik | Zakres | Status |
|---|------|--------|--------|
| 01 | [files-fix-01.md](files-fix-01.md) | Bezpieczeństwo: cross-tenant filtry (K1, K2, K3), `IRepository` → `IReadRepository` (K4), `RequiredId()` w 11 validatorach (K5) | ✅ |
| 02 | [files-fix-02.md](files-fix-02.md) | Klasy bazowe `ProjectScopedFilesRequestBase`/`FileScopedRequestBase`/`PackageScopedRequestBase` + extensions `AllowedFileExtension`/`AllowedContentType`/`MaxFileSize`/`ValidScope` w `CommonValidationExtensions` | ✅ |
| 03 | [files-fix-03.md](files-fix-03.md) | Konwencja: 6 web modeli + 11 Commands/Queries + 11 validatorów + 11 handlerów → `sealed` + `required init`; usunięty pusty `ProjectFileDto.cs`; naprawiona nazwa `GetPackageFilesQueryValidator`; usunięte dead injection (3 validatory); `var` → explicit; `==/!= null` → `is null/is not null`; `Forbidden` zamiast `NotFound` przy nieudanej autoryzacji; PermissionCodes: `UpdateFileShare` → `Share`, `AddFileVersionComment` → `ReadShared` | ✅ |
| 04 | [files-fix-04.md](files-fix-04.md) | `IFileAccessGuard` (Wariant B) — usunięto duplikowaną ręczną kontrolę `IsAdmin || IsOwner || HasShareAccess` z 6 handlerów write | ✅ |
| 05 | [files-fix-05.md](files-fix-05.md) | Atomowość uploadów: usunięte ręczne `SaveChangesAsync` (3× w `CreatePackage`, pętla w `UploadFiles`); kompensacja blobów Azure w `try/catch`; optymalizacja `MAX(VersionNumber)` przez projekcję | ✅ |
| 06 | [files-fix-06.md](files-fix-06.md) | `UpdateFileShareCommandHandler` ~430 → 142 linii (`Handle()` 32 linii); wydzielony `IFileShareDiffService` (czysty) i `IFileShareNotificationService` (try/catch); N+1 w validatorze: pętla `MustAsync` → 1 zapytanie | ✅ |
| 07 | [files-fix-07.md](files-fix-07.md) | DRY: `IFileVersionWebMapper` (DI, używany w 2 handlerach), `ProjectMemberNameResolver` (static, w 4 handlerach), `MapToPackageWeb`/`MapToCommentWeb` jako prywatne metody | ✅ |

## Decyzje produktowe (przyjęte od człowieka)

- `UpdateFileShareCommand.PermissionCode`: **`ProjectResourcesShare`** (poprzednio `Write`).
- `AddFileVersionCommentCommand.PermissionCode`: **`ProjectResourcesReadShared`** (poprzednio `Write`).
- `UploadProjectFileVersionCommand.PermissionCode`: **bez zmian** (`WriteShared` — intencjonalne).
- Brak uprawnień → `ForbiddenApiException` (zamiast NotFound). NotFound zostaje gdy zasób faktycznie nie istnieje.
- Atomowość uploadów: **all-or-nothing** (DB rollback + blob compensation).

## Metryki przed → po

| Metryka | Przed | Po |
|---------|-------|----|
| Cross-tenant leak (K1) | 1 (krytyczny) | 0 |
| Defense-in-depth braki (K2, K3) | 2 | 0 |
| Pokrycie `RequiredId(TenantId/ProjectId)` | 0 / 11 (0 %) | 11 / 11 (100 %) |
| Commands/Queries `sealed` | 5 / 11 | 11 / 11 |
| Commands/Queries z `required` init | 0 / 11 | 11 / 11 |
| Handlery `sealed` | 0 / 11 | 11 / 11 |
| Validatory `sealed` | 0 / 11 | 11 / 11 |
| Web modele `sealed` + `required` | 0 / 6 | 6 / 6 |
| Validatory używające `CommonValidationExtensions` | 0 / 11 | 11 / 11 |
| Handlery z `var` | 7 / 11 | 0 (w głównych ścieżkach) |
| Ręczna autoryzacja `IsAdmin || Owner || Share` w handlerach | 6 wystąpień | 0 (przez `IFileAccessGuard`) |
| Atomowość uploadów (ręczne `SaveChangesAsync`) | 3 handlery (3+N+1) | 0 (transakcja MediatR + blob compensation) |
| Linie `UpdateFileShareCommandHandler.Handle()` | ~140 | 32 |
| Linie `UpdateFileShareCommandHandler` całość | ~430 | 142 |
| Duplikaty mapowania `ProjectFileVersion → Web` | 2 (kopia/wklej) | 0 (`IFileVersionWebMapper`) |
| Duplikaty `userDict.TryGetValue → FullName` | 4 handlery | 0 (`ProjectMemberNameResolver`) |
| Pusty `ProjectFileDto.cs` | tak | usunięty |
| Błędna nazwa `GetProjectFilePackagesQueryValidator` w pliku PackageFiles | tak | naprawiona |

## Nowe artefakty infrastruktury

**Klasy bazowe** — `CQRS/Files/_Shared/`:
- `ProjectScopedFilesRequestBase` — `TenantId` + `ProjectId`, `IAuthorizableRequest`, `GetResource()`.
- `FileScopedRequestBase` — dziedziczy + `FileId`.
- `PackageScopedRequestBase` — dziedziczy + `PackageId`.

**Walidacje** — `CQRS/Extensions/CommonValidationExtensions.cs`:
- `AllowedFileExtension(IReadOnlyCollection<string>)`
- `AllowedContentType(IReadOnlyCollection<string>)`
- `MaxFileSize(long)`
- `ValidScope()`

**Serwisy** — `Business/{Interfaces,Implementation}/Services{,/Files}/`:
- `IFileAccessGuard` / `FileAccessGuard` (+ enum `FileAccessKind { Read, Write, Share, Delete }`)
- `IFileShareDiffService` / `FileShareDiffService` (+ DTO `FileShareDiffInput`, `FileShareDiffResult`) — czysty, singleton
- `IFileShareNotificationService` / `FileShareNotificationService` (+ DTO `FileShareNotificationContext`) — scoped, try/catch
- `IFileVersionWebMapper` / `FileVersionWebMapper` — singleton
- `ProjectMemberNameResolver` (static helper)

Wszystkie zarejestrowane w `WebApi/Extensions/ServiceCollectionExtensions.cs`.

## Pliki dotknięte poza domeną Files
- `WebApi/Controllers/FileController.cs` — 5 konstrukcji query/command zaktualizowanych z positional na property initializer.
- `Business/Interfaces/Services/` — 4 nowe interfejsy.
- `Business/Implementation/Services/Files/` — 4 nowe implementacje.
- `WebApi/Extensions/ServiceCollectionExtensions.cs` — rejestracja nowych serwisów.
- `CQRS/Extensions/CommonValidationExtensions.cs` — nowe metody rozszerzające.

## Pozostałe / odłożone

- Refaktor klas bazowych globalnych (`ProjectScopedRequestBase` na poziomie wszystkich domen) — nie istnieje globalny wzorzec; klasy bazowe Files są lokalne. Decyzja na przyszły refaktor międzydomenowy.
- Pre-istniejące ostrzeżenia NuGet (NU1603/NU1902/NU1903 dla MailKit, MimeKit, Kiota.Abstractions, StackExchange.Redis) — niezwiązane z domeną Files, do osobnego zadania.
