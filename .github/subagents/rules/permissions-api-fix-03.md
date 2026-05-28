# permissions-api-fix-03 — Kontrolery: zastąp wszystkie [Authorize(Policy)] nowymi kodami

## Zadanie

Zaktualizuj atrybuty `[Authorize(Policy = PermissionCodes.Xxx)]` we wszystkich kontrolerach projektowych — zastąp granularne kody jednym kodem per moduł.

## Mapa zastąpień (stary kod → nowy kod)

| Stary kod | Nowy kod |
|-----------|---------|
| `ProjectSettingsView` | `ProjectSettings` |
| `ProjectSettingsEdit` | `ProjectSettings` |
| `ProjectStatusToggle` | `ProjectSettings` |
| `ProjectDashboardView` | `ProjectDashboard` |
| `ProjectMembersView` | `ProjectMembers` |
| `ProjectMembersManage` | `ProjectMembers` |
| `ProjectFilesReadShared` | `ProjectFiles` |
| `ProjectFilesReadOwn` | `ProjectFiles` |
| `ProjectFilesReadAll` | `ProjectFiles` |
| `ProjectFilesWriteAssigned` | `ProjectFiles` |
| `ProjectFilesWriteShared` | `ProjectFiles` |
| `ProjectFilesWriteOwn` | `ProjectFiles` |
| `ProjectFilesWriteAll` | `ProjectFiles` |
| `ProjectFilesShare` | `ProjectFiles` |
| `ProjectEstimatesReadShared` | `ProjectEstimates` |
| `ProjectEstimatesReadOwn` | `ProjectEstimates` |
| `ProjectEstimatesReadAll` | `ProjectEstimates` |
| `ProjectEstimatesWriteAssigned` | `ProjectEstimates` |
| `ProjectEstimatesWriteShared` | `ProjectEstimates` |
| `ProjectEstimatesWriteOwn` | `ProjectEstimates` |
| `ProjectEstimatesWriteAll` | `ProjectEstimates` |
| `ProjectEstimatesShare` | `ProjectEstimates` |
| `ProjectCostsView` | `ProjectCosts` |
| `ProjectCostsWrite` | `ProjectCosts` |
| `ProjectCostsAccept` | `ProjectCosts` |
| `ProjectCostsShare` | `ProjectCosts` |
| `ProjectScheduleReadShared` | `ProjectSchedule` |
| `ProjectScheduleReadOwn` | `ProjectSchedule` |
| `ProjectScheduleReadAll` | `ProjectSchedule` |
| `ProjectScheduleWriteAssigned` | `ProjectSchedule` |
| `ProjectScheduleWriteShared` | `ProjectSchedule` |
| `ProjectScheduleWriteOwn` | `ProjectSchedule` |
| `ProjectScheduleWriteAll` | `ProjectSchedule` |
| `ProjectScheduleShare` | `ProjectSchedule` |
| `ProjectTrackerView` | `ProjectTracker` |
| `ProjectTrackerWrite` | `ProjectTracker` |
| `ChatRead` | `Chat` |
| `ChatWrite` | `Chat` |
| `ChatMembersManage` | `Chat` |
| `ChatRename` | `Chat` |
| `ChatDelete` | `Chat` |

## Pliki do modyfikacji

1. `src/WebApi/Controllers/ProjectController.cs`
2. `src/WebApi/Controllers/CostEstimateController.cs`
3. `src/WebApi/Controllers/WorkScheduleController.cs`
4. `src/WebApi/Controllers/FileController.cs`
5. `src/WebApi/Controllers/TenantChatsController.cs`
6. `src/WebApi/Controllers/CostTrackerController.cs`
7. `src/WebApi/Controllers/ProjectDashboardController.cs`
8. `src/WebApi/Controllers/ProjectCostController.cs`

## Zasada zastąpienia

Dla każdego pliku: znajdź wszystkie atrybuty `[Authorize(Policy = PermissionCodes.XxxOldCode)]` i podmień według tabeli powyżej.

**Ważne:** Jeśli endpoint miał dwa różne kody (np. ReadShared i WriteOwn), oba zastępujemy tym samym nowym kodem modułu — nie ma duplikatów.

## Przykład — ProjectController.cs

Stare:
```csharp
[Authorize(Policy = PermissionCodes.ProjectSettingsEdit)]
public async Task<IActionResult> UpdateProjectSettings(...)

[Authorize(Policy = PermissionCodes.ProjectMembersView)]
public async Task<IActionResult> GetProjectMembers(...)

[Authorize(Policy = PermissionCodes.ProjectMembersManage)]
public async Task<IActionResult> AddProjectMember(...)

[Authorize(Policy = PermissionCodes.ProjectStatusToggle)]
public async Task<IActionResult> ToggleProjectStatus(...)
```

Nowe:
```csharp
[Authorize(Policy = PermissionCodes.ProjectSettings)]
public async Task<IActionResult> UpdateProjectSettings(...)

[Authorize(Policy = PermissionCodes.ProjectMembers)]
public async Task<IActionResult> GetProjectMembers(...)

[Authorize(Policy = PermissionCodes.ProjectMembers)]
public async Task<IActionResult> AddProjectMember(...)

[Authorize(Policy = PermissionCodes.ProjectSettings)]
public async Task<IActionResult> ToggleProjectStatus(...)
```

## Przykład — WorkScheduleController.cs

Stare:
```csharp
[Authorize(Policy = PermissionCodes.ProjectScheduleWriteOwn)]
[Authorize(Policy = PermissionCodes.ProjectScheduleReadShared)]
[Authorize(Policy = PermissionCodes.ProjectScheduleWriteAssigned)]
```

Nowe (wszystkie → jeden kod):
```csharp
[Authorize(Policy = PermissionCodes.ProjectSchedule)]
```

## Krok — CQRS Commands PermissionCode property

Znajdź wszystkie CQRS Commands implementujące `IAuthorizableRequest` z property `PermissionCode` zwracającym stary kod.

Szukaj pliku Pattern: `src/CQRS/**/*Command.cs` — każdy który ma:
```csharp
public string PermissionCode => PermissionCodes.ProjectXxxOldCode;
```

Przykłady do zaktualizowania:
- `SyncWorkScheduleWithEstimateCommand.cs`: `ProjectScheduleWriteOwn` → `ProjectSchedule`
- Każdy command z `ProjectEstimatesXxx` → `ProjectEstimates`
- Każdy command z `ProjectFilesXxx` → `ProjectFiles`
- Każdy command z `ProjectCostsXxx` → `ProjectCosts`
- Każdy command z `ProjectScheduleXxx` → `ProjectSchedule`
- Każdy command z `ChatXxx` → `Chat`
- Każdy command z `ProjectTrackerXxx` → `ProjectTracker`

Polecenie do znalezienia wszystkich takich plików:
```powershell
Get-ChildItem -Recurse src/CQRS -Filter "*.cs" | Select-String "PermissionCode =>" | Select Path, Line
```

## Weryfikacja

```powershell
dotnet build src/WebApi/WebApi.csproj 2>&1 | Select-Object -Last 8
```

Oczekiwany rezultat: Build succeeded. Błędy kompilacji powinny być tylko z powodu jeszcze niezmienionych plików poza kontrolerami — będą naprawiane w fix-04.
