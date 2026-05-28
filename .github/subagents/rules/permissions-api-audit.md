# Audyt API — Uproszczenie Systemu Uprawnień Modułowych

**Feature:** Simplify module permissions (boolean access instead of AccessLevel enum)  
**Data audytu:** 2026-05-27  
**Audytor:** API Audit Agent

---

## BLOK 1 — Stan obecny

### Encja `ProjectMemberModulePermission`

**Plik:** `src/Entities/Models/Projects/ProjectMemberModulePermission.cs`

```csharp
public class ProjectMemberModulePermission
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectModule Module { get; set; }
    public ModuleAccessLevel AccessLevel { get; set; }   // ← DO USUNIĘCIA

    public ProjectMember ProjectMember { get; set; } = default!;
}
```

**Klucz główny (EF):** `(TenantId, ProjectId, UserId, Module)` — 4-kolumnowy PK  
**Klucz obcy:** `(TenantId, ProjectId, UserId)` → `ProjectMembers` z Cascade Delete  
**Tabela SQL:** `ProjectMemberModulePermissions`  
**DbSet:** `AppDbContext.ProjectMemberModulePermissions`

**Konfiguracja EF:** Brak osobnego pliku konfiguracyjnego — konfiguracja via konwencje, snapshot i migrację `20260527091258_migration-3.cs`.

---

### Enum `ModuleAccessLevel`

**Plik:** `src/Entities/Enums/ModuleAccessLevel.cs`

```
None=0, ViewShared=1, View=2, Read=3, WriteAssigned=4,
WriteShared=5, Write=6, WriteAll=7, Edit=8, Manage=9, Admin=10
```

**Używany w src w 6 plikach:**
- `ModulePermissionTranslator.cs` — parametr metody `Translate()`
- `AddProjectMemberCommand.cs` — property `ModulePermissionInput.AccessLevel`
- `AddProjectMemberCommandHandler.cs` — filtr `.Where(p => p.AccessLevel != None)` + insert
- `UpdateProjectMemberRoleCommand.cs` — property `ModulePermissionInput.AccessLevel`
- `UpdateProjectMemberRoleCommandHandler.cs` — filtr + insert
- `ProjectMemberModulePermission.cs` — kolumna encji

---

### `ModulePermissionTranslator`

**Plik:** `src/Business/Interfaces/Constants/ModulePermissionTranslator.cs`

Dwie metody publiczne:
- `Translate(ProjectModule module, ModuleAccessLevel level) → HashSet<string>` — translacja kombinacji modułu i poziomu
- `GetAllAdminPermissions() → HashSet<string>` — suma wszystkich kodów dla wszystkich modułów na poziomie Admin

**Mapowanie Admin (= nowa logika "ma dostęp"):**

| Moduł | Kody Admin |
|-------|-----------|
| Settings | ProjectSettingsView, ProjectSettingsEdit, ProjectStatusToggle, ProjectDashboardView |
| Members | ProjectMembersView, ProjectMembersManage |
| Files | ReadShared, ReadOwn, WriteAssigned, WriteShared, WriteOwn, ReadAll, WriteAll, Share |
| Estimates | ReadShared, ReadOwn, WriteAssigned, WriteShared, WriteOwn, ReadAll, WriteAll, Share |
| Costs | View, Accept, Write, Share |
| Schedule | ReadShared, ReadOwn, WriteAssigned, WriteOwn, ReadAll, WriteAll, Share |
| Dashboard | ProjectDashboardView |
| Chat | Read, Write, MembersManage, Rename, Delete |
| Tracker | View, Write |

> **Uwaga:** Schedule Admin nie zawiera `ProjectScheduleWriteShared` (pominięty między `WriteAssigned` i `WriteOwn`). Wymaga weryfikacji czy to celowe pominięcie.

**Używany w src w 1 pliku** (poza własnym plikiem):
- `CurrentUser.BuildProjectSnapshotAsync()` — 3 wywołania:
  1. TenantAdmin → `GetAllAdminPermissions()`
  2. IsAdmin member → `GetAllAdminPermissions()`
  3. Zwykły member → `Translate(mp.Module, mp.AccessLevel)` w pętli po `ModulePermissions`

---

### Endpoint `SyncWorkScheduleWithEstimate`

**Plik kontrolera:** `src/WebApi/Controllers/WorkScheduleController.cs` (linia ~101)

```csharp
[HttpPost("{workScheduleId}/sync-with-estimate")]
[Authorize(Policy = PermissionCodes.ProjectScheduleWriteOwn)]
public async Task<IActionResult> SyncWorkScheduleWithEstimate(...)
```

