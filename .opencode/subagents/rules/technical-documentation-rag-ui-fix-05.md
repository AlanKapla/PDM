# UI Fix 05 — `AddTechnicalDocumentationModal`

## Cel
Modal dodawania dokumentacji: nazwa, opis, multi-file upload → POST 202 → zamknięcie + invalidate listy.

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-forms-modals/SKILL.md`
- `.cursor/skills/ui-components/SKILL.md`

## Zależności
- **ui-fix-02** — `useCreateTechnicalDocumentation`
- **ui-fix-04** — `MultiDocumentDropzone`

## Pliki referencyjne
- `src/components/CostTracker/AICostImportModal.tsx` — AppModal + mutation + toast błędów
- `src/components/ui/AppModal.tsx`

---

## 1. `AddTechnicalDocumentationModal`

Plik: `src/components/technicalDocumentation/AddTechnicalDocumentationModal.tsx`

### Props
```typescript
export interface AddTechnicalDocumentationModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
}
```

### Formularz
| Pole | Komponent | Walidacja |
|------|-----------|-----------|
| Nazwa | `FormControl` + `Input` | required |
| Opis | `Textarea` | optional |
| Pliki | `MultiDocumentDropzone` | min 1 plik |

### Submit
- `useCreateTechnicalDocumentation({ tenantId, projectId })`
- `mutateAsync({ name, description, files })`
- Sukces: `onClose()`, toast sukcesu „Dokumentacja została dodana i oczekuje na przetwarzanie”
- Błąd: `showApiError` / `handleApiError`
- Przycisk submit: `isLoading={isPending}`, disabled gdy brak nazwy lub plików

### UX
- `AppModal` z tytułem „Dodaj dokumentację techniczną”
- Footer: Anuluj + „Dodaj”
- Po sukcesie lista odświeży się przez invalidate + polling/hub

## Weryfikacja
```powershell
npx tsc --noEmit
```

## Następny krok
Widok szczegółów JSON w **ui-fix-06**, strony w **ui-fix-07**.
