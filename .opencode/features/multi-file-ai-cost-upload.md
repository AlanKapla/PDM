# Feature: Masowe dodawanie kosztów/wydatków z wykorzystaniem AI (multi-file upload)

## Typ zmiany

**Full-stack** — API (encje, CQRS, kolejka, worker, cleanup job) + UI (multi-upload, zakładka weryfikacji, nawigacja z powiadomień)

## Cel

Rozszerzenie istniejącego single-file AI importu o upload wielu plików (łącznie ≤ 50 MB). **1 plik** → bez zmian (synchroniczny `ParseCostDocumentQuery`). **2+ pliki** → batch w tle z powiadomieniem SignalR i nową zakładką akceptacji, gdzie koszty **nie trafiają do bazy** do momentu accept przez użytkownika.

## Powiązane feature specs

- [ai-cost-document-import.md](./ai-cost-document-import.md) — obecna implementacja single-file

---

## Stan obecny (odkryte w kodzie)

| Obszar | Kluczowe pliki |
|--------|----------------|
| Sync AI parse | `AICostController.cs`, `ParseCostDocumentQueryHandler.cs`, `DocumentParserService.cs` |
| UI single-file | `AICostImportModal.tsx`, `DocumentDropzone.tsx` (1 plik, max 20 MB), `useAICostDocumentParser.ts` |
| Zapis kosztu | `CreateProjectCostCommand`, `CreateTrackedCostCommand` — dopiero po user accept w formularzu |
| Powiadomienia | `QueuedNotificationSender` → Azure Queue → `NotificationWorker` → SignalR `/api/hubs/notifications` |
| Metadata powiadomień | `Notification.MetadataJson` — pole istnieje, UI typ `metadata?: Record<string, unknown>` |
| Scheduled jobs | `FileShareConsolidationService` (daily 2:00 AM), workers jako `BackgroundService` |
| Kolejki | `IQueueStorageService` / `QueueStorageService` — używane przez notifications, messages, email |
| Brak | Encji pending AI costs, multi-file dropzone, hash duplikatów, dedykowanej kolejki AI |

### Obecny flow single-file (bez zmian)

```
User → AICostImportModal → DocumentDropzone (1 plik)
     → POST .../ai/cost/parse/{project-cost|tracked-cost}
     → ParseCostDocumentQueryHandler → ParsedCostDto
     → onParsed() → CostFormModal prefill → user edytuje → Create*CostCommand
```

---

## Architektura docelowa

```
┌─────────────────────────────────────────────────────────────────────────┐
│ UI: AICostImportModal                                                    │
│   files.length === 1  → istniejący sync flow (bez zmian)                │
│   files.length > 1    → POST .../ai/cost/import/batch → toast "w tle"   │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
         ParseCostDocument (sync)          SubmitAICostImportBatch
         (bez zmian)                       → blob storage + DB batch/items
                                           → enqueue ai-cost-import-process
                                                    │
                                                    ▼
                                           AICostImportWorker
                                           (parse, enrich, duplicate, retry)
                                                    │
                              ┌─────────────────────┼─────────────────────┐
                              ▼                     ▼                     ▼
                         Pending            ErrorNeedsReview      DuplicateRemoved
                              │                     │                (auto-delete)
                              └──────────┬──────────┘
                                         ▼
                              Batch complete → INotificationSender
                              (metadata.Route → review page)
                                         │
                                         ▼
                              /projects/:projectId/costs/ai-review
                              Accept → CreateProjectCost / CreateTrackedCost
```

---

## Model danych

### Encja: `AICostImportBatch`

Paczka plików przesłanych w jednej operacji.

| Pole | Typ | Opis |
|------|-----|------|
| Id | Guid | PK |
| TenantId | Guid | |
| ProjectId | Guid | |
| CreatedByUserId | Guid | |
| CostDocumentType | enum | ProjectCost / TrackedCost |
| TrackedCostContextJson | string? | `costEstimateItemId`, `workScheduleStageWorkId` — dziedziczone z miejsca otwarcia importu |
| Status | enum | Queued, Processing, Completed, Failed |
| TotalFiles | int | |
| ProcessedFiles | int | |
| PendingCount | int | |
| ErrorCount | int | |
| DuplicateCount | int | |
| CreatedAt | DateTimeOffset | |
| CompletedAt | DateTimeOffset? | |

