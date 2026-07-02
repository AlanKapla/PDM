# UI Fix 04 — `MultiDocumentDropzone` + `TechnicalDocumentationStatusBadge`

## Cel
Reużywalne komponenty UI: multi-file upload (PDF/JPG, 50 MB) i badge statusów async.

## Decyzje MVP
- Max **50 MB**/plik, typy: PDF + JPEG
- Statusy: Pending (żółty), Processing (niebieski + Spinner), Completed (zielony), Failed (czerwony)
- **Nie** modyfikuj istniejącego `DocumentDropzone` (single-file, 20 MB — używany przez AICostImport)

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-components/SKILL.md`
- `.cursor/skills/ui-accessibility/SKILL.md`

## Zależności
- **ui-fix-01** — `TechnicalDocumentationStatus`

## Pliki referencyjne
- `src/components/ui/DocumentDropzone.tsx` — label/input pattern, aria
- `src/components/CostEstimate/FileFieldRenderer.tsx` — walidacja 50 MB + MIME
- `src/components/ui/StatusBadge.tsx`

---

## 1. `MultiDocumentDropzone`

Plik: `src/components/ui/MultiDocumentDropzone.tsx`

### Props
```typescript
export interface MultiDocumentDropzoneProps {
  files: File[];
  onFilesChange: (files: File[]) => void;
  accept?: string;          // default ".pdf,.jpg,.jpeg"
  maxSizeBytes?: number;    // default 52_428_800
  isDisabled?: boolean;
  errorMessage?: string;
}
```

### Zachowanie
- Drag & drop wielu plików + `<input type="file" multiple>`
- Lista wybranych plików z nazwą, rozmiarem, przyciskiem usuń (IconButton)
- Walidacja per plik: rozmiar, MIME (`application/pdf`, `image/jpeg`)
- Błędy walidacji: `role="alert"` lub tekst pod strefą
- ARIA:
  - `aria-label="Wybierz pliki PDF lub JPG"` na input
  - `aria-label` na strefie drop
  - Placeholder tekst: kontrast ≥ 4.5:1 (użyj `neutral.600`, nie `gray.400` dla treści)

### Wzorzec
`<label htmlFor={inputId}>` + ukryty input — klawiatura OK.

## 2. `TechnicalDocumentationStatusBadge`

Plik: `src/components/technicalDocumentation/TechnicalDocumentationStatusBadge.tsx`

### Props
```typescript
export interface TechnicalDocumentationStatusBadgeProps {
  status: TechnicalDocumentationStatus;
  showSpinner?: boolean; // default true dla Processing
}
```

### Mapowanie
| Status | Etykieta PL | Kolory |
|--------|-------------|--------|
| Pending | Oczekuje | yellow.800 / yellow.100 |
| Processing | Przetwarzanie | blue.800 / blue.100 + Spinner |
| Completed | Ukończono | green.800 / green.100 |
| Failed | Błąd | red.800 / red.100 |

Processing: kontener ze `role="status"` + `aria-live="polite"`, Spinner z `aria-hidden="true"`.

## Weryfikacja
```powershell
npx tsc --noEmit
npm run test:run -- src/components/ui/__tests__/MultiDocumentDropzone.axe.test.tsx
npm run test:run -- src/components/technicalDocumentation/__tests__/TechnicalDocumentationStatusBadge.axe.test.tsx
```
(Testy AXE — pełna implementacja w **ui-fix-08** jeśli nie teraz)

## Następny krok
Modal upload w **ui-fix-05**.
