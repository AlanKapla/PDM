# ProjectCost — Fix 04: Handlery — wydzielenie serwisów, sealed, IReadRepository, var → explicit, cleanup

Cel: wyeliminować duplikację w handlerach Share/UpdateShare przez wydzielenie serwisów, ujednolicić handlery do wzorca (sealed, explicit types, IReadRepository).

Wymaga ukończenia fix-01, fix-02, fix-03.

## Zakres zmian

### 1. W7 — `IProjectCostAccessService` + implementacja

Lokalizacja: `src/Business/Interfaces/Services/IProjectCostAccessService.cs` + `src/Business/Implementation/Services/ProjectCostAccessService.cs`
(jeżeli w solution istnieje już katalog na services projektu/CQRS — użyć analogicznego miejsca; przy wątpliwości dopasować do struktury siostrzanych domen)

```csharp
public interface IProjectCostAccessService
{
    Task<bool> HasWriteAccessAsync(ProjectCost cost, Guid currentUserId, CancellationToken ct);
    Task<bool> HasShareAccessAsync(ProjectCost cost, Guid currentUserId, CancellationToken ct);
}
```

Logika: `isAdmin (tenant/project) || isOwner (cost.CreatedByUserId == currentUserId) || (dla share) isSharedWithMe`.
Wykorzystać istniejący `AccessService` / `IsTenantOrProjectAdminAsync` zamiast duplikować.

Zarejestrować w DI (`ServiceCollectionExtensions`).

W handlerach `Update`, `Delete`, `UpdateCostShare`, `ShareProjectCosts` zastąpić inline checki wywołaniem serwisu. Po niepowodzeniu — `throw new ForbiddenApiException();` (zgodnie z fix-01 W6).

### 2. W8 — `ProjectCostShareNotificationService`

Lokalizacja: `src/CQRS/ProjectCosts/Shared/ProjectCostShareNotificationService.cs` (lub w Business jeśli pasuje wzorzec).

Interfejs:
```csharp
internal interface IProjectCostShareNotificationService
{
    Task NotifyCostSharedAsync(
        ProjectCost cost,
        IReadOnlyCollection<Guid> targetUserIds,
        Guid actorUserId,
        CancellationToken ct);

    Task NotifyShareUpdatedAsync(
        ProjectCost cost,
        IReadOnlyCollection<Guid> addedUserIds,
        IReadOnlyCollection<Guid> removedUserIds,
        Guid actorUserId,
        CancellationToken ct);
}
```

Logika: zbudowanie `NotificationDto` (PL Title/Message), `NotificationPayloadHelper.CreatePayloadAsync`, `notificationSender.EnqueueAsync` — w jednym miejscu.

W `ShareProjectCostsCommandHandler` i `UpdateCostShareCommandHandler` zastąpić bloki notyfikacji wywołaniami serwisu.

Zarejestrować w DI.

### 3. W9 — `var` → explicit types

Pliki:
- `ShareProjectCostsCommandHandler.cs`
- `UpdateCostShareCommandHandler.cs`
- `ProjectCostController.cs` (N12)

Każde `var` zamienić na typ explicit (np. `List<Guid>`, `Project`, `IReadOnlyCollection<ProjectMember>`).

### 4. W10 — `IRepository` → `IReadRepository` w GetProjectCostsQueryHandler

Plik: `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs`

Zmienić `IRepository<ProjectCost>` i `IRepository<SharedProjectCost>` na `IReadRepository<...>`.

### 5. W13 — Logger w `RemoveAttachmentsAsync`

Plik: `src/CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs`

Wstrzyknąć `ILogger<ProjectCostHandlerBase>` (lub przekazać jako pole protected). Catch ma logować `LogWarning(ex, "Failed to delete blob {BlobName}", ...)` zamiast pustego swallow.

### 6. N3 — `sealed` dla wszystkich handlerów

```csharp
public sealed class CreateProjectCostCommandHandler : ...
```

Bazowy `ProjectCostHandlerBase` pozostaje `abstract` — nie sealed.

### 7. N4 — Cleanup nieużywanych usingów we wszystkich handlerach

Usunąć importy domen niezwiązanych (Chats, Files, Notifications [tam gdzie nie używane], Roles, Tenants, Users, WorkSchedules, itd.).

### 8. N9 — Zamiana `ArgumentOutOfRangeException` → `ValidationApiException`

Plik: `GetProjectCostsQueryHandler.cs`, `LoadCostsAsync` `default:` branch.

```csharp
default:
    throw new ValidationApiException($"Unsupported scope value: {request.Scope}");
```

(Po dodaniu `IsInEnum()` w validatorze ten branch powinien być nieosiągalny, ale zostaje jako ostatnia linia obrony.)

### 9. N11 — Komentarz w UpdateCostShare

Plik: `UpdateCostShareCommandHandler.cs`

Zaktualizować lub usunąć mylący komentarz `// 6. Save all changes` jeśli następuje już po `SaveChangesAsync`.

## Wymagania techniczne

- Zakaz `var`.
- Po zmianach: `dotnet build src\WebApi\WebApi.csproj` w `02-ApplicationServices/ProductDataManagementWebAPI`.
- Zarejestrować nowe serwisy w DI (najprawdopodobniej `WebApi/Extensions/ServiceCollectionExtensions.cs` lub analogicznie do innych serwisów domenowych — sprawdzić wzorzec w solution).
- Zwrócić raport: status buildu, lista plików, blokery, ewentualne odstępstwa.
