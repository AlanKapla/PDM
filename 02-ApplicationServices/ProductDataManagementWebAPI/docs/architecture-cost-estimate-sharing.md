# Architektura udostępniania kosztorysów

> **Status:** Zaakceptowana. Wszystkie decyzje projektowe rozstrzygnięte.

---

## 1. Cel i zakres

Mechanizm udostępniania encji `CostEstimate` między użytkownikami projektu. System
zachowuje pełną izolację multi-tenant i jest spójny z istniejącym modelem uprawnień
(`PermissionCodes`, `ResourceScope`, `AccessService`) oraz wzorcem cachowania z
`ProjectFilesService`.

---

## 2. Rozstrzygnięte decyzje projektowe

| # | Pytanie | Decyzja |
|---|---|---|
| 1 | Co może edytować Shared user? | Tylko wartości pól z `IsReadonly = false`. Nie może dodawać / usuwać / reorderować grup i pozycji. |
| 2 | Soft-delete CE a wpisy `SharedCostEstimate` | Wpisy usuwane **fizycznie** w tym samym handlerze co soft-delete CE. |
| 3 | Zakres udostępniania | Wyłącznie do aktywnych memberów tego samego projektu. |
| 4 | Kto widzi listę udziałów | Tylko owner i admin projektu / tenanta. |
| 5 | Guard write endpoints | `ProjectResourcesWriteShared` jako jednolity dolny guard HTTP + CQRS. |

---

## 3. Model uprawnień

Żadne nowe `PermissionCodes` nie są dodawane — reużywamy istniejących.

| Akcja | Guard (`PermissionCode`) | Kto go posiada |
|---|---|---|
| Odczyt listy – wszystkich | `ProjectResourcesReadAll` | ProjectAdmin, TenantAdmin, SuperAdmin |
| Odczyt listy – moich | `ProjectResourcesRead` | każdy member projektu |
| Odczyt listy – udostępnionych mi | `ProjectResourcesReadShared` | każdy member projektu |
| Odczyt szczegółów | `ProjectResourcesReadSingle` | każdy member projektu |
| Wszelka edycja (dolny guard) | `ProjectResourcesWriteShared` | owner + shared member + admin |
| Udostępnianie | `ProjectResourcesShare` | handler weryfikuje: tylko owner lub admin |

---

## 4. Nowy enum: `CostEstimateAccessLevel`

```csharp
// src/Business/Interfaces/Constants/CostEstimateAccessLevel.cs

public enum CostEstimateAccessLevel
{
    None       = 0,  // brak dostępu
    ReadOnly   = 1,  // zarezerwowane
    Restricted = 2,  // shared member: tylko pola !IsReadonly, bez zmian struktury
    Full       = 3   // owner / admin: wszystkie pola + pełna struktura
}
```

Wyznaczany raz przez `CostEstimateAccessService.GetAccessLevelAsync`,
cachowany per `(userId, costEstimateId)` z TTL 15 min.

---

## 5. Nowa encja: `SharedCostEstimate`

```csharp
// src/Entities/Models/CostEstimates/SharedCostEstimate.cs

public class SharedCostEstimate : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CostEstimateId { get; set; }
    public Guid SharedByUserId { get; set; }
    public Guid SharedWithUserId { get; set; }
    public DateTime SharedAt { get; set; }

    public CostEstimate CostEstimate { get; set; } = default!;
    public User SharedByUser { get; set; } = default!;
    public User SharedWithUser { get; set; } = default!;
}
```

Model płaski (brak Allow/Deny jak w `SharedProjectFile`) — jeden rekord = dostęp `Restricted`.
Usuwany fizycznie przy Unshare oraz przy soft-delete powiązanego `CostEstimate`.

| Cecha | `SharedProjectFile` | `SharedCostEstimate` |
|---|---|---|
| Granularność | Paczka + plik + Allow/Deny | Cały kosztorys |
| Model dostępu | Zbiór Allow/Deny | Jeden rekord = dostęp |
| Cascade przy delete zasobu | Wpisy zostają | Fizycznie usuwane |

---

## 6. Guard write endpoints — `ProjectResourcesWriteShared`

### Problem

Owner ma `ProjectResourcesWrite`, shared member ma `ProjectResourcesWriteShared`.
Oba typy muszą trafiać do tych samych write endpointów. ASP.NET Core `[Authorize(Policy)]`
i `AuthorizationBehavior` w MediatR pipeline obsługują jeden kod — natywne OR nie istnieje.

### Decyzja

`ProjectResourcesWriteShared` jako jednolity guard na warstwie HTTP i CQRS:

```csharp
// Kontroler
[Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]

// Command (IAuthorizableRequest)
public string PermissionCode => PermissionCodes.ProjectResourcesWriteShared;
```

Handler wywołuje `GetAccessLevelAsync()` i steruje zakresem edycji (`Full` / `Restricted`).

### Invariant seeda ról (warunek konieczny)

Każda rola posiadająca `ProjectResourcesWrite` lub `ProjectResourcesWriteAll`
**musi też posiadać** `ProjectResourcesWriteShared`. Bez tego owner dostanie 403.
Invariant zachowywany w `RolePermissionSeederService`.

### Macierz decyzji

| Rola | `WriteShared`? | Guard | `AccessLevel` w handlerze |
|---|---|---|---|
| SuperAdmin | tak | ✅ | `Full` |
| TenantAdmin | tak | ✅ | `Full` |
| ProjectAdmin | tak | ✅ | `Full` |
| Owner | tak (invariant seeda) | ✅ | `Full` |
| Shared member | tak | ✅ | `Restricted` |
| Read-only member | nie | 403 | — |

---

## 7. Matryca dostępu

| Rola | Lista (scope) | Szczegóły | Pola `IsReadonly=true` | Pola `IsReadonly=false` | Struktura (add/del/reorder) | Share |
|---|---|---|---|---|---|---|
| SuperAdmin / TenantAdmin / ProjectAdmin | All | ✅ | ✅ | ✅ | ✅ | ✅ |
| Owner | Mine | ✅ | ✅ | ✅ | ✅ | ✅ |
| Shared member | Shared | ✅ | **403** | ✅ | **403** | **403** |
| Read-only member | Mine | własne | 403 | 403 | 403 | 403 |

---

## 8. Nowy serwis: `ICostEstimateAccessService`

```csharp
// src/Business/Interfaces/Services/ICostEstimateAccessService.cs

public interface ICostEstimateAccessService
{
    // Cache: ce:access:{tId}:{pId}:ids:{uId}:{scope}  TTL: 15 min
    Task<HashSet<Guid>> GetAccessibleCostEstimateIdsAsync(
        ICurrentUser currentUser,
        Guid tenantId,
        Guid projectId,
        ResourceScope scope,
        CancellationToken cancellationToken = default);

    // Cache: ce:access:{tId}:{pId}:level:{uId}:{ceId}  TTL: 15 min
    Task<CostEstimateAccessLevel> GetAccessLevelAsync(
        ICurrentUser currentUser,
        Guid tenantId,
        Guid projectId,
        Guid costEstimateId,
        CancellationToken cancellationToken = default);

    // Cache: ce:access:{tId}:{pId}:shares:{ceId}  TTL: 15 min
    Task<List<Guid>> GetSharedWithUserIdsAsync(
        Guid tenantId,
        Guid projectId,
        Guid costEstimateId,
        CancellationToken cancellationToken = default);

    Task InvalidateAccessCacheAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task InvalidateCostEstimateAccessCacheAsync(
        Guid tenantId,
        Guid projectId,
        Guid costEstimateId,
        CancellationToken cancellationToken = default);
}
```

### Logika `GetAccessibleCostEstimateIdsAsync`

```
All:
    ceRepository.GetIdsBySearchAsync(
        ce => ce.ProjectId == projectId && ce.TenantId == tenantId && !ce.IsDeleted)

Mine:
    ceRepository.GetIdsBySearchAsync(
        ce => ce.ProjectId == projectId && ce.TenantId == tenantId
              && ce.OwnerId == currentUser.Id && !ce.IsDeleted)

Shared:
    sharedCeRepository.SelectToHashSetAsync(
        s => s.ProjectId == projectId && s.TenantId == tenantId
             && s.SharedWithUserId == currentUser.Id,
        s => s.CostEstimateId)
```

### Logika `GetAccessLevelAsync`

```
1. currentUser.IsSuperAdmin
   || await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, ct)
   → Full

2. ce.OwnerId == currentUser.Id
   → Full

3. await sharedCeRepository.AnyAsync(
       s => s.CostEstimateId == costEstimateId
            && s.SharedWithUserId == currentUser.Id, ct)
   → Restricted

4. else → None
```

---

## 9. Strategia cache

| Klucz | Typ | TTL | Kiedy invalidować |
|---|---|---|---|
| `ce:access:{tId}:{pId}:ids:{uId}:All/Mine/Shared` | `HashSet<Guid>` | 15 min | Share, Unshare, Delete CE |
| `ce:access:{tId}:{pId}:level:{uId}:{ceId}` | `IntWrapper` | 15 min | Share, Unshare, Delete CE |
| `ce:access:{tId}:{pId}:shares:{ceId}` | `List<Guid>` | 15 min | Share, Unshare |