### Encja: `AICostImportItem`

Pojedyncza pozycja oczekująca na akceptację.

| Pole | Typ | Opis |
|------|-----|------|
| Id | Guid | PK |
| BatchId | Guid | FK → AICostImportBatch |
| TenantId | Guid | |
| ProjectId | Guid | |
| Status | enum | patrz poniżej |
| OriginalFileName | string | |
| ContentType | string | |
| FileSizeBytes | long | |
| FileHashSha256 | string | hex, do detekcji duplikatów |
| BlobPath | string | ścieżka w blob storage |
| ParsedDataJson | string? | serializowany `ParsedCostDto` |
| RetryCount | int | |
| LastError | string? | |
| AnalyzedAt | DateTimeOffset? | start retencji 30 dni |
| AcceptedCostId | Guid? | ID utworzonego kosztu po accept |
| CreatedAt | DateTimeOffset | |
| UpdatedAt | DateTimeOffset | |

### Statusy pozycji (`AICostImportItemStatus`)

| Status | Persisted | Widoczny w review | Opis |
|--------|-----------|-------------------|------|
| `Queued` | tak | nie | Czeka w kolejce worker |
| `Processing` | tak | nie | Worker analizuje |
| `Pending` | tak | **tak** | Gotowe do akceptacji |
| `ErrorNeedsReview` | tak | **tak** | Wszystkie retry wyczerpane |
| `Accepted` | tak | nie | Koszt zapisany — archiwum/audit |
| `Rejected` | — | — | Hard delete — brak rekordu |
| `DuplicateRemoved` | — | — | Hard delete — brak rekordu |
| `ExpiredDeleted` | — | — | Hard delete po 30 dniach |

**Reguła retencji:** 30 dni od `AnalyzedAt` dla statusów `Pending` i `ErrorNeedsReview`.

### Migracja EF Core

```
dotnet ef migrations add AddAICostImportBatchAndItems --startup-project ../WebApi
```

Indeksy:
- `(TenantId, ProjectId, Status)` na `AICostImportItem`
- `(TenantId, ProjectId, FileHashSha256)` na `AICostImportItem` — duplicate lookup
- `(AnalyzedAt, Status)` na `AICostImportItem` — cleanup job

---

## Duplicate detection

### Strategia (dwupoziomowa)

1. **Primary — hash pliku (SHA-256):**
   - Porównaj z `FileHashSha256` innych `AICostImportItem` w tym samym projekcie (status `Pending`, `ErrorNeedsReview`, `Accepted`)
   - Porównaj z hashem zapisanym przy zaakceptowanych kosztach (nowe pole `SourceFileHash` na `BaseCost` lub osobna tabela `CostDocumentAttachment`)

2. **Secondary — pola biznesowe (gdy hash różny, np. skan tej samej faktury):**
   - `Net` + `Date` (dzień) + `ContractorId` lub `ContractorNip` + `Number` (numer faktury)
   - Porównaj z `ProjectCost` / `TrackedCost` w projekcie

**Decyzja:** duplikat gdy **hash się zgadza** LUB **wszystkie 4 pola secondary się zgadzają** (Net, Date, Contractor, Number).

**Akcja:** hard delete pozycji, inkrement `DuplicateCount` na batch, info w powiadomieniu.

### Uzasadnienie

Hash jest najszybszy i wykrywa identyczne pliki. Secondary chroni przed duplikatami logicznymi (ten sam dokument w innym formacie). Kombinacja OR daje dobre pokrycie przy minimalnej złożoności.

---

## Retry i konfiguracja

### Domyślne wartości (`appsettings.json`)

```json
"AICostImport": {
  "MaxRetryAttempts": 3,
  "InitialRetryDelaySeconds": 30,
  "RetryBackoffMultiplier": 2,
  "RetentionDays": 30,
  "MaxBatchTotalBytes": 52428800,
  "QueueName": "ai-cost-import-process",
  "WorkerPollIntervalSeconds": 5
}
```

### Harmonogram retry (exponential backoff)

