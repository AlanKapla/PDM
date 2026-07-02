# UI Fix 06 — `TechnicalDocumentationDetailsView` + `TechnicalDocumentationFileList`

## Cel
Czytelny widok wyników ekstrakcji (Accordion) i lista plików źródłowych z podglądem.

## Decyzje MVP
- Widok JSON: **tylko Accordion** — **bez** surowego `<pre>` / trybu debug JSON
- **Brak** wyświetlania `retryCount`
- Preview plików: `window.open(sasUriPreview)` — wzorzec `ProjectFiles`

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-components/SKILL.md`
- `.cursor/skills/ui-accessibility/SKILL.md`

## Zależności
- **ui-fix-01** — typy Details
- **ui-fix-04** — opcjonalnie StatusBadge w file list header

## Pliki referencyjne
- `src/pages/ProjectFiles.tsx` — `isPreviewSupported`, `window.open`
- Chakra `Accordion`, `AccordionItem`, `AccordionButton`, `AccordionPanel`

---

## 1. `TechnicalDocumentationFileList`

Plik: `src/components/technicalDocumentation/TechnicalDocumentationFileList.tsx`

### Props
```typescript
export interface TechnicalDocumentationFileListProps {
  files: TechnicalDocumentationFileWeb[];
}
```

### UI
- Tabela lub lista: nazwa pliku, rozmiar (formatowany), typ
- Przycisk „Podgląd” gdy `sasUriPreview` + `isPreviewSupported(contentType)`
- Przycisk „Pobierz” gdy `sasUriDownload`
- Ikony lucide: `aria-hidden="true"` obok tekstu
- `IconButton` z `aria-label`

## 2. `TechnicalDocumentationDetailsView`

Plik: `src/components/technicalDocumentation/TechnicalDocumentationDetailsView.tsx`

### Props
```typescript
export interface TechnicalDocumentationDetailsViewProps {
  details: ProjectTechnicalDocumentationDetailsWeb;
}
```

### Sekcje Accordion (kolejność)
1. **Informacje o projekcie** — ProjectInfo (nazwa, adres, autor, klient, data)
2. **Rysunki** — nested Accordion per drawing:
   - Nagłówek: `{drawingType}` + skala + źródło (plik, strona)
   - Pomieszczenia: nazwa, wymiary (szer/dł/wys/pow), ściany, otwory
3. **Dach** — RoofDetails (jeśli present)
4. **Instalacje** — lista InstallationInfo (typ, obecność, notatki)
5. **Zestawienie materiałów** — tabela MaterialSummary
6. **Powierzchnia całkowita** — `TotalAreaM2` m²

### Formatowanie
- Liczby: 2 miejsca po przecinku gdzie sensowne
- Puste sekcje: nie renderuj itemu Accordion
- Tekst: `neutral.700`, nagłówki semibold

### Zakaz
- `JSON.stringify` jako główny widok
- Collapsible raw JSON

## 3. Komponent stanu przetwarzania (opcjonalnie tutaj lub w fix-07)

`TechnicalDocumentationProcessingState.tsx`:
- Alert info + Spinner
- Tekst: „Trwa przetwarzanie dokumentacji…”
- `role="status"`, `aria-live="polite"`

## Weryfikacja
```powershell
npx tsc --noEmit
```

## Następny krok
Strony listy i szczegółów w **ui-fix-07**.