Po Share / Unshare:
```
InvalidateCostEstimateAccessCacheAsync(tenantId, projectId, ceId)
    → usuwa klucze: level per (user, ceId), shares per ceId

RemoveCacheContainsAsync($"ce:access:{tenantId}:{projectId}:ids:*:Shared")
    → usuwa klucze IDs Shared dla wszystkich userów w projekcie
```

---

## 10. Zmiany w istniejącym CQRS

### 10.1 `ICostEstimateCacheService` — usunięcie `ownerId`

```diff
- Task<CostEstimate?> GetCostEstimateAsync(
-     Guid ceId, Guid tenantId, Guid projectId, Guid? ownerId, CancellationToken ct);
+ Task<CostEstimate?> GetCostEstimateAsync(
+     Guid ceId, Guid tenantId, Guid projectId, CancellationToken ct);
```

Walidacja własności przenosi się wyłącznie do `GetAccessLevelAsync`.

### 10.2 `GetCostEstimatesQueryHandler` — scope `Shared`

```csharp
case ResourceScope.Shared:
    var sharedIds = await ceAccessService.GetAccessibleCostEstimateIdsAsync(
        currentUser, request.TenantId, request.ProjectId,
        ResourceScope.Shared, cancellationToken);

    costEstimates = sharedIds.Count == 0
        ? Enumerable.Empty<CostEstimate>()
        : await costEstimateRepository.GetBySearch(
            ce => sharedIds.Contains(ce.Id) && !ce.IsDeleted && !ce.Template.IsDeleted,
            q => q.Include(ce => ce.Owner));
    break;
```

Usunąć `if (Scope == Shared) throw`.

### 10.3 `GetCostEstimateDetailsQueryHandler` — access check

```csharp
var accessLevel = await ceAccessService.GetAccessLevelAsync(
    currentUser, request.TenantId, request.ProjectId,
    request.CostEstimateId, cancellationToken);

if (accessLevel == CostEstimateAccessLevel.None)
    throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
```

Pole `AccessLevel` dodać do `CostEstimateDetailsWeb`.

### 10.4 Wszystkie write command handlers

**Dotyczy:** `UpsertItemField`, `UpsertGroupField`, `AddGroup`, `AddItem`,
`DeleteGroup`, `DeleteItem`, `ReorderGroups`, `ReorderItems`, `MoveItem`,
`UpdateCostEstimate`, `CopyCostEstimate`, `DeleteCostEstimate`.

Schemat (identyczny we wszystkich):

```csharp
// 1. CE z cache — bez ownerId
var ce = await cacheService.GetCostEstimateAsync(
    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
    ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

// 2. Tenant isolation
if (ce.TenantId != currentUser.ActiveTenantId)
    throw new ForbiddenApiException("Access to this cost estimate is not allowed.");

// 3. Access level
var accessLevel = await ceAccessService.GetAccessLevelAsync(
    currentUser, request.TenantId, request.ProjectId,
    request.CostEstimateId, cancellationToken);

if (accessLevel == CostEstimateAccessLevel.None)
    throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
```

Następnie, zależnie od komendy:

```csharp
// UpsertItemField / UpsertGroupField
if (accessLevel == CostEstimateAccessLevel.Restricted && fieldDef?.IsReadonly == true)
    throw new ForbiddenApiException("This field is read-only and cannot be modified.");

// AddGroup, AddItem, DeleteGroup, DeleteItem, ReorderGroups, ReorderItems, MoveItem
if (accessLevel == CostEstimateAccessLevel.Restricted)
    throw new ForbiddenApiException("Shared users cannot modify the cost estimate structure.");

// DeleteCostEstimate, CopyCostEstimate
if (accessLevel != CostEstimateAccessLevel.Full)
    throw new ForbiddenApiException("Only the owner or an admin can delete or copy this cost estimate.");
```

Zmiana `PermissionCode` we wszystkich write commands:

```diff
- public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
+ public string PermissionCode => PermissionCodes.ProjectResourcesWriteShared;
```

### 10.5 `DeleteCostEstimateCommandHandler` — cascade shares

```csharp
// Po soft-delete CE, przed końcem handlera
var shares = await sharedCeRepository.GetBySearch(
    s => s.CostEstimateId == request.CostEstimateId);

foreach (var share in shares)
    sharedCeRepository.Delete(share);

// TransactionBehavior zapisze atomowo — NIE wywołujemy SaveChangesAsync ręcznie

await ceAccessService.InvalidateCostEstimateAccessCacheAsync(
    request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);
await ceAccessService.InvalidateAccessCacheAsync(
    request.TenantId, request.ProjectId, cancellationToken);
```