| Próba | Opóźnienie |
|-------|------------|
| 1 | natychmiast |
| 2 | 30 s |
| 3 | 60 s |

Po wyczerpaniu → status `ErrorNeedsReview`, `LastError` zapisany.

### Co traktować jako failure

- Exception z `DocumentParserService`
- `confidence === 0` (parser zwraca fallback przy błędzie)
- Timeout Azure OpenAI
- Błąd zapisu blob

---

## API — nowe endpointy

**Istniejące endpointy parse — BEZ ZMIAN:**
- `POST .../ai/cost/parse/project-cost`
- `POST .../ai/cost/parse/tracked-cost`

### Nowe endpointy (`AICostController`)

| Method | Route | CQRS | Opis |
|--------|-------|------|------|
| POST | `.../ai/cost/import/batch` | `SubmitAICostImportBatchCommand` | Multi-file upload, walidacja 50 MB |
| GET | `.../ai/cost/import/pending` | `GetPendingAICostImportItemsQuery` | Lista pozycji Pending + ErrorNeedsReview |
| GET | `.../ai/cost/import/pending/{itemId}` | `GetAICostImportItemQuery` | Szczegóły + URL podglądu pliku |
| PUT | `.../ai/cost/import/pending/{itemId}` | `UpdateAICostImportItemCommand` | Edycja ParsedData przed accept |
| POST | `.../ai/cost/import/pending/{itemId}/accept` | `AcceptAICostImportItemCommand` | Zapis kosztu + status Accepted |
| POST | `.../ai/cost/import/pending/accept-all` | `AcceptAllAICostImportItemsCommand` | Bulk accept |
| DELETE | `.../ai/cost/import/pending/{itemId}` | `RejectAICostImportItemCommand` | Hard delete |
| GET | `.../ai/cost/import/pending/count` | `GetPendingAICostImportCountQuery` | Badge w UI |

### SubmitAICostImportBatchCommand

- Input: `IFormFileCollection files`, `CostDocumentType`, opcjonalny `TrackedCostContext`
- Walidacja: suma `file.Length` ≤ `MaxBatchTotalBytes` (50 MB)
- Jeśli przekroczony → `400 BadRequest` z komunikatem: aktualna waga vs limit
- Tworzy `AICostImportBatch` + N × `AICostImportItem` (Queued)
- Upload plików do blob storage
- Oblicza SHA-256 per plik
- Enqueue message `{ batchId, itemId }` per plik

### AcceptAICostImportItemCommand

- Mapuje `ParsedDataJson` → `CreateProjectCostCommand` lub `CreateTrackedCostCommand`
- Używa istniejących handlerów (nie duplikuje logiki zapisu)
- Ustawia `AcceptedCostId`, status `Accepted`
- Opcjonalnie: zapisuje `SourceFileHash` na koszcie

### Permission codes

- `CostDocumentType.ProjectCost` → `PermissionCodes.ProjectCosts`
- `CostDocumentType.TrackedCost` → `PermissionCodes.ProjectDashboardTracker`

---

## Background processing

### `AICostImportWorker` (BackgroundService)

Wzorzec: `NotificationWorker` + `MessageWorker`

```
loop:
  dequeue from ai-cost-import-process
  load AICostImportItem
  set Status = Processing
  try:
    read blob → ParseAsync → EnrichWithContractor → EnrichWithCategory
    check duplicate → if dup: hard delete, increment DuplicateCount
    else: set ParsedDataJson, Status = Pending, AnalyzedAt = now
  catch:
    if RetryCount < MaxRetryAttempts:
      schedule retry with backoff (re-enqueue with delay)
    else:
      Status = ErrorNeedsReview, LastError = ex.Message
  update batch counters
  if all items terminal → send notification
```

### `AICostImportRetentionCleanupService` (BackgroundService)

Wzorzec: `FileShareConsolidationService` (daily 2:00 AM)

```
daily:
  find items where Status in (Pending, ErrorNeedsReview)
    AND AnalyzedAt < now - RetentionDays
  for each:
    delete blob
    hard delete item
    log audit: itemId, batchId, tenantId, projectId, fileName
```

Idempotentny — ponowne uruchomienie nie powoduje błędów.

