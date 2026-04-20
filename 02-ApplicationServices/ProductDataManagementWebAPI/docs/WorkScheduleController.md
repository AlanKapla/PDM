# WorkSchedule API — Dokumentacja

## Przegląd

**Kontroler:** `WorkScheduleController`  
**Base route:** `api/tenants/{tenantId}/project/{projectId}/work-schedule`  
**Cel:** Zarządzanie harmonogramami prac w ramach projektu. Harmonogram składa się z drzewa etapów (`Stages`), zakresów prac (`Works`) oraz zależności między zakresami (`Dependencies`).

---

## Kody uprawnień

| Stała | Opis |
|-------|------|
| `ProjectView` | Podgląd zasobów projektu |
| `ProjectResourcesReadSingle` | Odczyt pełnego szczegółu pojedynczego zasobu |
| `ProjectResourcesWrite` | Tworzenie, edycja i usuwanie zasobów projektu |

---

## Spis endpointów

| # | Metoda | Ścieżka (względna) | Uprawnienie | Command / Query | Odpowiedź |
|---|--------|--------------------|------------|-----------------|-----------|
| 1 | POST | `/` | ProjectResourcesWrite | CreateWorkScheduleCommand | 201 + Guid |
| 2 | PUT | `/{workScheduleId}` | ProjectResourcesWrite | UpdateWorkScheduleCommand | 204 |
| 3 | GET | `/{scope}` | ProjectView | GetWorkSchedulesQuery | 200 + List<WorkScheduleSummaryWeb> |
| 4 | GET | `/details/{workScheduleId}` | ProjectResourcesReadSingle | GetWorkScheduleQuery | 200 + WorkScheduleDetailsWeb |
| 5 | GET | `/my` | ProjectView | GetMyWorkSchedulesQuery | 200 + List<MyWorkSchedulesTenantDto> |
| 6 | POST | `/{workScheduleId}/sync-with-estimate` | ProjectResourcesWrite | SyncWorkScheduleWithEstimateCommand | 204 |
| 7 | DELETE | `/{workScheduleId}` | ProjectResourcesWrite | DeleteWorkScheduleCommand | 204 |
| 8 | POST | `/{workScheduleId}/stages` | ProjectResourcesWrite | AddWorkScheduleStageCommand | 201 + Guid |
| 9 | DELETE | `/{workScheduleId}/stages/{stageId}` | ProjectResourcesWrite | DeleteWorkScheduleStageCommand | 204 |
| 10 | POST | `/{workScheduleId}/stages/{stageId}/works` | ProjectResourcesWrite | AddWorkScheduleStageWorkCommand | 201 + Guid |
| 11 | DELETE | `/{workScheduleId}/stages/{stageId}/works/{workId}` | ProjectResourcesWrite | DeleteWorkScheduleStageWorkCommand | 204 |
| 12 | PUT | `/{workScheduleId}/stages/{stageId}/works/{workId}/periods` | ProjectResourcesWrite | SetWorkScheduleStageWorkPeriodsCommand | 204 |
| 13 | PATCH | `/{workScheduleId}/stages/{stageId}/works/{workId}/is-closed` | ProjectResourcesWrite | SetWorkScheduleStageWorkIsClosedCommand | 204 |
| 14 | PATCH | `/{workScheduleId}/stages/{stageId}/works/{workId}/periods/{periodId}/is-closed` | ProjectResourcesWrite | SetWorkScheduleStageWorkPeriodIsClosedCommand | 204 |
| 15 | POST | `/{workScheduleId}/stages/{stageId}/works/{workId}/comments` | ProjectResourcesWrite | AddWorkScheduleStageWorkCommentCommand | 201 + Guid |
| 16 | PUT | `/{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId}` | ProjectResourcesWrite | UpdateWorkScheduleStageWorkCommentCommand | 204 |
| 17 | DELETE | `/{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId}` | ProjectResourcesWrite | DeleteWorkScheduleStageWorkCommentCommand | 204 |
| 18 | PUT | `/{workScheduleId}/dependencies` | ProjectResourcesWrite | SetWorkScheduleDependenciesCommand | 200 + WorkScheduleDetailsWeb |
| 19 | PUT | `/{workScheduleId}/stages/{stageId}/works/{workId}/assignments` | ProjectResourcesWrite | SetWorkScheduleStageWorkAssignmentsCommand | 204 |
| 20 | PATCH | `/{workScheduleId}/stages/{stageId}/name` | ProjectResourcesWrite | RenameWorkScheduleStageCommand | 204 |
| 21 | PUT | `/{workScheduleId}/stages/order` | ProjectResourcesWrite | ReorderWorkScheduleStagesCommand | 204 |
| 22 | PATCH | `/{workScheduleId}/stages/{stageId}/parent` | ProjectResourcesWrite | MoveWorkScheduleStageCommand | 204 |
| 23 | PATCH | `/{workScheduleId}/stages/{stageId}/works/{workId}/name` | ProjectResourcesWrite | RenameWorkScheduleStageWorkCommand | 204 |
| 24 | PUT | `/{workScheduleId}/stages/{stageId}/works/order` | ProjectResourcesWrite | ReorderWorkScheduleStageWorksCommand | 204 |
| 25 | PATCH | `/{workScheduleId}/stages/{stageId}/works/{workId}/stage` | ProjectResourcesWrite | MoveWorkScheduleStageWorkCommand | 204 |

