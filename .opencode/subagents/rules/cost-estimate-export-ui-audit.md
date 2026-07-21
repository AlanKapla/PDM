# Audyt UI — Eksport kosztorysu (Cost Estimate) do PDF i XLSX

**Data audytu:** 2026-07-21  
**Audytor:** UI Audit Agent + Feature Planner  
**Feature spec:** `.opencode/features/cost-estimate-export.md`  
**Zakres:** `CostEstimateToolbar`, `CostEstimateEditPage`, `costEstimateApi`, hooki RQ, mock demo, wzorce blob download, a11y

---

## BLOK 1 — Stan obecny UI

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|------------------|------------|------|---------------------|
| `CostEstimateToolbar` | `src/components/CostEstimateToolbar.tsx` | Menu **„Akcje”** + **„Widok”** + search | **Główne miejsce UI** — dodać Excel/PDF |
| `CostEstimateEditPage` | `src/pages/CostEstimateEditPage.tsx` | Wiring toolbar; accessLevel; toast; loading sync/recalc | Handlery download |
| `costEstimateApi` | `src/api/costEstimateApi.ts` | CRUD + schemat + upload plików | Brak export |
| `useCostEstimateDetails` | `src/hooks/queries/useCostEstimate.ts` | RQ details + mutacje edycji | Brak mutation eksportu |
| `useToastNotification` | `src/hooks/useToastNotification.ts` | showError / showApiError | Błędy eksportu |
| `FileFieldRenderer` / `AttachmentList` / `ProjectFiles` | różne | Download przez SAS lub lokalny File | **Nie** axios+JWT blob |
| Mock CE | `src/api/mock/mockHandlers.ts` + `index.ts` | GET list/details; adapter JSON | Brak mocków blob |
| `CostEstimateAccessLevel` | `costEstimate.types.new.ts` | None / ReadOnly / Restricted / Full | Eksport gdy `!= None` |

**Wniosek:** Wejście UX gotowe. Brak łańcucha: blob API → helper download → handler → MenuItem → mock.

---

## BLOK 2 — Luki (priorytet)

| Brak | Priorytet | Opis |
|------|-----------|------|
| MenuItem Excel/PDF | **HIGH** | Spec: jeden klik, bez modalu |
| Props toolbara | **HIGH** | `onExport*`, `isExporting*` (wzorzec `isSyncing`) |
| Wiring EditPage | **HIGH** | Loading + toast |
| `exportPdf` / `exportXlsx` | **HIGH** | GET + `responseType: 'blob'` |
| Helper `downloadBlob` + CD parse | **HIGH** | W UI **zero** axios blob |
| Blob error unwrap | **HIGH** | `handleApiError` zakłada JSON |
| Uprawnienia ≠ canShare/canEdit | **HIGH** | Shared/ReadOnly też eksportują |
| Mock demo blob | **MEDIUM** | Adapter JSON-only dziś |
| Hook `useExportCostEstimate` | **MEDIUM** | Opcjonalnie useMutation |
| AXE test toolbara | **MEDIUM** | Brak istniejących axe dla toolbar |
| Timeout dużych CE | **MEDIUM** | Rozważyć dłuższy timeout na export |

---

## BLOK 3 — Typy

| Typ | Nowy/Modyfikacja | Opis |
|-----|------------------|------|
| `CostEstimateExportFormat` | Nowy | `'pdf' \| 'xlsx'` |
| `CostEstimateExportFile` | Nowy | `{ blob, fileName, contentType }` |
| `CostEstimateToolbarProps` | Modyfikacja | callbacki + flagi loading |
| `CostEstimateDetailsWeb` | Bez zmian | name / accessLevel wystarczą |

---

## BLOK 4 — API client

| Funkcja | Endpoint |
|---------|----------|
| `exportXlsx(tenantId, projectId, id)` | `GET .../cost-estimate/{id}/export/xlsx` |
| `exportPdf(...)` | `GET .../cost-estimate/{id}/export/pdf` |

Helpery: `src/utils/downloadBlob.ts` — `downloadBlob(blob, fileName)`, `parseContentDispositionFileName(header)`.

---

## BLOK 5 — Hooki

Preferowane: `useExportCostEstimate` (`useMutation`) — bez invalidate cache.  
Alternatywa: lokalny `useState` na page jak `isSyncing`.

---

## BLOK 6–7 — Komponenty

Brak nowych ekranów. Kolejność w menu Akcje (rekomendacja):

1. Harmonogram (istniejące)  
2. Odśwież  
3. **Eksportuj do Excel**  
4. **Eksportuj do PDF**  
5. Udostępnij (jeśli canShare)  
6. Pola dodatkowe (jeśli canEdit)

Widoczność eksportu: zawsze gdy użytkownik jest na stronie edycji (już przeszedł gate dostępu); nie uzależniać od `canEdit` / `canShare`.

---

## BLOK 8 — A11y

- Ikony Lucide: `aria-hidden="true"`
- MenuItem: czytelny tekst PL
- Loading: Spinner + `isDisabled` + opcjonalnie `aria-busy`
- Nowy `CostEstimateToolbar.axe.test.tsx` (smoke)

---

## BLOK 9 — Ryzyka

1. Pierwszy axios blob w projekcie  
2. Błędy 403/404 jako Blob — trzeba `blob.text()` + JSON parse  
3. Mock adapter zawsze JSON  
4. Błędne gating `canShare`  
5. `hasChanges` lokalne vs stan serwera (eksport = stan **serwera**)  
6. Timeout  

---

## Decyzje UI v1 (defaulty Feature Planner — pytania z audytu zamknięte)

| Pytanie | Default v1 |
|---------|------------|
| Confirm przy `hasChanges`? | **Nie** — eksportuje stan serwera; opcjonalnie później |
| Toast sukcesu? | **Nie** — cichy download; toast tylko przy błędzie |
| Toast „Generuję…”? | Loading w MenuItem wystarczy; bez osobnego toastu |
| Demo mock? | Minimalny niepusty Blob + `Content-Disposition` (nawet 1-byte/placeholder) |

---

## Pliki UI do zmiany

```
src/components/CostEstimateToolbar.tsx
src/pages/CostEstimateEditPage.tsx
src/api/costEstimateApi.ts
src/utils/downloadBlob.ts                          (new)
src/hooks/queries/useCostEstimate.ts               (opcjonalnie mutation)
src/types/costEstimate.types.new.ts                (typy export)
src/api/mock/mockHandlers.ts
src/api/mock/index.ts                              (jeśli adapter JSON-only)
src/components/CostEstimateToolbar.axe.test.tsx    (new, zalecane)
```

## Pytania domenowe — ZAMKNIĘTE

Zamknięte defaultami powyżej + spec feature.