### Refaktoryzacja współdzielonej logiki

Wyciągnij z `ParseCostDocumentQueryHandler`:
- `EnrichWithContractorAsync`
- `EnrichWithCategoryAsync`

Do serwisu `IAICostDocumentEnrichmentService` — używany przez sync handler i worker.

---

## Powiadomienia

### Kanał

Istniejący: `INotificationSender` → `QueuedNotificationSender` → queue → `NotificationWorker` → SignalR.

### Nowy serwis: `IAICostImportNotificationService`

Wzorzec: `FileShareNotificationService`

**Trigger:** batch `Status = Completed` (wszystkie pliki przetworzone)

**Treść powiadomienia:**
```
Title: "Analiza dokumentów kosztowych zakończona"
Message: "Przeanalizowano {N} dokumentów. {P} oczekuje na akceptację, 
          {E} wymaga ręcznej weryfikacji, {D} pominięto jako duplikaty."
Type: Info (lub Warning jeśli ErrorCount > 0)
```

**MetadataJson:**
```json
{
  "route": "/tenants/{tenantId}/projects/{projectId}/costs/ai-review",
  "batchId": "{batchId}",
  "pendingCount": 5,
  "errorCount": 1,
  "duplicateCount": 2
}
```

### UI — rozszerzenie `NotificationBell`

- Parsuj `metadata.route` przy kliknięciu powiadomienia
- `useNavigate(metadata.route)` + `markAsRead`
- Toast przy SignalR — opcjonalnie przycisk "Przejdź do weryfikacji" jeśli `metadata.route` obecne

---

## UI — komponenty i routing

### Nowe / zmodyfikowane pliki

| Plik | Akcja |
|------|-------|
| `components/ui/MultiDocumentDropzone.tsx` | **Nowy** — multi drag&drop, walidacja 50 MB łącznie |
| `components/CostTracker/AICostImportModal.tsx` | **Modyfikacja** — routing 1 vs N plików |
| `hooks/useAICostImportBatch.ts` | **Nowy** — submit batch mutation |
| `hooks/usePendingAICostImports.ts` | **Nowy** — lista pending, accept, reject |
| `api/aiCostApi.ts` | **Rozszerzenie** — nowe endpointy |
| `types/ai.types.ts` | **Rozszerzenie** — typy batch/pending |
| `pages/AICostReviewPage.tsx` | **Nowy** — zakładka weryfikacji |
| `components/AICostReview/AICostReviewItem.tsx` | **Nowy** — side-by-side preview |
| `components/AICostReview/AICostReviewItemForm.tsx` | **Nowy** — edycja ParsedCostDto |
| `components/NotificationBell.tsx` | **Modyfikacja** — nawigacja z metadata |
| `routes/AppRouter.tsx` | **Modyfikacja** — nowa trasa |

### Route

```
/tenants/:tenantId/projects/:projectId/costs/ai-review
```

### MultiDocumentDropzone

- `multiple` na `<input type="file">`
- Drag&drop wielu plików
- Props: `files: File[]`, `onChange`, `maxTotalSizeMB: 50`
- Walidacja **przed** wysłaniem — jeśli suma > 50 MB:
  - `onChange` nie aktualizuje listy
  - callback `onSizeExceeded(currentBytes, limitBytes)` → toast z komunikatem
- Brak limitu liczby plików

### AICostImportModal — routing

```typescript
if (files.length === 1) {
  // ISTNIEJĄCY FLOW — parseDocument sync → onParsed
} else if (files.length > 1) {
  // NOWY FLOW — submitBatch → toast "Dokumenty są analizowane w tle"
  // → zamknij modal, user kontynuuje pracę
}
```

### AICostReviewPage

- Header z licznikiem pending + przycisk "Zaakceptuj wszystkie"
- Lista `AICostReviewItem`:
  - Lewa kolumna: podgląd pliku (img / iframe dla PDF jeśli wspierane)
  - Prawa kolumna: formularz edycji danych AI
  - Status badge: Pending / Błąd analizy
  - Akcje: Akceptuj, Odrzuć
- Odrzucenie → `DeleteAlertDialog`:
  > "Czy na pewno chcesz odrzucić tę pozycję? Tej operacji nie można cofnąć."
