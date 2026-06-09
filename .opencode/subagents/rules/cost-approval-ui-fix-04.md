# UI Fix 04 — ExpenseCard + CostModal — usunięcie isAccepted, dodanie status badge

## Cel
Zaktualizować komponenty kart i modala kosztów:
1. `ExpenseCard.tsx` — usunąć checkbox `isAccepted`, dodać status badge
2. `CostModal.tsx` (dashboard) — usunąć checkbox `isAccepted` dla `type='project'`

Przeczytaj skill `.opencode/skills/ui/skill-ui-components.md`.

---

## Krok 1 — `ExpenseCard.tsx`

Plik: `src/components/ExpenseCard.tsx`

**Usuń:**
- Props `canToggleAccepted?: boolean`
- Props `onToggleAccepted?: (id: string, value: boolean) => void`
- Element `<Checkbox>` z etykietą "Zaakceptowane"/"Niezaakceptowane"

**Dodaj:**
- Badge wyświetlający `approvalStatus` (jak w fix-03, spójny wygląd):
  ```tsx
  const statusConfig: Record<CostApprovalStatus, { label: string; colorScheme: string }> = {
    Draft: { label: 'Roboczy', colorScheme: 'gray' },
    PendingApproval: { label: 'Do akceptacji', colorScheme: 'orange' },
    Approved: { label: 'Zaakceptowany', colorScheme: 'green' }
  };
  const { label, colorScheme } = statusConfig[cost.approvalStatus];
  // <Badge colorScheme={colorScheme}>{label}</Badge>
  ```

---

## Krok 2 — `CostModal.tsx` (dashboard)

Plik: `src/features/dashboard/components/CostModal.tsx`

Dla `type='project'` usuń:
- Pole formularza z checkbox `isAccepted`
- Wszelkie state/logikę związaną z `isAccepted` w formularzu

---

## Krok 3 — Sprawdź `CostSummaryBar` lub podobne

Jeśli gdzieś jest `CostSummaryBar` wyświetlający podział "Zaakceptowane/Niezaakceptowane" wg `isAccepted: bool`:
- Zaktualizuj na podział wg `approvalStatus` (Draft / PendingApproval / Approved)

---

## Weryfikacja końcowa — pełny build

```powershell
cd C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI
npx tsc --noEmit 2>&1 | Select-Object -Last 5
```

Oczekiwany wynik: `Exit: 0` — brak błędów TypeScript.