---

## 11. Nowe CQRS

### 11.1 `ShareCostEstimateCommand`

```
src/CQRS/CostEstimates/ShareCostEstimate/
├── ShareCostEstimateCommand.cs
├── ShareCostEstimateCommandHandler.cs
└── ShareCostEstimateCommandValidator.cs
```

```csharp
public sealed record ShareCostEstimateCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public List<Guid> ShareWithUserIds { get; init; } = [];

    public string PermissionCode => PermissionCodes.ProjectResourcesShare;
    public ResourceRef GetResource() => new(TenantId, ProjectId);
}
```

**Validator:**
- `ShareWithUserIds` — `NotEmpty`, brak duplikatów
- `CostEstimateId` — `MustAsync`: istnieje w projekcie i tenancie, `!IsDeleted`
- `ShareWithUserIds` — `MustAsync`: każdy userId jest aktywnym memberem projektu

**Handler:**
```
1. CE z cache (bez ownerId).
2. ce.OwnerId != currentUser.Id && !IsTenantOrProjectAdminAsync
       → ForbiddenApiException("Only the owner or an admin can share this cost estimate.")
3. Pobierz istniejące SharedCostEstimate dla CostEstimateId.
4. Dla każdego userId: jeśli nie istnieje → utwórz SharedCostEstimate (idempotent).
5. InvalidateCostEstimateAccessCacheAsync + InvalidateAccessCacheAsync.
6. logger.LogInformation(...)
```

### 11.2 `UnshareCostEstimateCommand`

```
src/CQRS/CostEstimates/UnshareCostEstimate/
├── UnshareCostEstimateCommand.cs
├── UnshareCostEstimateCommandHandler.cs
└── UnshareCostEstimateCommandValidator.cs
```

```csharp
public sealed record UnshareCostEstimateCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public List<Guid> UnshareUserIds { get; init; } = [];

    public string PermissionCode => PermissionCodes.ProjectResourcesShare;
    public ResourceRef GetResource() => new(TenantId, ProjectId);
}
```

**Handler:** sprawdź owner || admin → pobierz wpisy → usuń fizycznie → invaliduj cache.

### 11.3 `GetCostEstimateSharesQuery`

```
src/CQRS/CostEstimates/GetCostEstimateShares/
├── GetCostEstimateSharesQuery.cs
├── GetCostEstimateSharesQueryHandler.cs
└── GetCostEstimateSharesQueryValidator.cs
```

```csharp
public sealed record GetCostEstimateSharesQuery(
    Guid TenantId,
    Guid ProjectId,
    Guid CostEstimateId
) : IRequestQuery<List<CostEstimateShareWeb>>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
    public ResourceRef GetResource() => new(TenantId, ProjectId);
}
```

**Handler:** sprawdź `accessLevel == Full || ce.OwnerId == currentUser.Id`
→ pobierz `SharedCostEstimate` → pobierz dane userów → zmapuj na `CostEstimateShareWeb`.

---

## 12. Nowe i zmienione DTO

```csharp
// Wejście
public record ShareCostEstimateRequestWeb(List<Guid> UserIds);
public record UnshareCostEstimateRequestWeb(List<Guid> UserIds);

// Wyjście GET /shares
public record CostEstimateShareWeb(
    Guid UserId,
    string FullName,
    string Email,
    DateTime SharedAt
);
```

Rozszerzenia istniejących rekordów:

```csharp
// CostEstimateListItemWeb — dwa nowe pola
bool IsSharedWithMe,   // true gdy scope == Shared (currentUser nie jest ownerem)
bool IsSharedByMe      // true gdy owner i istnieje przynajmniej jeden wpis SharedCostEstimate

// CostEstimateDetailsWeb — jedno nowe pole
CostEstimateAccessLevel AccessLevel
// Full / Restricted — frontend steruje edytowalnością pól
```

---

## 13. Zmiany w `CostEstimateController`

Zmiana `[Authorize]` na wszystkich write endpointach:

```diff
- [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
+ [Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]
```

Nowe endpointy:

```csharp
/// <summary>Share cost estimate with project members</summary>
[HttpPost("{ceId:guid}/shares")]
[Authorize(Policy = PermissionCodes.ProjectResourcesShare)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> ShareCostEstimate(
    [FromRoute] Guid tenantId, [FromRoute] Guid projectId,
    [FromRoute] Guid ceId, [FromBody] ShareCostEstimateRequestWeb body)

/// <summary>Remove cost estimate share from project members</summary>
[HttpDelete("{ceId:guid}/shares")]
[Authorize(Policy = PermissionCodes.ProjectResourcesShare)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> UnshareCostEstimate(
    [FromRoute] Guid tenantId, [FromRoute] Guid projectId,
    [FromRoute] Guid ceId, [FromBody] UnshareCostEstimateRequestWeb body)

/// <summary>Get list of users a cost estimate is shared with</summary>
[HttpGet("{ceId:guid}/shares")]
[Authorize(Policy = PermissionCodes.ProjectResourcesReadSingle)]
[ProducesResponseType(typeof(List<CostEstimateShareWeb>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> GetCostEstimateShares(
    [FromRoute] Guid tenantId, [FromRoute] Guid projectId,
    [FromRoute] Guid ceId)
```

---

## 14. Migracja bazy danych

```sql
CREATE TABLE SharedCostEstimates (
    Id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId         UNIQUEIDENTIFIER NOT NULL,
    ProjectId        UNIQUEIDENTIFIER NOT NULL,
    CostEstimateId   UNIQUEIDENTIFIER NOT NULL,
    SharedByUserId   UNIQUEIDENTIFIER NOT NULL,
    SharedWithUserId UNIQUEIDENTIFIER NOT NULL,
    SharedAt         DATETIME2        NOT NULL,

    CONSTRAINT FK_SharedCostEstimates_CostEstimate
        FOREIGN KEY (CostEstimateId) REFERENCES CostEstimates(Id),
    CONSTRAINT FK_SharedCostEstimates_SharedByUser
        FOREIGN KEY (SharedByUserId) REFERENCES Users(Id),
    CONSTRAINT FK_SharedCostEstimates_SharedWithUser
        FOREIGN KEY (SharedWithUserId) REFERENCES Users(Id),

    CONSTRAINT UQ_SharedCostEstimates
        UNIQUE (CostEstimateId, SharedWithUserId)
);

CREATE INDEX IX_SharedCostEstimates_SharedWithUserId_ProjectId
    ON SharedCostEstimates (SharedWithUserId, ProjectId);

CREATE INDEX IX_SharedCostEstimates_CostEstimateId
    ON SharedCostEstimates (CostEstimateId);
```

EF Core: `HasIndex(...).IsUnique()` dla unique constraint,
`OnDelete(DeleteBehavior.Restrict)` na FK do `CostEstimates` (kaskada ręczna w handlerze).

---

## 15. Rejestracja DI

```csharp
// AddAppRepositories
services.AddScoped<IReadRepository<SharedCostEstimate>, ReadRepository<SharedCostEstimate>>();
services.AddScoped<IRepository<SharedCostEstimate>, Repository<SharedCostEstimate>>();

// AddAppServices
services.AddScoped<ICostEstimateAccessService, CostEstimateAccessService>();
```

---

## 16. Kolejność implementacji

| Krok | Co | Dotknięte pliki |
|---|---|---|
| 1 | Encja + EF configuration | `SharedCostEstimate.cs`, `SharedCostEstimateConfiguration.cs` |
| 2 | Migracja EF Core | `Migrations/` |
| 3 | Enum `CostEstimateAccessLevel` | `Business.Interfaces.Constants/` |
| 4 | `ICostEstimateAccessService` + implementacja | `Business.Interfaces.Services/`, `Business.Implementation.Services/` |
| 5 | Rejestracja DI | `ServiceCollectionExtensions.cs` |
| 6 | Usunięcie `ownerId` z `ICostEstimateCacheService` | Interface + `CostEstimateCacheService.cs` |
| 7 | Write handlers — podmiana access check (×11) | Wszystkie handlery mutujące CE |
| 8 | `GetCostEstimatesQueryHandler` — scope Shared | `GetCostEstimatesQueryHandler.cs` |
| 9 | `GetCostEstimateDetailsQueryHandler` — access check + `AccessLevel` | `GetCostEstimateDetailsQueryHandler.cs` |
| 10 | `DeleteCostEstimateCommandHandler` — cascade shares | `DeleteCostEstimateCommandHandler.cs` |
| 11 | Nowe CQRS: Share, Unshare, GetShares | 9 nowych plików (3 × 3) |
| 12 | Nowe i zmienione DTO | `CostEstimateListItemWeb.cs`, `CostEstimateDetailsWeb.cs`, 3 nowe web modele |
| 13 | Kontroler — nowe endpointy + zmiana `[Authorize]` | `CostEstimateController.cs` |
| 14 | Invariant seed ról (`WriteShared` ⊆ role z `Write`) | `RolePermissionSeederService.cs` |
