# API — CostTrackerController & ProjectDashboardController

Base path: `api/tenants/{tenantId}/projects/{projectId}`

---

## ProjectDashboardController

Route prefix: `.../dashboard`  

---

### `GET .../dashboard`

Zwraca pełny dashboard kosztowo-czasowy projektu. Agreguje dane ze wszystkich kosztorysów, harmonogramów i kosztów niespietych.

**Query:** `GetCostTrackerByProjectQuery`

| Pole        | Typ    | Źródło       |
|-------------|--------|--------------|
| `tenantId`  | `Guid` | route        |
| `projectId` | `Guid` | route        |

**Odpowiedź: `200 OK` → `ProjectDashboardWeb`**

```
ProjectDashboardWeb
├── projectId                  Guid
├── generatedAt                DateTime (UTC)
├── referenceDate              DateTime (UTC — punkt odniesienia dla TimelineStatus)
│
├── financialSummary           ProjectFinancialSummaryWeb
│   ├── totalBudgetNet/Gross           — kosztorysy + rezerwa projektu
│   ├── estimateBudgetNet/Gross        — suma budżetów kosztorysów
│   ├── projectReserveBudgetNet/Gross  — Project.BudgetNet/Gross
│   ├── totalCostsNet/Gross            — spiete + niespiete
│   ├── linkedCostsNet/Gross           — koszty z WorkItemLinkId != null
│   ├── additionalCostsNet/Gross       — koszty z WorkItemLinkId = null
│   ├── deviationNet/Gross             — budżet − koszty (ujemna = przekroczenie)
│   ├── deviationPercent               — deviationNet / totalBudgetNet * 100
│   ├── coveredPercent                 — totalCostsNet / totalBudgetNet * 100
│   ├── isBudgetExceeded               bool
│   ├── financialStatus                FinancialStatus
│   ├── totalCostCount / linkedCostCount / additionalCostCount
│   ├── costEstimatesCount / costEstimatesWithCostsCount / costEstimatesOverBudgetCount
│   └── workSchedulesCount
│
├── timelineSummary            ProjectTimelineSummaryWeb
│   ├── totalWorkCount
│   ├── completedCount / completedLateCount / inProgressCount / notStartedCount / delayedCount
│   ├── overallStatus          TimelineStatus
│   ├── isDelayed / isCompleted
│   └── workSchedulesCount / activeSchedulesCount / completedSchedulesCount
│
├── costEstimateSummaries[]    CostEstimateSummaryWeb  (jeden wpis na kosztorys)
│   ├── costEstimateId / costEstimateName
│   ├── budgetNet/Gross / costsNet/Gross
│   ├── deviationNet/Gross / deviationPercent / coveredPercent / isBudgetExceeded
│   ├── costCount
│   ├── financialStatus        FinancialStatus
│   ├── timelineStatus         TimelineStatus  (NoSchedule gdy brak spięcia)
│   ├── hasLinkedSchedule      bool
│   ├── linkedWorkScheduleId   Guid?
│   ├── timeline               TimelineStatsWeb?  (null gdy brak harmonogramu)
│   ├── totalItemsCount / itemsWithCostsCount / itemsWithoutCostsCount
│   ├── itemsOverBudgetCount / itemsNearLimitCount
│   ├── groups[]               TrackerGroupWeb  (drzewo rekurencyjne)
│   │   ├── groupId / groupName / order
│   │   ├── budgetNet/Gross / costsNet/Gross
│   │   ├── deviationNet/Gross / deviationPercent / coveredPercent / isBudgetExceeded
│   │   ├── costCount
│   │   ├── financialStatus / timelineStatus / hasLinkedSchedule
│   │   ├── timeline           TimelineStatsWeb?
│   │   ├── totalItemsCount / itemsWithCostsCount / itemsWithoutCostsCount
│   │   ├── itemsOverBudgetCount / itemsNearLimitCount
│   │   ├── items[]            WorkItemLinkWeb  (liście — pozycje kosztorysu)
│   │   │   ├── workItemLinkId          Guid  (Id encji WorkItemLink)
│   │   │   ├── costEstimateItemId      Guid?
│   │   │   ├── workScheduleStageWorkId Guid?  (null = brak spięcia z harmonogramem)
│   │   │   ├── displayName / order
│   │   │   ├── budgetNet/Gross / costsNet/Gross
│   │   │   ├── deviationNet/Gross / deviationPercent / coveredPercent / isBudgetExceeded
│   │   │   ├── costCount
│   │   │   ├── financialStatus / timelineStatus / hasLinkedSchedule
│   │   │   ├── timeline       TimelineStatsWeb?
│   │   │   └── costs[]        TrackedCostWeb
│   │   ├── childGroups[]      TrackerGroupWeb  (rekurencja, dowolna głębokość)
│   │   └── additionalCosts    TrackerAdditionalCostsWeb
│   └── additionalCosts        TrackerAdditionalCostsWeb
│
├── scheduleSummaries[]        ScheduleSummaryWeb  (jeden wpis na harmonogram)
│   ├── workScheduleId / workScheduleName
│   ├── hasLinkedEstimate      bool
│   ├── linkedCostEstimateId   Guid?
│   ├── budgetNet/Gross / costsNet/Gross
│   ├── deviationNet/Gross / deviationPercent / coveredPercent / isBudgetExceeded
│   ├── costCount
│   ├── financialStatus / timelineStatus / hasLinkedSchedule
│   ├── timeline               TimelineStatsWeb?
│   ├── totalWorkItemsCount / workItemsWithCostsCount
│   ├── workItemsOverBudgetCount / workItemsNearLimitCount / workItemsDelayedCount
│   ├── stages[]               ScheduleStageWeb
│   │   ├── stageId / stageName / order
│   │   ├── budgetNet/Gross / costsNet/Gross / deviationNet/Gross / deviationPercent / coveredPercent
│   │   ├── financialStatus / timelineStatus / hasLinkedSchedule
│   │   ├── timeline           TimelineStatsWeb?
│   │   ├── totalWorkItemsCount / completedWorkItemsCount / delayedWorkItemsCount
│   │   ├── workItems[]        WorkItemLinkWeb  (ta sama struktura co groups[].items[])
│   │   └── additionalCosts    TrackerAdditionalCostsWeb
│   └── additionalCosts        TrackerAdditionalCostsWeb
│
└── projectAdditionalCosts     ProjectAdditionalCostsWeb
    ├── totalNet / totalGross
    ├── costsCount
    └── costs[]                TrackedCostWeb
```

