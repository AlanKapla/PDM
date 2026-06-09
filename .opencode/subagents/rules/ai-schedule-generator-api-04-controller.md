# API-04: Endpoint w WorkScheduleController + DI Registration

## Zadanie
1. Dodaj nowy endpoint w `WorkScheduleController.cs`
2. Zarejestruj `IWorkScheduleAIGeneratorService` w DI

## 1. Nowy endpoint w WorkScheduleController.cs

**Plik**: `WebApi/Controllers/WorkScheduleController.cs`

Dodaj przed zamykającym `}` klasy (przed linią 502):

```csharp
[HttpPost("{workScheduleId}/generate-from-ai")]
[Authorize(Policy = PermissionCodes.ProjectSchedule)]
public async Task<IActionResult> GenerateFromAI(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromRoute] Guid workScheduleId,
    [FromBody] GenerateScheduleFromEstimateAICommand command)
{
    command = command with { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
    WorkScheduleDetailsWeb result = await Send(command);
    return Ok(result);
}
```

Dodaj brakujący import na górze pliku (obok istniejących importów z CQRS.WorkSchedules):
```csharp
using CQRS.WorkSchedules.GenerateScheduleFromEstimateAI;
```

### Wzorzec do naśladowania
Wzoruj się na istniejącym endpointcie `SyncWorkScheduleWithEstimate` (linie 99-109):
- Ta sama ścieżka bazowa
- Ten sam Authorize policy
- Command z `with { TenantId, ProjectId, WorkScheduleId }`
- Zwraca `Ok(result)` z `WorkScheduleDetailsWeb`

## 2. Rejestracja DI

**Plik**: `WebApi/Extensions/ServiceCollectionExtensions.cs`

Znajdź sekcję gdzie rejestrowane są serwisy WorkSchedule (linie 389-393):
```csharp
services.AddScoped<IWorkScheduleSyncService, WorkScheduleSyncService>();
services.AddScoped<IWorkScheduleNotificationService, WorkScheduleNotificationService>();
services.AddScoped<IWorkScheduleCacheService, WorkScheduleCacheService>();
services.AddScoped<IWorkScheduleAccessService, WorkScheduleAccessService>();
services.AddScoped<WorkScheduleBuilder>();
```

Dodaj po tych liniach:
```csharp
services.AddScoped<IWorkScheduleAIGeneratorService, WorkScheduleAIGeneratorService>();
```

Dodaj brakujący import na górze pliku:
```csharp
using Business.Implementation.Services;
```
(lub sprawdź czy `WorkScheduleAIGeneratorService` znajduje się w już zaimportowanym namespace)

### Uwaga
Upewnij się że `Business.Implementation.Services` jest w usings — sprawdź plik, może być już zaimportowany przez inne serwisy WorkSchedule.