---

## Endpointy — szczegóły

### 1. `POST /` — CreateWorkSchedule

**Uprawnienie:** `ProjectResourcesWrite`

#### `CreateWorkScheduleCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Body | `Name` | `string` | Nazwa harmonogramu |
| Body | `CostEstimateId` | `Guid?` | Opcjonalne powiązanie z kosztorysem |

**Walidacja:** `Name` — wymagany, maks. 255 znaków; jeśli `CostEstimateId` podany — kosztorys musi istnieć w obrębie tenanta/projektu.
**Odpowiedź:** `201 Created` + `Guid` — ID nowego harmonogramu; `Location` wskazuje na `/details/{workScheduleId}`.

---

### 2. `PUT /{workScheduleId}` — UpdateWorkSchedule

**Uprawnienie:** `ProjectResourcesWrite`

#### `UpdateWorkScheduleCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Body | `Name` | `string` | Nowa nazwa harmonogramu |

**Walidacja:** `Name` — wymagany, maks. 255 znaków.
**Odpowiedź:** `204 No Content`

---

### 3. `GET /{scope}` — GetWorkSchedules

**Uprawnienie:** `ProjectView`

#### `GetWorkSchedulesQuery`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `Scope` | `ResourceScope` | Zakres filtrowania — patrz: sekcja *Enumeracje* |

**Odpowiedź:** `200 OK` + `List<WorkScheduleSummaryWeb>`

---

### 4. `GET /details/{workScheduleId}` — GetWorkSchedule

**Uprawnienie:** `ProjectResourcesReadSingle`

#### `GetWorkScheduleQuery`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |

**Odpowiedź:** `200 OK` + `WorkScheduleDetailsWeb` — pełne drzewo etapów, prac, okresów, przypisań, komentarzy i zależności.

---

### 5. `GET /my` — GetMyWorkSchedules

**Uprawnienie:** `ProjectView`

#### `GetMyWorkSchedulesQuery`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Context | `UserId` | `Guid` | ID zalogowanego użytkownika (`ICurrentUser.Id`) |

**Odpowiedź:** `200 OK` + `List<MyWorkSchedulesTenantDto>` — harmonogramy, do których użytkownik jest przypisany.

---

### 6. `POST /{workScheduleId}/sync-with-estimate` — SyncWorkScheduleWithEstimate

**Uprawnienie:** `ProjectResourcesWrite`

#### `SyncWorkScheduleWithEstimateCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |

**Logika biznesowa:** Synchronizuje strukturę etapów i prac harmonogramu ze strukturą powiązanego kosztorysu — tworzy brakujące etapy/prace, usuwa te których nie ma w kosztorysie.
**Odpowiedź:** `204 No Content`

---

### 7. `DELETE /{workScheduleId}` — DeleteWorkSchedule

**Uprawnienie:** `ProjectResourcesWrite`

#### `DeleteWorkScheduleCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |

**Odpowiedź:** `204 No Content`

---

## Etapy (Stages)

### 8. `POST /{workScheduleId}/stages` — AddStage

**Uprawnienie:** `ProjectResourcesWrite`

#### `AddWorkScheduleStageCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Body | `ParentStageId` | `Guid?` | Opcjonalny ID etapu nadrzędnego (zagnieżdżenie) |
| Body | `CostEstimateGroupId` | `Guid?` | Opcjonalne powiązanie z grupą kosztorysu |
| Body | `Name` | `string` | Nazwa etapu |
| Body | `Order` | `int` | Kolejność wyświetlania |

