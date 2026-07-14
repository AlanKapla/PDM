# Files — Fix 04: Pipeline autoryzacji (IAssignedAuthorizableRequest)

Cel: usunąć zduplikowaną ręczną autoryzację z 6 write handlerów Files i przenieść ją do pipeline'u MediatR.

## Wymagania wstępne
- fix-03 zakończony (Commands/Queries już są sealed + required + dziedziczą po klasach bazowych).

## Kontekst
Aktualnie handlery `AddFileVersionComment`, `DeleteProjectFile`, `UpdateFileShare`, `UploadProjectFiles`, `UploadProjectFileVersion`, `SharePackages` (częściowo) ręcznie wywołują kombinację:
```csharp
bool isAdmin = await accessService.IsTenantOrProjectAdminAsync(...);
bool isOwner = file.OwnerId == currentUser.Id;
bool hasShareAccess = await accessService.HasShareAccessAsync(...);
if (!isAdmin && !isOwner && !hasShareAccess) throw new NotFoundApiException(...);
```

Cel: przenieść tę logikę do pipeline'u przez nowe `IAssignedAuthorizableRequest` lub dedykowany `IFileAccessGuard`.

## Krok 1 — Rozpoznanie istniejącej infrastruktury

Sprawdź w `#codebase`:
- Czy istnieje już interfejs `IAssignedAuthorizableRequest` i `AssignedAuthorizationBehavior` (są w `CQRS/Behaviours/` wg `copilot-instructions.md`).
- Jak wygląda jego API — jakie pola udostępnia, jakie metody musi implementować request.
- Jak inne domeny (np. WorkSchedules, CostEstimates) korzystają z tego mechanizmu dla zasobów per-rekord (file/package).

Zwróć w raporcie krótkie podsumowanie tego co znalazłeś **przed** implementacją.

## Krok 2 — Wybór wariantu

Na bazie znaleziska wybierz JEDEN:

### Wariant A — rozszerzenie `IAssignedAuthorizableRequest`
Jeśli istniejący interfejs pozwala na wskazanie dowolnego zasobu (np. fileId/packageId) i pipeline potrafi pobrać jego owner/share — użyj go. Dodaj implementację w 6 Commands.

### Wariant B — nowy `IFileAccessGuard`
Jeśli istniejący mechanizm nie pasuje (np. wymaga konkretnego typu encji) — wprowadź serwis `IFileAccessGuard` w `Business/Implementation/Services/`:
```csharp
public interface IFileAccessGuard
{
    Task EnsureCanAccessFileAsync(Guid tenantId, Guid projectId, Guid fileId, FileAccessKind kind, CancellationToken ct);
    Task EnsureCanAccessPackageAsync(Guid tenantId, Guid projectId, Guid packageId, FileAccessKind kind, CancellationToken ct);
}
public enum FileAccessKind { Read, Write, Share, Delete }
```
Implementacja zawiera ten sam OR (`isAdmin || isOwner || hasShareAccess`) — ale w jednym miejscu. Handlery wstrzykują guarda i wołają `EnsureCanAccessXAsync` jako pierwszą linię.

Zarejestruj nowy serwis w DI (`ServiceCollectionExtensions`).

## Krok 3 — Migracja handlerów

Dotknij 6 handlerów:
- `AddFileVersionCommentCommandHandler` (FileAccessKind.Write — komentowanie)
- `DeleteProjectFileCommandHandler` (Delete)
- `UpdateFileShareCommandHandler` (Share)
- `UploadProjectFilesCommandHandler` (Write)
- `UploadProjectFileVersionCommandHandler` (Write)
- `SharePackagesCommandHandler` (Share)

W każdym:
- Usuń ręczną kombinację `IsTenantOrProjectAdminAsync || IsOwner || HasShareAccess`.
- Zastąp wywołaniem mechanizmu z kroku 2 (Wariant A — pipeline robi to automatycznie; Wariant B — wstrzyknij guarda i wywołaj go po pobraniu zasobu, lub przed jeśli logicznie pasuje).
- Usuń niepotrzebne wstrzyknięcia (`IAccessService`, `ICurrentUser` jeśli już niepotrzebne).
- Forbidden vs NotFound — zachowaj zgodność z fix-03 (autoryzacja → Forbidden, brak zasobu → NotFound).

## Reguły jakości
- Zakaz `var` — explicit types.
- `is null` / `is not null`.
- Wszystkie nowe klasy `sealed`.
- `IReadRepository<>` w guardzie (tylko odczyt).

## Po wykonaniu
Zbuduj solution. Zwróć raport: status buildu, wybrany wariant (A/B) z uzasadnieniem, lista zmodyfikowanych plików, lista usuniętych zależności DI w handlerach, blokery.
