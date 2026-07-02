# UI Fix 07 — Strony listy i szczegółów + retry z potwierdzeniem

## Cel
Główne widoki modułu: lista dokumentacji, szczegóły ze status-driven UI, przycisk „Ponów przetwarzanie” przy `Failed`.

## Decyzje MVP
- Retry = ponowne uruchomienie pipeline AI (`POST .../retry`) **bez** ponownego uploadu
- Przycisk „Ponów przetwarzanie” na widoku szczegółów gdy `Failed`
- **AlertDialog** (`DeleteAlertDialog`) z potwierdzeniem — kosztowne wywołania AI
- **Brak** wyświetlania `retryCount` / `AutoRetryCount`
- Pending/Processing: komunikat zamiast JSON
- Completed: `TechnicalDocumentationDetailsView`
- Failed: Alert error + retry + lista plików

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-components/SKILL.md`
- `.cursor/skills/ui-hooks/SKILL.md`

## Zależności
- **ui-fix-02** — hooki
- **ui-fix-04** — StatusBadge
- **ui-fix-05** — AddTechnicalDocumentationModal
- **ui-fix-06** — DetailsView, FileList, ProcessingState

## Pliki referencyjne
- `src/pages/ProjectSchedules.tsx` — wzorzec strony modułu
- `src/pages/ProjectCosts.tsx` — statusy, klikalne wiersze
- `src/components/ui/DeleteAlertDialog.tsx` — potwierdzenie retry

---

## 1. `ProjectTechnicalDocumentationPage`

Plik: `src/pages/ProjectTechnicalDocumentationPage.tsx`

### Layout
- `MainLayout`, `BackToProjectButton`, `LoadingSpinner`, `EmptyState`
- Nagłówek: „Dokumentacja techniczna”
- Przycisk „Dodaj dokumentację” gdy `canWriteTechnicalDocumentation` → otwiera `AddTechnicalDocumentationModal`

### Lista
- Tabela: nazwa, opis (truncate), status (`TechnicalDocumentationStatusBadge`), liczba plików, data utworzenia
- Wiersz: `cursor="pointer"`, `_hover`, `onClick` → navigate `/projects/${projectId}/technical-documentation/${id}`
- `useTechnicalDocumentationList` + `useProjectPermissions`

### Uprawnienia
- Brak `canViewTechnicalDocumentation` → redirect lub komunikat braku dostępu (sprawdź wzorzec innych modułów)

## 2. `ProjectTechnicalDocumentationDetailsPage`

Plik: `src/pages/ProjectTechnicalDocumentationDetailsPage.tsx`

### Routing param
- `docId` z `useParams`

### Nagłówek
- Nazwa, opis, `TechnicalDocumentationStatusBadge`
- Daty: utworzono, ukończono (jeśli present)

### Treść (switch status)
```
Pending | Processing:
  → TechnicalDocumentationProcessingState
  → TechnicalDocumentationFileList

Completed:
  → TechnicalDocumentationDetailsView (jeśli details)
  → TechnicalDocumentationFileList

Failed:
  → Alert status="error" + errorMessage
  → Przycisk „Ponów przetwarzanie” (tylko canWriteTechnicalDocumentation)
  → TechnicalDocumentationFileList
```

### Retry flow
1. Klik „Ponów przetwarzanie” → otwórz `DeleteAlertDialog` (lub dedykowany `ConfirmDialog`)
2. Tytuł: „Ponowić przetwarzanie?”
3. Opis: ostrzeżenie o koszcie AI / czasie przetwarzania
4. Confirm → `useRetryTechnicalDocumentation.mutateAsync(docId)`
5. Sukces: toast info „Przetwarzanie zostało ponownie uruchomione”
6. Przycisk retry: `aria-describedby` wskazujący na `errorMessage` gdy present

**Nie** pokazuj licznika auto-retry.

## Weryfikacja
```powershell
npx tsc --noEmit
```

## Następny krok
Routing + kafelek + testy w **ui-fix-08**.