**Odpowiedź:** `201 Created` + `Guid` — ID nowego etapu

---

### 9. `DELETE /{workScheduleId}/stages/{stageId}` — DeleteStage

**Uprawnienie:** `ProjectResourcesWrite`

#### `DeleteWorkScheduleStageCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `StageId` | `Guid` | ID etapu |

**Odpowiedź:** `204 No Content`

---

### 20. `PATCH /{workScheduleId}/stages/{stageId}/name` — RenameStage

**Uprawnienie:** `ProjectResourcesWrite`

#### `RenameWorkScheduleStageCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `StageId` | `Guid` | ID etapu |
| Body | `Name` | `string` | Nowa nazwa etapu |

**Walidacja:** `Name` — wymagany, maks. 255 znaków.
**Odpowiedź:** `204 No Content`

---

### 21. `PUT /{workScheduleId}/stages/order` — ReorderStages

**Uprawnienie:** `ProjectResourcesWrite`

#### `ReorderWorkScheduleStagesCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Body | `OrderedStageIds` | `List<Guid>` | Lista ID etapów w nowej kolejności |

**Walidacja:** `OrderedStageIds` — wymagany (nie pusty), brak duplikatów; wszystkie ID muszą należeć do danego harmonogramu — naruszenie zwraca `400 ValidationError`.

> **Uwaga:** Operacja **replace-all** — ustawia `Order = indeks` (0-based) dla każdego etapu zgodnie z kolejnością listy.

**Odpowiedź:** `204 No Content`

---

### 22. `PATCH /{workScheduleId}/stages/{stageId}/parent` — MoveStage

**Uprawnienie:** `ProjectResourcesWrite`

#### `MoveWorkScheduleStageCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `StageId` | `Guid` | ID etapu |
| Body | `ParentStageId` | `Guid?` | ID nowego etapu nadrzędnego; `null` = przeniesienie na poziom główny |

**Walidacja:** `ParentStageId` != `StageId` (etap nie może być swoim własnym rodzicem).

**Logika biznesowa:** Aktualizuje `ParentStageId` na etapie. Weryfikuje że nowy rodzic należy do tego samego harmonogramu i że zmiana nie tworzy cyklu w hierarchii — naruszenie zwraca `400 ValidationError`.
**Odpowiedź:** `204 No Content`

---

## Prace (Works)

### 10. `POST /{workScheduleId}/stages/{stageId}/works` — AddWork

**Uprawnienie:** `ProjectResourcesWrite`

#### `AddWorkScheduleStageWorkCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageId` | `Guid` | ID etapu (z trasy jako `stageId`) |
| Body | `CostEstimateItemId` | `Guid?` | Opcjonalne powiązanie z pozycją kosztorysu |
| Body | `Name` | `string` | Nazwa zakresu pracy |
| Body | `Order` | `int` | Kolejność wyświetlania |
| Body | `ColorRgb` | `string` | Kolor w formacie RGB hex |

**Odpowiedź:** `201 Created` + `Guid` — ID nowej pracy

---

### 11. `DELETE /{workScheduleId}/stages/{stageId}/works/{workId}` — DeleteWork

**Uprawnienie:** `ProjectResourcesWrite`

#### `DeleteWorkScheduleStageWorkCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageId` | `Guid` | ID etapu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy (z trasy jako `workId`) |

**Odpowiedź:** `204 No Content`

---

### 23. `PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/name` — RenameWork

**Uprawnienie:** `ProjectResourcesWrite`

#### `RenameWorkScheduleStageWorkCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageId` | `Guid` | ID etapu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy (z trasy jako `workId`) |
| Body | `Name` | `string` | Nowa nazwa zakresu pracy |

**Walidacja:** `Name` — wymagany, maks. 255 znaków.
**Odpowiedź:** `204 No Content`

---

### 24. `PUT /{workScheduleId}/stages/{stageId}/works/order` — ReorderWorks

**Uprawnienie:** `ProjectResourcesWrite`

#### `ReorderWorkScheduleStageWorksCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageId` | `Guid` | ID etapu (z trasy jako `stageId`) |
| Body | `OrderedWorkIds` | `List<Guid>` | Lista ID zakresów prac w nowej kolejności |

