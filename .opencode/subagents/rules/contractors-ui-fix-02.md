# contractors-ui-fix-02 — Strona ContractorsPage + nawigacja w Sidebar + routing

## Cel
Stworzenie strony zarządzania kontrahentami (`ContractorsPage`) dostępnej pod `/contractors`,
widocznej tylko dla admina tenanta. Funkcje: lista z filtrowaniem, dodawanie, edycja, usuwanie.

## Skill
Przeczytaj `.opencode/skills/ui/skill-ui-components.md` i `.opencode/skills/ui/skill-ui-forms-modals.md` przed implementacją.

## Kontekst
- Raport audytu UI: `.opencode/subagents/rules/contractors-ui-audit.md`
- Hooki istnieją po `contractors-ui-fix-01`
- Wzorzec strony tenant-level: `src/pages/TenantDetails.tsx` lub `src/pages/CostEstimateTemplates.tsx`
- Wzorzec modala CRUD: `AppModal` z `src/components/ui/AppModal.tsx`
- Wzorzec usuwania: `DeleteAlertDialog` z `src/components/ui/`
- Wzorzec sprawdzania uprawnień: `useTenantPermissions().canEdit`

## Zmiany do wykonania

### 1. Nowa strona: `src/pages/ContractorsPage.tsx`

Strona zawiera:
- Nagłówek z tytułem „Kontrahenci" i przyciskiem „Dodaj kontrahenta" (widoczny tylko gdy `canEdit`)
- Pole wyszukiwania (`Input` z ikoną) do filtrowania listy po `search` param (debounce 300ms)
- Tabela lub lista kontrahentów z kolumnami: Nazwa, NIP, Email, Telefon, Miasto, Akcje (Edytuj, Usuń)
- Paginacja lub virtualizacja jeśli lista jest duża (opcjonalne)
- Stan ładowania (`isLoading`) i pusty stan (brak kontrahentów)
- Modal dodawania/edycji (AppModal z formularzem)
- DeleteAlertDialog do potwierdzenia usunięcia

Formularz kontrahenta w modalu (pola):
- `name` — wymagane
- `taxId` — NIP (opcjonalne)
- `email` — opcjonalne
- `phoneNumber` — telefon (opcjonalne)
- `street` — ulica (opcjonalne)
- `city` — miasto (opcjonalne)
- `postalCode` — kod pocztowy (opcjonalne)
- `country` — kraj (opcjonalne)
- `notes` — notatki (opcjonalne, Textarea)

Hooki:
- `useContractors(tenantId, search)` — lista z filtrowaniem
- `useCreateContractor(tenantId)` — tworzenie
- `useUpdateContractor(tenantId)` — edycja
- `useDeleteContractor(tenantId)` — usuwanie
- `useTenantPermissions()` lub odpowiednik — sprawdzenie `canEdit`
- `useActiveTenant()` lub `useTenantContext()` — pobranie `tenantId`

Zachowania:
- Po zapisaniu formularza: zamknąć modal, lista odświeży się automatycznie (invalidateQueries)
- Usunięcie: najpierw `DeleteAlertDialog`, po potwierdzeniu → `deleteContractor`
- Jeśli `!canEdit` → ukryć przyciski Dodaj/Edytuj/Usuń (read-only view)
- Kolory i tokeny zgodnie z `appColors` z `src/theme/tokens/colors.ts`
- Zakaz inline styles

### 2. Modyfikacja `src/routes/AppRouter.tsx`

Dodać nową trasę w odpowiednim miejscu (obok innych tras tenant-level):
```tsx
<Route
  path="/contractors"
  element={
    <ProtectedRoute>
      <ContractorsPage />
    </ProtectedRoute>
  }
/>
```

Sprawdź jak są definiowane inne chronione trasy w AppRouter i użyj tego samego wzorca.

### 3. Modyfikacja `src/components/Sidebar.tsx` (lub `SidebarContent.tsx`)

Dodać nowy wpis nawigacyjny „Kontrahenci":
- Ikona: `Users` z `lucide-react` (lub dopasuj do ikon używanych w sidebarze)
- Ścieżka: `/contractors`
- Widoczność warunkowa: tylko gdy użytkownik ma `canEdit` dla tenanta (lub jest adminem tenanta)
- Umieścić po wpisie „Zarządzanie" lub w sekcji administracyjnej

Sprawdź jaki wzorzec jest używany do warunkowego renderowania wpisów w sidebarze (np. `{isTenantAdmin && <NavItem ... />}`).

## Wymagania jakościowe
- Brak `any` w TypeScript
- Logika w hookach (`useContractors`, `useCreateContractor` etc.), komponent tylko renderuje
- `AppModal` zamiast własnej implementacji modala
- `DeleteAlertDialog` do potwierdzenia usunięcia
- Kolory przez Chakra tokens lub `appColors`
- Responsive layout (`useBreakpointValue` jeśli potrzebne)
- `isLoading` spinner na liście podczas ładowania
- Błędy API — pokazać przez `useToastNotification` (wzorzec z innych komponentów)

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-Object -Last 20
npm run build 2>&1 | Select-Object -Last 10
```
Brak błędów TypeScript i build powinien przejść.