**`TimelineStatsWeb`** (używany na każdym poziomie drzewa gdy `timeline != null`):

| Pole | Typ | Opis |
|------|-----|------|
| `plannedStart` / `plannedEnd` | `DateTime?` | Zakres z encji |
| `totalPlannedDays` | `double?` | `plannedEnd - plannedStart` w dniach |
| `totalWorkCount` | `int` | Liczba zakresów pracy w węźle |
| `completedCount` / `completedLateCount` / `inProgressCount` / `notStartedCount` / `delayedCount` | `int` | Liczniki per status |
| `progressPercent` | `decimal?` | `completedCount / totalWorkCount * 100` |
| `delayDays` | `double?` | Maks. dni opóźnienia |
| `overallStatus` | `TimelineStatus` | Agregat priorytetowy |
| `isDelayed` / `isCompleted` | `bool` | |

**`TrackedCostWeb`**:

| Pole | Typ |
|------|-----|
| `id` | `Guid` |
| `workItemLinkId` | `Guid?` |
| `costEstimateId` / `costEstimateItemId` | `Guid?` |
| `isAdditional` | `bool` |
| `name` / `description` / `contractor` | `string?` |
| `net` / `gross` | `decimal?` |
| `date` | `DateTime?` |
| `createdAt` / `updatedAt` | `DateTime` |
| `attachments[]` | `TrackedCostAttachmentWeb[]` |

**Kody odpowiedzi:**

| Kod | Opis |
|-----|------|
| `200` | Sukces |
| `403` | Brak uprawnienia `ProjectResourcesReadSingle` |
| `404` | Projekt nie istnieje |

---

### Enum: `FinancialStatus`

| Wartość | Nazwa | Warunek |
|---------|-------|---------|
| `0` | `NoBudget` | `BudgetNet = null` |
| `1` | `NoCosts` | Budżet zdefiniowany, brak kosztów |
| `2` | `InProgress` | Koszty > 0 i ≤ 85% budżetu |
| `3` | `NearLimit` | Koszty > 85% i ≤ 100% budżetu |
| `4` | `OverBudget` | Koszty > budżet |

