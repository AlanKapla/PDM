# Files — Fix 06: Refaktor UpdateFileShareCommandHandler

Cel: rozbić handler ~430 linii na komponenty o jednej odpowiedzialności + naprawić N+1 w validatorze.

## Wymagania wstępne
- fix-03, fix-04 zakończone (handler powinien już być sealed, bez ręcznej autoryzacji, z Forbidden, z PermissionCode = Share).

## Zakres

### 1) Naprawa N+1 w `UpdateFileShareCommandValidator` (W8)
Plik: `02-ApplicationServices/.../CQRS/Files/UpdateFileShare/UpdateFileShareCommandValidator.cs`

Aktualnie `MustAsync` w pętli `foreach (userId)` wykonuje N kolejnych queries.
- Zamień na pojedynczą weryfikację: pobierz `userRepo.GetBySearch(u => userIds.Contains(u.Id) && u.TenantId == tenantId)` raz, sprawdź `Count == userIds.Count`.
- Użyj `RuleFor(x => x.SharedWithUserIds).MustAsync(...)` na całej liście, nie per element.

### 2) Refaktor `UpdateFileShareCommandHandler` (W9)
Plik: `02-ApplicationServices/.../CQRS/Files/UpdateFileShare/UpdateFileShareCommandHandler.cs`

Wydziel komponenty:

**a) `IFileShareDiffService`** — w `Business/Interfaces/Services/` lub `Business/Implementation/Services/Files/`.
```csharp
public interface IFileShareDiffService
{
    FileShareDiff Compute(
        IReadOnlyCollection<SharedProjectFile> existing,
        IReadOnlyCollection<Guid> targetUserIds,
        FileShareMode mode);
}
public sealed record FileShareDiff
{
    public required IReadOnlyCollection<Guid> ToGrant { get; init; }
    public required IReadOnlyCollection<Guid> ToRevoke { get; init; }
}
```
Cała logika `wasGranted`/`denyWasRemoved`/`wasRevoked`/`allowWasRemoved` (W13/N13) — wewnątrz tej klasy. **Bez DB, bez I/O** — czyste obliczenia, łatwe do testowania.

**b) `IFileShareNotificationService`** (lub równoważny komponent) — w `Business/Implementation/Services/Files/`.
Owijka nad `notificationSender.EnqueueAsync` z metodami:
- `NotifyShareGrantedAsync(...)`
- `NotifyShareRevokedAsync(...)`

Wewnątrz **try/catch** — błąd notyfikacji nie powinien przerywać operacji już zapisanej (loguj i kontynuuj).

**c) Handler — orkiestrator**
Po refaktorze `Handle()` ma być orkiestratorem (≤30 linii):
```csharp
public async Task<Unit> Handle(UpdateFileShareCommand request, CancellationToken ct)
{
    ProjectFilePackage package = await GetAndValidatePackageAsync(...);
    IReadOnlyList<SharedProjectFile> existing = await LoadExistingSharesAsync(...);
    FileShareDiff diff = shareDiffService.Compute(existing, request.SharedWithUserIds, request.Mode);
    await ApplyDiffAsync(diff, ct);
    await notifications.NotifyShareGrantedAsync(diff.ToGrant, ct);
    await notifications.NotifyShareRevokedAsync(diff.ToRevoke, ct);
    return Unit.Value;
}
```
Pomocnicze prywatne metody: `GetAndValidatePackageAsync`, `LoadExistingSharesAsync`, `ApplyDiffAsync`, `BuildSharedProjectFile`.

**d) Rejestracja DI**
Zarejestruj nowe serwisy w `WebApi/Extensions/ServiceCollectionExtensions` (scoped).

## Reguły jakości
- Zakaz `var` — explicit types.
- `is null` / `is not null`.
- Sealed handler + sealed serwisy + sealed validator.
- Każda publiczna metoda serwisu ma jeden cel.
- DiffService nie zależy od repo / I/O — wstrzyknij tylko czyste dane.

## Po wykonaniu
Zbuduj solution. Zwróć raport: status buildu, lista zmodyfikowanych/utworzonych plików, ile linii ma teraz `Handle()`, blokery.
