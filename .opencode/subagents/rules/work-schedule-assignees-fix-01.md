# Prompt: work-schedule-assignees-fix-01 — Endpoint assignable + alerty konfliktów terminów

## Cel
1. Dedykowany endpoint do pobierania kandydatów do przypisania (członkowie projektu + kontrahenci tenanta) pod uprawnieniem harmonogramu (bez `TenantSettingsView`).
2. Endpoint sprawdzający konflikty terminów (osoba/kontrahent już przypisany do innej pracy w nakładającym się okresie).
3. UI: alert ostrzegawczy przy przypisywaniu + alert na stronie zaplanowanych prac użytkownika (więcej niż jedna praca w tym samym dniu).

## Skills (przeczytaj przed implementacją)
- `.opencode/skills/api-cqrs/SKILL.md`
- `.opencode/skills/api-controllers/SKILL.md`
- `.opencode/skills/api-validators/SKILL.md`
- `.opencode/skills/api-unit-tests/SKILL.md`
- `.opencode/skills/ui-api-client/SKILL.md`
- `.opencode/skills/ui-hooks/SKILL.md`
- `.opencode/skills/ui-components/SKILL.md`
- `.opencode/skills/ui-types/SKILL.md`
- `.opencode/skills/ui-unit-tests/SKILL.md`

## Kontekst obecny (NIE zgaduj — zweryfikuj Grep/Read)

### API
- Kontroler: `src/WebApi/Controllers/WorkScheduleController.cs`
  - Route: `api/tenants/{tenantId}/projects/{projectId}/work-schedule`
  - `PUT .../works/{workId}/assignments` → `SetWorkScheduleStageWorkAssignmentsCommand`
  - Policy: `PermissionCodes.ProjectSchedule`
- Handler przypisań: `CQRS/WorkSchedules/SetWorkScheduleStageWorkAssignments/`
- Encje: `WorkScheduleStageWorkAssignment`, `WorkScheduleStageWorkPeriod` (StartDate/EndDate)
- Członkowie projektu: `IUserService.GetProjectMembersAsync` / `GetProjectMembersQuery`
- Kontrahenci: `GetContractorsQuery` wymaga `TenantSettingsView` — stąd potrzeba dedykowanego endpointu pod `ProjectSchedule`
- Wzorzec query: inne foldery w `CQRS/WorkSchedules/`

### UI
- `GanttContext.tsx` ładuje osobno `projectApi.getProjectMembers` + `contractorApi.getAll` — zamień na nowy endpoint
- UI przypisań: `AssignmentsModal.tsx`, `GanttAssigneesPopover.tsx`, `GanttWorkRow.tsx`
- Zaplanowane prace: `pages/AssignedWorks.tsx` + `hooks/useMyWorks` + typy `UserAssignedWorkWeb` (okresy w `periods`)
- API client: `api/workScheduleApi.ts`
- Typy: `types/workSchedule.types.ts`

## Część A — API: GetWorkScheduleAssignableAssignees

### Endpoint
```
GET /api/tenants/{tenantId}/projects/{projectId}/work-schedule/assignable-assignees
[Authorize(Policy = PermissionCodes.ProjectSchedule)]
→ 200 WorkScheduleAssignableAssigneesWeb
```

### Web model (`Business/Interfaces/WebModels/WorkSchedules/`)
```csharp
public sealed record WorkScheduleAssignableAssigneesWeb(
    IReadOnlyList<WorkScheduleAssignableMemberWeb> Members,
    IReadOnlyList<WorkScheduleAssignableContractorWeb> Contractors);

public sealed record WorkScheduleAssignableMemberWeb(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? CompanyName);

public sealed record WorkScheduleAssignableContractorWeb(
    Guid Id,
    string Name);
```