**Walidacja:** `OrderedWorkIds` — wymagany (nie pusty), brak duplikatów; wszystkie ID muszą należeć do danego etapu — naruszenie zwraca `400 ValidationError`.

> **Uwaga:** Operacja **replace-all** — ustawia `Order = indeks` (0-based) dla każdego zakresu pracy zgodnie z kolejnością listy.

**Odpowiedź:** `204 No Content`

---

### 25. `PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/stage` — MoveWork

**Uprawnienie:** `ProjectResourcesWrite`

#### `MoveWorkScheduleStageWorkCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy (z trasy jako `workId`); `stageId` z trasy nie jest przekazywany do komendy |
| Body | `TargetStageId` | `Guid` | ID docelowego etapu |
| Body | `TargetOrder` | `int` | Pozycja w docelowym etapie (0-based) |

**Walidacja:** `TargetStageId` — wymagany; `TargetOrder` — >= 0. Weryfikuje że `TargetStageId` należy do tego samego `WorkScheduleId` — naruszenie zwraca `400 ValidationError`.

**Logika biznesowa:** Przenosi zakres pracy do docelowego etapu (`WorkScheduleStageId = TargetStageId`), ustawia `Order = TargetOrder`, przesuwa pozostałe prace w etapie docelowym (gdzie `Order >= TargetOrder`) o 1 w górę.
**Odpowiedź:** `204 No Content`

---

## Okresy (Periods)

### 12. `PUT /{workScheduleId}/stages/{stageId}/works/{workId}/periods` — SetPeriods

**Uprawnienie:** `ProjectResourcesWrite`

#### `SetWorkScheduleStageWorkPeriodsCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy |
| Body | `Periods` | `List<WorkPeriodDto>` | Lista nowych okresów — zastępuje wszystkie istniejące |

> **Uwaga:** Operacja **replace-all** — usuwa wszystkie istniejące okresy danej pracy i tworzy nowe.

**Odpowiedź:** `204 No Content`

---

### 13. `PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/is-closed` — SetWorkIsClosed

**Uprawnienie:** `ProjectResourcesWrite`

#### `SetWorkScheduleStageWorkIsClosedCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy |
| Body | `IsClosed` | `bool` | Flaga zamknięcia zakresu pracy |

**Odpowiedź:** `204 No Content`

---

### 14. `PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/periods/{periodId}/is-closed` — SetPeriodIsClosed

**Uprawnienie:** `ProjectResourcesWrite`

#### `SetWorkScheduleStageWorkPeriodIsClosedCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy |
| Route | `PeriodId` | `Guid` | ID okresu |
| Body | `IsClosed` | `bool` | Flaga zamknięcia okresu |

**Odpowiedź:** `204 No Content`

---

## Komentarze (Comments)

### 15. `POST /{workScheduleId}/stages/{stageId}/works/{workId}/comments` — AddComment

**Uprawnienie:** `ProjectResourcesWrite`

#### `AddWorkScheduleStageWorkCommentCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy |
| Context | `CreatedByUserId` | `Guid` | ID autora komentarza (`ICurrentUser.Id`) |
| Body | `Content` | `string` | Treść komentarza |

**Odpowiedź:** `201 Created` + `Guid` — ID nowego komentarza

---

### 16. `PUT /{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId}` — UpdateComment

**Uprawnienie:** `ProjectResourcesWrite`

#### `UpdateWorkScheduleStageWorkCommentCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `CommentId` | `Guid` | ID komentarza |
| Context | `UpdatedByUserId` | `Guid` | ID edytującego (`ICurrentUser.Id`) |
| Body | `Content` | `string` | Nowa treść komentarza |

**Odpowiedź:** `204 No Content`

---

### 17. `DELETE /{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId}` — DeleteComment

**Uprawnienie:** `ProjectResourcesWrite`

#### `DeleteWorkScheduleStageWorkCommentCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `CommentId` | `Guid` | ID komentarza |
| Context | `UserId` | `Guid` | ID użytkownika — sprawdzenie autorstwa (`ICurrentUser.Id`) |

**Odpowiedź:** `204 No Content`

---

## Zależności (Dependencies)

### 18. `PUT /{workScheduleId}/dependencies` — SetDependencies

**Uprawnienie:** `ProjectResourcesWrite`

#### `SetWorkScheduleDependenciesCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Body | `Dependencies` | `List<WorkDependencyDto>` | Lista zależności — zastępuje wszystkie istniejące |

**Logika biznesowa:**
1. Usuwa wszystkie istniejące zależności harmonogramu.
2. Zapisuje nową listę zależności.
3. Dla każdej zależności oblicza wymagane przesunięcie okresów następnika (patrz: *Algorytm przesunięcia okresów*).
4. Zwraca przeliczoną, pełną strukturę harmonogramu.

**Odpowiedź:** `200 OK` + `WorkScheduleDetailsWeb`

---

## Przypisania (Assignments)

### 19. `PUT /{workScheduleId}/stages/{stageId}/works/{workId}/assignments` — SetAssignments

**Uprawnienie:** `ProjectResourcesWrite`

#### `SetWorkScheduleStageWorkAssignmentsCommand`

| Źródło | Pole | Typ | Opis |
|--------|------|-----|------|
| Route | `TenantId` | `Guid` | ID tenanta |
| Route | `ProjectId` | `Guid` | ID projektu |
| Route | `WorkScheduleId` | `Guid` | ID harmonogramu |
| Route | `WorkScheduleStageWorkId` | `Guid` | ID pracy (z trasy jako `workId`) |
| Body | `UserIds` | `List<Guid>` | Lista użytkowników do przypisania — zastępuje całą istniejącą listę |

**Walidacja:** `UserIds` — wymagany (nie null); każdy element — NotEmpty (brak pustych Guidów); pusta lista odpina wszystkich użytkowników; wszyscy podani `UserIds` muszą być członkami projektu — naruszenie zwraca `400 ValidationError`.

**Logika biznesowa:**
1. Oblicza diff między istniejącymi a nowymi przypisaniami.
2. Usuwa przypisania dla `UserId`, których nie ma w nowej liście (`ExecuteDeleteAsync`).
3. Dodaje przypisania dla `UserId`, których jeszcze nie było (`InsertRange`).
4. Wysyła powiadomienia do dodanych i usuniętych użytkowników (`SendAssignmentChangedNotificationsAsync`).

> **Uwaga:** Operacja **replace-all** — pusta lista `UserIds: []` usuwa wszystkich przypisanych użytkowników.

**Odpowiedź:** `204 No Content`

---

## Modele Web

### `WorkScheduleDetailsWeb`

Pełna reprezentacja harmonogramu z drzewem etapów, prac i metadanych.

```
WorkScheduleDetailsWeb
├── Id                    Guid
├── TenantId              Guid
├── ProjectId             Guid
├── CostEstimateId        Guid?
├── Name                  string
├── CreatedAt             DateTime
├── CreatedByUserId       Guid
├── CreatedByUserName     string
├── Stages                List<WorkScheduleStageWeb>
│   ├── Id                      Guid
│   ├── Name                    string
│   ├── Order                   int
│   ├── ParentStageId           Guid?
│   ├── CostEstimateGroupId     Guid?
│   ├── ChildStages             List<WorkScheduleStageWeb>   (rekurencja)
│   └── Works                   List<WorkScheduleStageWorkWeb>
│       ├── Id                      Guid
│       ├── Name                    string
│       ├── Order                   int
│       ├── ColorRgb                string
│       ├── IsClosed                bool
│       ├── PlannedStartDate        DateTime?
│       ├── PlannedEndDate          DateTime?
│       ├── Periods                 List<WorkScheduleStageWorkPeriodWeb>
│       │   ├── PeriodId        Guid
│       │   ├── StartDate       DateTime
│       │   ├── EndDate         DateTime
│       │   └── IsClosed        bool
│       ├── Assignees               List<WorkScheduleStageWorkAssigneeWeb>
│       │   ├── UserId          Guid
│       │   └── UserName        string
│       └── Comments                List<WorkScheduleStageWorkCommentWeb>
│           ├── Id                  Guid
│           ├── Content             string
│           ├── CreatedByUserId     Guid
│           ├── CreatedByUserName   string
│           └── CreatedAt           DateTime
└── Dependencies          List<WorkScheduleWorkDependencyWeb>
    ├── Id                  Guid
    ├── PredecessorWorkId   Guid
    ├── SuccessorWorkId     Guid
    ├── DependencyType      WorkDependencyType
    └── LagDays             int
```

---

### `WorkScheduleSummaryWeb`