- Sekcja "Pominięte duplikaty" — info z ostatniego batcha (jeśli dostępne)
- Link/badge w `ProjectSimpleCosts` i dashboard toolbar

### Entry points (bez zmian miejsca przycisku)

- `CostFormModal` / `CostFormDrawer` — przycisk "Importuj z dokumentu"
- `DashboardAddCostToolbar`
- `ProjectSimpleCosts`

Przekazać `TrackedCostContext` do batch gdy import z dashboardu trackera.

---

## Mapowanie Gherkin → implementacja

| Scenario | Implementacja |
|----------|---------------|
| Upload pojedynczego pliku | `files.length === 1` w `AICostImportModal` → istniejący `parseDocument` |
| Upload wielu plików ≤ 50 MB | `SubmitAICostImportBatchCommand` + worker |
| Przekroczenie limitu | Walidacja w `MultiDocumentDropzone` + validator CQRS |
| Zakończenie przetwarzania | `AICostImportNotificationService` + metadata route |
| Akceptacja pojedyncza | `AcceptAICostImportItemCommand` |
| Akceptacja zbiorcza | `AcceptAllAICostImportItemsCommand` |
| Odrzucenie | `RejectAICostImportItemCommand` + `DeleteAlertDialog` |
| Retencja 30 dni | `AICostImportRetentionCleanupService` |
| Błąd z retry | Worker retry logic → `ErrorNeedsReview` |
| Duplikat | `IAICostDuplicateDetectionService` → hard delete |

---

## Plan faz wdrożenia

### Faza 1 — API fundament (MVP backend)
1. Encje + migracja
2. `SubmitAICostImportBatchCommand` + validator (50 MB)
3. Blob storage dla pending files
4. `AICostImportWorker` (parse bez duplicate/retry)
5. `GetPendingAICostImportItemsQuery`
6. `AcceptAICostImportItemCommand` / `RejectAICostImportItemCommand`
7. Rejestracja DI + hosted service

### Faza 2 — UI upload
1. `MultiDocumentDropzone`
2. Rozszerzenie `AICostImportModal` (routing 1 vs N)
3. `useAICostImportBatch` + `aiCostApi.ts`
4. Toast "analiza w tle"

### Faza 3 — Zakładka weryfikacji
1. `AICostReviewPage` + routing
2. Side-by-side preview + edycja
3. Accept / Reject / Accept All
4. `DeleteAlertDialog` przy odrzuceniu
5. Badge pending count w nawigacji

### Faza 4 — Powiadomienia + duplicate + retry + cleanup
1. `AICostImportNotificationService`
2. `NotificationBell` — nawigacja z metadata
3. `IAICostDuplicateDetectionService`
4. Retry z exponential backoff w worker
5. `AICostImportRetentionCleanupService`
6. `IAICostDocumentEnrichmentService` (refactor)

### Faza 5 — Testy
1. CQRS: batch submit, accept, reject, duplicate, retry — xUnit + Moq
2. Worker: integration test z mock parser
3. UI: Vitest — MultiDocumentDropzone walidacja, modal routing
4. AXE: AICostReviewPage accessibility

---

## Checklist plików do utworzenia/modyfikacji

### API — Entities
- [ ] `Entities/Models/AI/AICostImportBatch.cs`
- [ ] `Entities/Models/AI/AICostImportItem.cs`
- [ ] `Entities/Enums/AICostImportItemStatus.cs`
- [ ] `Entities/Enums/AICostImportBatchStatus.cs`
- [ ] Migracja EF Core

### API — CQRS
- [ ] `CQRS/AI/SubmitAICostImportBatch/`
- [ ] `CQRS/AI/GetPendingAICostImportItems/`
- [ ] `CQRS/AI/GetAICostImportItem/`
- [ ] `CQRS/AI/UpdateAICostImportItem/`
- [ ] `CQRS/AI/AcceptAICostImportItem/`
- [ ] `CQRS/AI/AcceptAllAICostImportItems/`
- [ ] `CQRS/AI/RejectAICostImportItem/`
- [ ] `CQRS/AI/GetPendingAICostImportCount/`