### Query / Handler / Validator
- Folder: `CQRS/WorkSchedules/GetWorkScheduleAssignableAssignees/`
- `GetWorkScheduleAssignableAssigneesQuery` : `IRequestQuery<WorkScheduleAssignableAssigneesWeb>`, `IAuthorizableRequest`
  - `PermissionCode => PermissionCodes.ProjectSchedule`
  - Resource: TenantId + ProjectId
- Handler:
  - Members: przez istniejący `IUserService.GetProjectMembersAsync(tenantId, projectId)` (mapuj companyName jeśli dostępne)
  - Contractors: `IReadRepository<Contractor>` / istniejący serwis — tylko aktywne, nieusunięte kontrahenci tenanta (`TenantId`, `!IsDeleted` jeśli `DeletableEntity`)
  - Predykaty zawsze z `TenantId` (+ `ProjectId` dla members)
- Validator: `RequiredId()` na TenantId, ProjectId
- Kontroler: metoda w `WorkScheduleController`
- Testy: handler + controller (wzoruj na istniejących testach WorkSchedule)

## Część B — API: CheckWorkScheduleAssignmentConflicts

### Endpoint
```
POST /api/tenants/{tenantId}/projects/{projectId}/work-schedule/{workScheduleId}/stages/{stageId}/works/{workId}/assignment-conflicts
[Authorize(Policy = PermissionCodes.ProjectSchedule)]
Body: CheckWorkScheduleAssignmentConflictsCommand (UserIds, ContractorIds)
→ 200 WorkScheduleAssignmentConflictsWeb
```

Uwaga: to jest **Query-like Command** albo lepiej czysta **Query z body**. Preferuj Query:
`CheckWorkScheduleAssignmentConflictsQuery` z polami route + `List<Guid> UserIds` + `List<Guid> ContractorIds` w body (jak inne query z listami w tym projekcie). Jeśli w projekcie nie ma POST-query, użyj GET z query string albo POST zwracający 200 — ważne: **nie zapisuje nic**, tylko sprawdza.

### Web model
```csharp
public sealed record WorkScheduleAssignmentConflictsWeb(
    IReadOnlyList<WorkScheduleAssignmentConflictWeb> Conflicts);

public sealed record WorkScheduleAssignmentConflictWeb(
    Guid? UserId,
    Guid? ContractorId,
    string AssigneeName,
    Guid ConflictingWorkId,
    string ConflictingWorkName,
    Guid ConflictingWorkScheduleId,
    string ConflictingWorkScheduleName,
    Guid ConflictingProjectId,
    string ConflictingProjectName,
    DateOnly OverlapStart,
    DateOnly OverlapEnd);
```
(Jeśli projekt używa `DateTime` zamiast `DateOnly` dla periods — dopasuj do istniejącego typu w `WorkScheduleStageWorkPeriod`.)

### Logika konfliktów
1. Pobierz okresy (`StartDate`/`EndDate`) pracy `workId` (TenantId + ProjectId).
2. Jeśli brak okresów → pusta lista konfliktów.
3. Znajdź inne przypisania (`WorkScheduleStageWorkAssignment`) w **tym samym tenancie** gdzie:
   - `UserId` w podanych UserIds LUB `ContractorId` w podanych ContractorIds
   - `WorkScheduleStageWorkId != workId`
   - praca / harmonogram nieusunięty (jak w `GetUserAssignedWorksQueryHandler`)
4. Dla każdego takiego przypisania sprawdź nakładanie okresów z okresami bieżącej pracy:
   - overlap: `a.Start <= b.End && b.Start <= a.End`
5. Zwróć listę konfliktów z nazwami (praca, harmonogram, projekt, assignee).
6. **Nie blokuj** zapisu w `SetWorkScheduleStageWorkAssignments` — konflikt to tylko informacja (alert).

### Validator
- RequiredId na route ids
- UserIds / ContractorIds: UniqueIds (mogą być puste → pusty wynik)