Uproszczona karta harmonogramu zwracana przez endpoint listy (`GET /{scope}`).

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | `Guid` | ID harmonogramu |
| `CostEstimateId` | `Guid?` | Powiązany kosztorys |
| `Name` | `string` | Nazwa harmonogramu |
| `CreatedAt` | `DateTime` | Data utworzenia |
| `CreatedByUserId` | `Guid` | ID twórcy |
| `CreatedByUserName` | `string` | Imię i nazwisko twórcy |

---

### `MyWorkSchedulesTenantDto`

Hierarchia harmonogramów zwracana przez endpoint `GET /my`. Zawiera wyłącznie harmonogramy, do których zalogowany użytkownik jest przypisany.

```
MyWorkSchedulesTenantDto
├── TenantId        Guid
├── TenantName      string
└── Projects        List<MyWorkSchedulesProjectDto>
    ├── ProjectId       Guid
    ├── ProjectName     string
    └── WorkSchedules   List<MyWorkSchedulesItemDto>
        ├── WorkScheduleId    Guid
        └── WorkScheduleName  string
```

---

## Współdzielone DTO

### `WorkDependencyDto`

Używany w `SetWorkScheduleDependenciesCommand` (body).

| Pole | Typ | Opis |
|------|-----|------|
| `PredecessorWorkId` | `Guid` | ID poprzednika |
| `SuccessorWorkId` | `Guid` | ID następnika |
| `DependencyType` | `WorkDependencyType` | Typ zależności |
| `LagDays` | `int` | Opóźnienie w dniach (wartość ujemna = wyprzedzenie) |

### `WorkPeriodDto`

Używany w `SetWorkScheduleStageWorkPeriodsCommand` (body).

| Pole | Typ | Opis |
|------|-----|------|
| `StartDate` | `DateTime` | Data rozpoczęcia okresu |
| `EndDate` | `DateTime` | Data zakończenia okresu |

---

## Enumeracje

### `WorkDependencyType`

| Wartość | Nazwa | Warunek przesunięcia następnika |
|---------|-------|--------------------------------|
| 0 | `FinishToStart` | `successor.Start >= predecessor.End + LagDays` |
| 1 | `StartToStart` | `successor.Start >= predecessor.Start + LagDays` |
| 2 | `FinishToFinish` | `successor.End >= predecessor.End + LagDays` |
| 3 | `StartToFinish` | `successor.End >= predecessor.Start + LagDays` |

### `ResourceScope`

| Wartość | Opis |
|---------|------|
| `All` | Wszystkie harmonogramy projektu |
| `Mine` | Harmonogramy, do których zalogowany użytkownik jest przypisany |
| `Shared` | Harmonogramy współdzielone |

---

## Algorytm przesunięcia okresów

Wywoływany przez `SetWorkScheduleDependenciesCommandHandler` po zapisaniu nowych zależności.

```
dla każdej zależności dep (predecessor → successor):
    wymaganaDelta = oblicz(dep.Type, dep.LagDays,
                    predecessor.periods, successor.periods)
    shiftDays = max(0, ceil(wymaganaDelta))

dla każdej pracy będącej następnikiem:
    maxShift = max(shiftDays) ze wszystkich zależności wpływających na tę pracę
    jeśli maxShift > 0:
        dla każdego okresu pracy:
            StartDate += maxShift dni
            EndDate   += maxShift dni
        rekurencyjnie przesuń wszystkich następników tej pracy
```

---

## Konwencje kontrolera

1. **Pola z trasy nie w body** — `TenantId`, `ProjectId`, `WorkScheduleId` i inne identyfikatory z `[FromRoute]` są zawsze przepisywane do komendy przez `command with { ... }`. Klient nigdy nie podaje ich w body.
2. **Kontekst użytkownika** — `CreatedByUserId`, `UpdatedByUserId` i `UserId` są wypełniane z `ICurrentUser.Id` — nigdy z body żądania.
3. **Location header** — metody tworzące zasoby (`POST`) zwracają `CreatedAtAction` wskazujący na endpoint `GetWorkSchedule` (nr 4).
4. **`stageId` w trasie** — `stageId` jest przekazywany tylko do komend operujących bezpośrednio na etapach lub pracach. Komendy `is-closed`, `periods` i `comments` nie przyjmują `stageId` — odwołują się do pracy przez `WorkScheduleStageWorkId`.