**Plik komendy:** `src/CQRS/WorkSchedules/SyncWorkScheduleWithEstimate/SyncWorkScheduleWithEstimateCommand.cs`

```csharp
public sealed record SyncWorkScheduleWithEstimateCommand : WorkScheduleCommandBase, IRequestCommand<Unit>
{
    public override string PermissionCode => PermissionCodes.ProjectScheduleWriteOwn;
}
```

**Aktualnie wymagany permission:** `PROJECT.SCHEDULE.WRITE_OWN`  
**Wniosek:** Endpoint JUŻ używa kodu ze modułu **Schedule** — zgodnie z wymaganiem wyjątku. W nowym modelu: posiadanie modułu Schedule automatycznie nadaje `ProjectScheduleWriteOwn` → **żadna zmiana tu nie jest wymagana.**

---

### `PermissionAuthorizationHandler` / `AuthorizationBehavior` + `AccessService`

**Plik Behavior:** `src/CQRS/Behaviours/AuthorizationBehavior.cs`  
**Plik Service:** `src/Business/Implementation/Services/AccessService.cs`

**Przepływ:**
1. `AuthorizationBehavior` wyciąga `PermissionCode` i `ResourceRef` z `IAuthorizableRequest`
2. Deleguje do `IAccessService.AuthorizeAsync(user, permissionCode, resource, resourceScope)`
3. `AccessService` na podstawie scope wywołuje `GetProjectSnapshotAsync()` lub `GetTenantSnapshotAsync()`
4. Sprawdza `projectSnapshot.ProjectPermissionCodes.Contains(permissionCode)`

**Snapshot building** (`CurrentUser.BuildProjectSnapshotAsync()`):
- Krok 3 (zwykły member): iteruje `membership.ModulePermissions` i dla każdego wywołuje `ModulePermissionTranslator.Translate(mp.Module, mp.AccessLevel)` — **tu jest klucz zmiany**

**Po zmianie:** Krok 3 wywoła `ModulePermissionTranslator.Translate(mp.Module)` — zawsze Admin-level.

---

### Web Models / DTO

**Plik:** `src/Business/Interfaces/WebModels/Projects/ProjectMemberWeb.cs`

```csharp
public sealed record ModulePermissionWeb
{
    public required int Module { get; init; }
    public required int AccessLevel { get; init; }   // ← DO USUNIĘCIA
}

public sealed record ProjectMemberWeb
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTime JoinedAt { get; init; }
    public required bool IsAdmin { get; init; }
    public IReadOnlyList<ModulePermissionWeb> ModulePermissions { get; init; } = Array.Empty<ModulePermissionWeb>();
}
```

---

### Commands / Validators

**`AddProjectMemberCommand.cs`** — `ModulePermissionInput` ma `Module + AccessLevel`  
**`UpdateProjectMemberRoleCommand.cs`** — DUPLIKUJE definicję `ModulePermissionInput` z `Module + AccessLevel` (powinno być wspólne)

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|-----------|------|
| Kolumna `AccessLevel` w encji | Entities | WYSOKI | Do usunięcia — nowy model nie używa poziomów |
| `ModulePermissionInput.AccessLevel` w Command | CQRS (2 pliki) | WYSOKI | `AddProjectMemberCommand` i `UpdateProjectMemberRoleCommand` mają `AccessLevel` w DTO wejściowym |
| `ModulePermissionWeb.AccessLevel` w DTO wyjściowym | Business | WYSOKI | `GetProjectMembers` zwraca `AccessLevel` do frontendu |
| Translator — sygnatura metody | Business | WYSOKI | `Translate(module, level)` zmienia się na `Translate(module)` |
| Handler — logika inserta | CQRS (2 pliki) | WYSOKI | Filtr `AccessLevel != None` i ustawianie `AccessLevel` do usunięcia |
| Duplikat `ModulePermissionInput` | CQRS | NISKI | Zdefiniowany oddzielnie w 2 namespace'ach |
| Brakujący mock w teście | Tests | ŚREDNI | `UpdateProjectMemberRoleCommandHandlerTests` nie przekazuje `IRepository<ProjectMemberModulePermission>` do konstruktora — prawdopodobnie nie kompiluje się |
| Migracja EF Core | Entities | WYSOKI | Nowa migracja: truncate + drop AccessLevel column |

---