### Enum: `TimelineStatus`

| Wartość | Nazwa | Warunek (względem `referenceDate`) |
|---------|-------|-------------------------------------|
| `0` | `NoSchedule` | Brak powiązanego `WorkScheduleStageWork` |
| `1` | `NotStarted` | `referenceDate < plannedStart` |
| `2` | `InProgress` | `plannedStart ≤ referenceDate ≤ plannedEnd` |
| `3` | `Delayed` | `referenceDate > plannedEnd`, praca nieukończona |
| `4` | `Completed` | Ukończone w terminie |
| `5` | `CompletedLate` | Ukończone po `plannedEnd` |

> **Priorytet agregacji:** `Delayed (3) › CompletedLate (5) › InProgress (2) › NotStarted (1) › Completed (4) › NoSchedule (0)`

---

## CostTrackerController

Route prefix: `.../cost-trackers`

---

### `POST .../cost-trackers/costs`

Tworzy nowy koszt rzeczywisty (`TrackedCost`) w projekcie.

**Command:** `CreateTrackedCostCommand` — `multipart/form-data`

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `workItemLinkId` | `Guid?` | nie | Powiązanie z pozycją kosztorysu/harmonogramu. `null` = koszt niespięty |
| `name` | `string` | **tak** | Nazwa kosztu |
| `description` | `string?` | nie | |
| `net` | `decimal?` | nie | Kwota netto |
| `gross` | `decimal?` | nie | Kwota brutto |
| `contractor` | `string?` | nie | Wykonawca |
| `date` | `DateTime?` | nie | Data kosztu |
| `newFiles` | `IFormFile[]?` | nie | Załączniki (max 50 MB łącznie) |

**Odpowiedź: `200 OK` → `TrackedCostWeb`**

| Kod | Opis |
|-----|------|
| `200` | Koszt utworzony |
| `403` | Brak uprawnienia `ProjectResourcesWrite` |
| `404` | Projekt nie istnieje |

---

### `PUT .../cost-trackers/costs/{costId}`

Pełne nadpisanie istniejącego kosztu rzeczywistego.

**Command:** `UpdateTrackedCostCommand` — `multipart/form-data`

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `name` | `string` | **tak** | |
| `description` | `string?` | nie | |
| `net` | `decimal?` | nie | |
| `gross` | `decimal?` | nie | |
| `vatRate` | `decimal?` | nie | |
| `contractor` | `string?` | nie | |
| `date` | `DateTime?` | nie | |
| `newFiles` | `IFormFile[]?` | nie | Nowe załączniki (max 50 MB łącznie) |
| `existingAttachmentIds` | `Guid[]?` | nie | Id załączników do zachowania — pozostałe zostaną usunięte |

**Odpowiedź: `200 OK` → `TrackedCostWeb`**

| Kod | Opis |
|-----|------|
| `200` | Zaktualizowano |
| `403` | Brak uprawnienia `ProjectResourcesWrite` |
| `404` | Koszt lub projekt nie istnieje |

---

### `PUT .../cost-trackers/{costTrackerId}/budget`

Aktualizuje pola budżetowe projektu (`BudgetNet`, `BudgetGross`). Obciążają rezerwę projektu — pomniejszają pulę dostępną na koszty niespiete.

**Command:** `UpdateTrackerBudgetCommand` — `application/json`

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `budgetNet` | `decimal?` | nie | Rezerwa netto |
| `budgetGross` | `decimal?` | nie | Rezerwa brutto |

**Odpowiedź: `204 No Content`**

| Kod | Opis |
|-----|------|
| `204` | Zaktualizowano |
| `400` | Błąd walidacji |
| `403` | Brak uprawnienia `ProjectEdit` |
| `404` | Projekt nie istnieje |

---

### `DELETE .../cost-trackers/costs/{costId}`

Soft-delete kosztu rzeczywistego. Usuwa powiązane załączniki z Blob Storage.

**Command:** `DeleteTrackedCostCommand`

| Pole | Typ | Źródło |
|------|-----|--------|
| `costId` | `Guid` | route |

**Odpowiedź: `204 No Content`**

| Kod | Opis |
|-----|------|
| `204` | Usunięto |
| `403` | Brak uprawnienia `ProjectResourcesWrite` |
| `404` | Koszt nie istnieje |
