# UI Fix 03 — Strona ProjectSimpleCosts — nowe zakładki + akcje akceptacji

## Cel
Zaktualizować stronę `ProjectSimpleCosts.tsx`:
1. Usunąć zakładkę "Udostępnione" (scope Shared)
2. Dodać zakładkę "Do akceptacji" (scope PendingApproval, widoczna tylko dla adminów)
3. Dodać akcje "Skieruj do akceptacji" / "Wycofaj" dla właściciela
4. Dodać akcje "Akceptuj" / "Odrzuć" w zakładce "Do akceptacji" dla adminów

Przeczytaj skill `.github/skills/ui/skill-ui-components.md`.

---

## Krok 1 — Zaktualizuj strukturę zakładek

Plik: `src/pages/ProjectSimpleCosts.tsx`

Usuń zakładkę z `ResourceScope.Shared`.

Dodaj zakładkę z `ResourceScope.PendingApproval` widoczną tylko gdy `isAdmin`:
```tsx
{isAdmin && (
  <Tab>Do akceptacji</Tab>
)}
```

---

## Krok 2 — Zaktualizuj logikę pobierania danych

Zakładka "Do akceptacji" używa:
```ts
getProjectCosts(tenantId, projectId, ResourceScope.PendingApproval)
```

---

## Krok 3 — Dodaj badge statusu

W liście kosztów (zakładka "Wszystkie" i "Moje") wyświetlaj badge `approvalStatus`:
- `Draft` → szary badge "Roboczy"
- `PendingApproval` → żółty/pomarańczowy badge "Do akceptacji"
- `Approved` → zielony badge "Zaakceptowany"

Użyj Chakra UI `<Badge>` z odpowiednim `colorScheme`.

---

## Krok 4 — Przyciski akcji w zakładce "Moje"

Dla każdego kosztu właściciela:
- Jeśli `approvalStatus === 'Draft'`: przycisk "Skieruj do akceptacji" → `submitProjectCostForApproval()`
- Jeśli `approvalStatus === 'PendingApproval'`: przycisk "Wycofaj" → `withdrawProjectCostFromApproval()`
- Jeśli `approvalStatus === 'Approved'`: brak przycisku zmiany statusu

---

## Krok 5 — Przyciski akcji w zakładce "Do akceptacji"

Dla każdego kosztu (widok admina):
- Przycisk "Akceptuj" → `approveProjectCost()` — `colorScheme="green"`
- Przycisk "Odrzuć" → `rejectProjectCost()` — `colorScheme="red"`

Po akcji: invaliduj cache React Query dla kosztów projektu.

---

## Krok 6 — Usuń logikę `canToggleAccepted` / `onToggleAccepted`

Usuń wszelkie props i wywołania `canToggleAccepted` i `onToggleAccepted` przekazywane do `<ExpenseCard>` lub tabel.

---

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-String "ProjectSimpleCosts|error TS" | Select-Object -First 20
```