## BLOK 3 — Zmiany w encjach/DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|----------------|
| `ProjectMemberModulePermission` | Usunąć kolumnę `AccessLevel (int)` | Usunięcie kolumny | **TAK** |
| `ProjectMemberModulePermission` | Wyczyścić dane (truncate) | Data migration | **TAK** (w tej samej migracji) |
| `ModuleAccessLevel` enum | Usunąć plik | Usunięcie enum | nie (tylko kod) |

**Szczegóły migracji:**
```sql
-- W Up():
DELETE FROM ProjectMemberModulePermissions;  -- wyczyść dane
ALTER TABLE ProjectMemberModulePermissions DROP COLUMN AccessLevel;

-- W Down():
ALTER TABLE ProjectMemberModulePermissions ADD AccessLevel int NOT NULL DEFAULT 0;
```

**PK POZOSTAJE NIEZMIENIONY:** `(TenantId, ProjectId, UserId, Module)` — Module nadal jest częścią klucza.

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|--------------|-----|------|---------|
| `AddProjectMemberCommand` | Modyfikacja | Usunąć `ModulePermissionInput.AccessLevel`, `ModulePermissions` staje się `IReadOnlyList<ProjectModule>` | `AddProjectMemberCommandHandler` |
| `UpdateProjectMemberRoleCommand` | Modyfikacja | Jak wyżej — usunąć `AccessLevel` z `ModulePermissionInput` | `UpdateProjectMemberRoleCommandHandler` |

> Brak potrzeby tworzenia nowych Commands. Istniejące `AddProjectMemberCommand` i `UpdateProjectMemberRoleCommand` pokrywają oba scenariusze.

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP Method | Nowy/Modyfikacja | Opis |
|----------|------------|-----------------|------|
| `POST /projects/{projectId}/members` | POST | Modyfikacja | Body zmienia się: `modulePermissions` = lista modułów (bez accessLevel) |
| `PATCH /projects/{projectId}/members/{userId}/role` | PATCH | Modyfikacja | Body zmienia się: `modulePermissions` = lista modułów (bez accessLevel) |
| `POST /{workScheduleId}/sync-with-estimate` | POST | **Bez zmian** | Już używa `ProjectScheduleWriteOwn` — poprawne |

---

## BLOK 6 — Zmiany w serwisach

| Serwis / Klasa | Interfejs | Nowy/Modyfikacja | Metody |
|----------------|-----------|-----------------|--------|
| `ModulePermissionTranslator` | — (static) | Modyfikacja | `Translate(module, level)` → `Translate(module)` + uproszczenie ciała |
| `CurrentUser` (BuildProjectSnapshotAsync) | `ICurrentUser` | Modyfikacja | Krok 3: wywołanie `Translate(mp.Module)` zamiast `Translate(mp.Module, mp.AccessLevel)` |

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | `UpdateProjectMemberRoleCommandHandlerTests` brakuje `Mock<IRepository<ProjectMemberModulePermission>>` w konstruktorze handlera | Tests | **WYSOKI** — prawdopodobnie nie kompiluje się | Dodać `_modulePermissionRepoMock` i przekazać do konstruktora |
| 2 | `ProjectScheduleWriteShared` pominięty w `TranslateSchedule(Admin)` | Business | **ŚREDNI** — niezamierzone pominięcie kodu uprawnienia dla harmonogramów | Zweryfikować z domeną czy to celowe; jeśli nie — dodać do `TranslateSchedule(Admin)` |
| 3 | Duplikat `ModulePermissionInput` w dwóch namespace'ach | CQRS | NISKI — brak błędów, ale duplikacja kodu | Przenieść do `CQRS/Projects/Shared/ModulePermissionInput.cs` lub usunąć po uproszczeniu do `IReadOnlyList<ProjectModule>` |
| 4 | Cache projektu (`GetProjectSnapshotAsync`) po zmianie uprawnień | Business | **ŚREDNI** — stary snapshot z `AccessLevel` zostanie unieważniony przez `BumpVersionAsync`, ale `AddProjectMemberCommandHandler` NIE bumps versji | `AddProjectMemberCommandHandler` powinien też wywołać `BumpVersionAsync` dla nowego członka |
| 5 | Usunięcie `ModuleAccessLevel` enum — sprawdzić czy jest używany w tests lub innych projektach | Entities | NISKI | Sprawdzić `tests/**` przed usunięciem |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Zmodyfikowane encje | 1 (`ProjectMemberModulePermission` — drop column) |
| Nowe Commands | 0 |
| Zmodyfikowane Commands | 2 (`AddProjectMemberCommand`, `UpdateProjectMemberRoleCommand`) |
| Nowe Queries | 0 |
| Zmodyfikowane Queries | 0 |
| Nowe endpointy | 0 |
| Zmodyfikowane endpointy | 2 (ciało requestu) |
| Nowe serwisy | 0 |
| Zmodyfikowane serwisy | 2 (`ModulePermissionTranslator`, `CurrentUser`) |
| Wymaga migracji DB | **TAK** (truncate + drop column) |
| Plików do modyfikacji | **10** |
| Pytania domenowe | 2 |

