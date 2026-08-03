# cost-estimate-export-ui-fix-02 — Toolbar + EditPage + a11y

## Kontekst

- Feature: `.opencode/features/cost-estimate-export.md`
- Audyt: `.opencode/subagents/rules/cost-estimate-export-ui-audit.md`
- Wymaga: `cost-estimate-export-ui-fix-01`
- Skills: `.opencode/skills/ui-components/SKILL.md`, `ui-hooks/SKILL.md`, `ui-accessibility/SKILL.md`

## Cel

Wpiąć eksport w menu „Akcje” i obsłużyć download na stronie edycji kosztorysu.

## Zadania

1. `CostEstimateToolbar.tsx` — rozszerz props:
   - `onExportXlsx: () => void`
   - `onExportPdf: () => void`
   - `isExportingXlsx: boolean`
   - `isExportingPdf: boolean`
   - W menu **Akcje** (po „Odśwież”, przed „Udostępnij”):
     - „Eksportuj do Excel” (ikonka `FileSpreadsheet` lub podobna z lucide)
     - „Eksportuj do PDF” (ikonka `FileText` / `Download`)
   - Loading: Spinner + `isDisabled` + tekst „Eksportuję…” jak przy sync/recalc
   - **Nie** owijać w `canEdit` / `canShare` — eksport widoczny zawsze w toolbarze (strona już wymaga dostępu)

2. `CostEstimateEditPage.tsx`:
   - Stan lub `useMutation` (`useExportCostEstimate` w `useCostEstimate.ts` — preferowane)
   - Handlery: wywołaj API → `downloadBlob` z fileName z response (fallback: sanitize `details.name` + data)
   - Toast **tylko przy błędzie** (`showApiError` / `showError`) — cichy sukces (spec default)
   - **Nie** blokuj eksportu przez `hasChanges` (eksport = stan serwera)
   - Przekaż props do `CostEstimateToolbar`

3. A11y:
   - Ikony w MenuItem: `aria-hidden="true"`
   - Czytelne etykiety PL
   - Opcjonalnie `CostEstimateToolbar.axe.test.tsx` — render z Chakra + `toHaveNoViolations`

4. Eksportuj named functions; bez inline styles; bez `any`.

## Poza zakresem

- Modal opcji eksportu
- Cost Tracker
- Toast sukcesu / confirm dirty state

## Kryteria done

- [ ] Oba MenuItem widoczne i wywołują download
- [ ] Loading blokuje podwójne kliknięcie
- [ ] Shared/ReadOnly użytkownik może eksportować (brak gate canEdit)
- [ ] AXE smoke (jeśli dodany test) przechodzi