### Testy
- Brak overlap → pusta lista
- Overlap user → konflikt
- Overlap contractor → konflikt
- Ta sama praca (wykluczona) → brak konfliktu
- Controller test

## Część C — UI: klient + Gantt

1. Typy w `workSchedule.types.ts` odpowiadające web modelom.
2. `workScheduleApi.ts`:
   - `getAssignableAssignees(tenantId, projectId)`
   - `checkAssignmentConflicts(tenantId, projectId, wsId, stageId, workId, userIds, contractorIds)`
3. `GanttContext.tsx`: zamiast `projectApi.getProjectMembers` + `contractorApi.getAll` użyj `getAssignableAssignees`. Zachowaj mapowanie do `GanttMember` / `GanttContractor`.
4. Przy zapisie przypisań (`setAssignments` / `AssignmentsModal` / `GanttAssigneesPopover` / `GanttWorkRow`):
   - Przed (lub zaraz po wyborze nowych osób — preferuj **przed save** gdy user klika Zapisz / toggle dodaje osobę) wywołaj `checkAssignmentConflicts` dla **nowo dodawanych** userIds/contractorIds (diff względem aktualnych assignees pracy).
   - Jeśli są konflikty → pokaż Chakra `AlertDialog` lub `Alert` (ostrzeżenie, nie error):
     - PL: poinformuj że osoba/kontrahent jest już przypisany do innej pracy w nakładającym się terminie; wypisz nazwę assignees + conflicting work + daty.
     - Akcje: „Anuluj” / „Przypisz mimo to” (kontynuuj save).
   - Użyj istniejących wzorców modali (`AppModal` / `AlertDialog` z Chakra) — bez inline styles w nowych komponentach Chakra; `GanttAssigneesPopover` ma inline styles (legacy) — nowy alert może być osobnym komponentem Chakra.
5. Nowy komponent np. `AssignmentConflictAlertDialog.tsx` (jeden plik = jeden komponent).

## Część D — UI: AssignedWorks (zaplanowane prace)

Na `AssignedWorks.tsx` (lub w `useMyWorks` / util):
1. Na podstawie listy prac użytkownika i ich `periods` wykryj dni kalendarzowe, w których użytkownik ma **więcej niż jedną** otwartą pracę (`!isClosed`) z nakładającymi się okresami (overlap dat).
2. Pokaż widoczny `Alert` (status=`warning`) na górze listy gdy wykryto takie konflikty, np.:
   - „W wybranych dniach masz więcej niż jedną zaplanowaną pracę.” + skrócona lista (data / nazwy prac).
3. Logika w utilu np. `utils/detectSameDayWorkConflicts.ts` + prosty test Vitest.
4. Nie blokuj UI — tylko informacja.

## Konwencje
### API
- Brak `var`
- `is null` / `is not null`
- `{}` na każdym bloku
- Handlery `sealed`, metody krótkie (~20 linii), orkiestracja w `Handle`
- `IReadRepository<T>` do odczytu
- Predykaty z TenantId (+ ProjectId gdzie dotyczy)
- Domain exceptions tylko gdy potrzeba (tu zwykle nie — pusta lista OK)
- Brak migracji DB

### UI
- Brak `any`
- Logika w hookach/utilach, komponenty renderują
- Kolory tokenami Chakra / `appColors`
- Teksty UI po polsku

## Poza zakresem
- Zmiana logiki zapisu `SetWorkScheduleStageWorkAssignments` (bez twardego blokowania)
- Migracje EF
- Zmiana uprawnień ProjectMembers / TenantSettings

## Definition of done
- Oba endpointy działają, build API OK
- Gantt pobiera assignable z nowego endpointu
- Alert konfliktów przy przypisaniu (można kontynuować)
- Alert na AssignedWorks przy >1 pracy tego samego dnia
- Testy jednostkowe dla handlerów konfliktów + util UI
- `dotnet build` / UI typecheck bez błędów w zmienionych obszarach