---

## LISTA WSZYSTKICH PLIKÓW DO MODYFIKACJI

| # | Plik | Zmiana |
|---|------|--------|
| 1 | `src/Entities/Models/Projects/ProjectMemberModulePermission.cs` | Usunąć property `AccessLevel (ModuleAccessLevel)` |
| 2 | `src/Entities/Enums/ModuleAccessLevel.cs` | Usunąć plik (jeśli enum nieużywany po pozostałych zmianach) |
| 3 | `src/Business/Interfaces/Constants/ModulePermissionTranslator.cs` | Zmienić `Translate(module, level)` → `Translate(module)` — zawsze zwraca Admin-level kody; uprościć implementację |
| 4 | `src/Business/Implementation/Model/CurrentUser.cs` | W `BuildProjectSnapshotAsync` (Krok 3): `Translate(mp.Module, mp.AccessLevel)` → `Translate(mp.Module)` |
| 5 | `src/CQRS/Projects/AddProjectMember/AddProjectMemberCommand.cs` | `ModulePermissionInput` — usunąć `AccessLevel`; lub zmienić `IReadOnlyList<ModulePermissionInput>` na `IReadOnlyList<ProjectModule>` |
| 6 | `src/CQRS/Projects/AddProjectMember/AddProjectMemberCommandHandler.cs` | Usunąć filtr `Where(p => p.AccessLevel != None)`; usunąć `AccessLevel = mp.AccessLevel` z inserta |
| 7 | `src/CQRS/Projects/UpdateProjectMemberRole/UpdateProjectMemberRoleCommand.cs` | Jak plik #5 — usunąć `AccessLevel` z `ModulePermissionInput` |
| 8 | `src/CQRS/Projects/UpdateProjectMemberRole/UpdateProjectMemberRoleCommandHandler.cs` | Jak plik #6 — usunąć filtr i `AccessLevel` z inserta |
| 9 | `src/Business/Interfaces/WebModels/Projects/ProjectMemberWeb.cs` | `ModulePermissionWeb` — usunąć property `AccessLevel (int)` |
| 10 | `src/CQRS/Projects/GetProjectMembers/GetProjectMembersQueryHandler.cs` | Mapowanie: usunąć `AccessLevel = (int)mp.AccessLevel` z `ModulePermissionWeb` |
| 11 | `src/Entities/Migrations/` (nowy plik) | Nowa migracja: truncate `ProjectMemberModulePermissions` + drop `AccessLevel` column |
| 12 | `src/Entities/Migrations/AppDbContextModelSnapshot.cs` | Zaktualizować snapshot (automatycznie przez `dotnet ef`) |

### Pliki testowe do modyfikacji

| # | Plik | Zmiana |
|---|------|--------|
| 13 | `tests/CQRS.Tests/Projects/AddProjectMemberCommandHandlerTests.cs` | `IRepository<ProjectMemberModulePermission>` mock nadal potrzebny; zaktualizować testy weryfikujące insert modułów (bez AccessLevel) |
| 14 | `tests/CQRS.Tests/Projects/UpdateProjectMemberRoleCommandHandlerTests.cs` | **BUG:** Dodać `Mock<IRepository<ProjectMemberModulePermission>> _modulePermissionRepoMock` i przekazać do konstruktora handlera |

---

## Pytania domenowe wymagające decyzji

1. **Schedule — brakujący `ProjectScheduleWriteShared`:** W `TranslateSchedule(Admin)` pominięty jest kod `PROJECT.SCHEDULE.WRITE_SHARED` (między `WriteAssigned` i `WriteOwn`). Czy to celowe pominięcie, czy bug? W nowym modelu Admin-level będzie wartością kanoniczną dla "ma dostęp do Schedule".

2. **`AddProjectMemberCommandHandler` — brak `BumpVersionAsync`:** Przy dodawaniu członka do projektu nie jest wywoływane `permissionsVersionService.BumpVersionAsync(userId)` (w odróżnieniu od `UpdateProjectMemberRoleCommandHandler` który to robi). Czy to celowe (nowy członek ma świeży cache)? Rozważyć czy dodać wywołanie dla spójności.
