# API Fix 07 — Processing pipeline + Worker + SignalR Dispatcher

## Cel
Pełny async pipeline: worker kolejki, przetwarzanie AI, aktualizacja statusu, powiadomienia SignalR do **wszystkich członków projektu z uprawnieniem** `PROJECT.TECHNICAL_DOCUMENTATION`.

## Decyzje MVP
- Auto-retry max **3** (`MaxDequeueCount = 3`) — tylko w tym workerze (nie zmieniaj istniejących workerów z wartością 5)
- Po przekroczeniu limitu: `Status = Failed`, `ErrorMessage = "Auto-retry limit exceeded"`, SignalR notify
- **Wszystkie strony PDF** — bez limitu
- Odbiorcy SignalR: wszyscy członkowie projektu z modułem `TechnicalDocumentation` (+ admin projektu)
- Worker: `IServiceScopeFactory` per message (scoped serwisy)

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
- `.cursor/skills/api-services/SKILL.md`

## Zależności
- **api-fix-01** do **api-fix-06**

## Pliki referencyjne
- `src/Business/Implementation/Services/NotificationWorker.cs` — wzorzec worker
- `src/WebApi/Services/SignalRNotificationDispatcher.cs` — wzorzec dispatcher
- `src/Entities/Models/Projects/ProjectMemberModulePermission.cs`
- `src/Entities/Models/Users/User.cs` — `AzureAdB2CObjectId`

---

## 1. `ITechnicalDocumentationProcessingService`

Plik: `src/Business/Interfaces/Services/ITechnicalDocumentationProcessingService.cs`

```csharp
Task ProcessAsync(
    Guid documentationId,
    Guid tenantId,
    Guid projectId,
    CancellationToken cancellationToken);
```

## 2. `TechnicalDocumentationProcessingService`

Plik: `src/Business/Implementation/Services/TechnicalDocumentationProcessingService.cs`

Pipeline:
```
1. Load documentation + Files (Include), predykat TenantId+ProjectId+Id
2. Status → Processing, SaveChanges
3. Dla każdego pliku źródłowego:
   - Pobierz blob (IBlobStorageService.DownloadAsync)
   - PDF → IPdfToImageConverterService.ConvertAllPagesToJpegAsync (wszystkie strony)
   - JPG → dodaj jako pojedynczy obraz
   - Zbuduj List<TechnicalDocumentationImageInput> (FileName, PageNumber)
4. ITechnicalDocumentationOrchestratorService.ProcessImagesAsync(images)
5. Serializuj → DetailsJson
6. Status → Completed, CompletedAt = UtcNow, ErrorMessage = null
7. SaveChanges
8. ITechnicalDocumentationDispatcher.DispatchCompletedAsync(...)

--- on error ---
   Status → Failed, ErrorMessage = ex.Message (truncate jeśli za długi)
   SaveChanges
   DispatchCompletedAsync (status Failed)
```

## 3. `ITechnicalDocumentationDispatcher` + implementacja

### Interfejs
`src/Business/Interfaces/Services/ITechnicalDocumentationDispatcher.cs`

```csharp
Task DispatchCompletedAsync(
    TechnicalDocumentationProcessingResultDto payload,
    CancellationToken cancellationToken);
```

### `SignalRTechnicalDocumentationDispatcher`
Plik: `src/WebApi/Services/SignalRTechnicalDocumentationDispatcher.cs`

**Logika odbiorców (decyzja MVP):**
1. Pobierz `ProjectMemberModulePermission` gdzie `TenantId`, `ProjectId`, `Module == ProjectModule.TechnicalDocumentation`
2. Dodaj adminów projektu (`ProjectMember.IsAdmin` lub równoważnik — sprawdź model)
3. Join z `User` → lista `AzureAdB2CObjectId` (distinct, not null)
4. Dla każdego: `hubContext.Clients.User(azureAdB2CObjectId).ProcessingCompleted(payload)`

Alternatywa: `Clients.Users(IEnumerable<string> userIds)` jeśli dostępne w SignalR.

Wzorzec: `SignalRNotificationDispatcher` ale **wielu odbiorców** zamiast jednego.

## 4. `TechnicalDocumentationWorker`

Plik: `src/Business/Implementation/Services/TechnicalDocumentationWorker.cs`

```csharp
private const int MaxDequeueCount = 3;
```

- `BackgroundService`
- `IServiceScopeFactory` — twórz scope per message
- `EnsureQueueAsync(QueueNames.TechnicalDocumentationProcess)`
- Dequeue → deserialize `TechnicalDocumentationQueueMessageDto`
- Jeśli `DequeueCount > 3`:
  - Ustaw dokumentację `Failed`, `ErrorMessage = "Auto-retry limit exceeded"`
  - Dispatch SignalR
  - Delete message
- Else: wywołaj `ITechnicalDocumentationProcessingService.ProcessAsync`
- Przy auto-retry: inkrementuj `AutoRetryCount` na encji przed processing (lub po failure — ustal jedną konwencję)
- Delete message po sukcesie
- Visibility timeout kolejki: rozważ 30 min (konfiguracja w `QueueStorageService` jeśli wspierane)

## 5. Rejestracja DI

W `ServiceCollectionExtensions.AddAppServices()`:
```csharp
services.AddHostedService<TechnicalDocumentationWorker>();
services.AddScoped<ITechnicalDocumentationProcessingService, TechnicalDocumentationProcessingService>();
services.AddScoped<ITechnicalDocumentationDispatcher, SignalRTechnicalDocumentationDispatcher>();
```

**Uwaga:** Dispatcher w WebApi — sprawdź czy `Business` może referencjonować WebApi (NIE). Dispatcher musi być w `WebApi/Services/`, interfejs w `Business/Interfaces/Services/`.

## Weryfikacja
```powershell
dotnet build --configuration Release
```

## Następny krok
Hub + kontroler w **api-fix-08**.