### API — Business
- [ ] `Business/Interfaces/Services/IAICostImportNotificationService.cs`
- [ ] `Business/Implementation/Services/AICostImportNotificationService.cs`
- [ ] `Business/Interfaces/Services/IAICostDuplicateDetectionService.cs`
- [ ] `Business/Implementation/Services/AICostDuplicateDetectionService.cs`
- [ ] `Business/Interfaces/Services/IAICostDocumentEnrichmentService.cs`
- [ ] `Business/Implementation/Services/AICostDocumentEnrichmentService.cs`
- [ ] `Business/Implementation/Services/AICostImportWorker.cs`
- [ ] `Business/Implementation/Services/AICostImportRetentionCleanupService.cs`
- [ ] `Business/Interfaces/Configuration/AICostImportOptions.cs`

### API — WebApi
- [ ] Rozszerzenie `AICostController.cs`
- [ ] Rejestracja w `ServiceCollectionExtensions.cs`

### UI
- [ ] `components/ui/MultiDocumentDropzone.tsx`
- [ ] Modyfikacja `AICostImportModal.tsx`
- [ ] `pages/AICostReviewPage.tsx`
- [ ] `components/AICostReview/*`
- [ ] `hooks/useAICostImportBatch.ts`
- [ ] `hooks/usePendingAICostImports.ts`
- [ ] Rozszerzenie `api/aiCostApi.ts`, `types/ai.types.ts`
- [ ] Modyfikacja `NotificationBell.tsx`, `AppRouter.tsx`

### Testy
- [ ] `tests/CQRS.Tests/AI/SubmitAICostImportBatchCommandHandlerTests.cs`
- [ ] `tests/CQRS.Tests/AI/AcceptAICostImportItemCommandHandlerTests.cs`
- [ ] `tests/Business.Tests/Services/AICostDuplicateDetectionServiceTests.cs`
- [ ] `src/components/ui/MultiDocumentDropzone.test.tsx`

---

## Ryzyka i mitigacje

| Ryzyko | Mitigacja |
|--------|-----------|
| Rate limits Azure OpenAI przy dużych batchach | Kolejka sekwencyjna per batch; konfigurowalny delay między plikami |
| TrackedCost wymaga kontekstu (`costEstimateItemId`) | Przekazać `TrackedCostContext` z UI w batch metadata |
| Parser zwraca fallback (confidence=0) zamiast exception | Worker traktuje confidence=0 jako failure → retry |
| Brak nawigacji z powiadomień dziś | Rozszerzyć `NotificationBell` o `metadata.route` |
| 50 MB limit UI vs 20 MB limit API na single parse | Single parse bez zmian (20 MB); batch endpoint z 50 MB `RequestSizeLimit` |
| Duplikat hash vs ten sam dokument w innym formacie | Secondary match po polach biznesowych |

---

## Pytania do zatwierdzenia

1. **TrackedCost context:** Batch ma dziedziczyć `costEstimateItemId` / `workScheduleStageWorkId` z miejsca otwarcia importu? **Rekomendacja: tak** — zapisać na `AICostImportBatch.TrackedCostContextJson`.

2. **PDF w multi-upload:** Obecny `AICostController` akceptuje tylko JPG/PNG (PDF usunięty z walidacji kontrolera). Czy multi-upload ma wspierać PDF? **Rekomendacja:** tak, jeśli `DocumentParserService` obsługuje PDF — ujednolicić dozwolone formaty.

3. **Pole `SourceFileHash` na `BaseCost`:** Dodać do encji bazowej dla trwałego duplicate detection po accept? **Rekomendacja: tak** — nullable `string? SourceFileHashSha256`.

---

## Następne kroki (workflow OpenCode)

1. ✅ Plan feature (ten dokument)
2. ⬜ Zatwierdzenie planu przez usera
3. ⬜ Audyt API (`api-audit-agent`) — analiza istniejącego kodu AI cost
4. ⬜ Audyt UI (`ui-audit-agent`) — analiza modal/dropzone
5. ⬜ Implementacja API (`api-refactor-agent`)
6. ⬜ Implementacja UI (`ui-refactor-agent`)
7. ⬜ Testy (`unit-test-orchestrator-agent`)
