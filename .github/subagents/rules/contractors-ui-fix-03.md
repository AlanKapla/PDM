# contractors-ui-fix-03 — ContractorPicker + integracja w CostForm i formularzach kosztów

## Cel
Stworzenie komponentu `ContractorPicker` (select kontrahenta z listy tenanta) i jego integracja
we wszystkich formularzach kosztów: `CostForm`, `CostFormDrawer`, `CostFormModal`.

## Skill
Przeczytaj `.github/skills/ui/skill-ui-components.md` i `.github/skills/ui/skill-ui-forms-modals.md` przed implementacją.

## Kontekst
- Raport audytu UI: `.github/subagents/rules/contractors-ui-audit.md`
- Typy istnieją po `contractors-ui-fix-01`
- `useContractors` hook istnieje po `contractors-ui-fix-01`
- `ContractorQuickAddModal` zostanie dodany w `contractors-ui-fix-04`

## Zmiany do wykonania

### 1. Nowy komponent: `src/components/ContractorPicker.tsx`

Props:
```typescript
interface ContractorPickerProps {
  tenantId: string;
  value: string | null;
  onChange: (id: string | null) => void;
  canQuickAdd?: boolean;       // czy pokazać przycisk + do szybkiego dodania
  isDisabled?: boolean;
  isInvalid?: boolean;
  placeholder?: string;
}
```

Implementacja:
- Użyj Chakra UI `Select` lub `Menu`/`Combobox` (sprawdź co jest używane w projekcie)
- Pobiera listę przez `useContractors(tenantId)` — isLoading spinner
- Opcja pusta: „— Brak kontrahenta —" (wartość null)
- Lista opcji: `{contractor.name}{contractor.taxId ? ` (NIP: ${contractor.taxId})` : ''}`
- Jeśli `canQuickAdd=true` i lista załadowana — na dole lub obok select renderuj `IconButton` z `AddIcon` z tooltipem „Dodaj nowego kontrahenta"
- Kliknięcie + → otwiera `ContractorQuickAddModal` (przekazywane jako prop `onQuickAdd` lub obsługiwane stanem wewnętrznym)
- Po quick-add: automatycznie selektuje nowo dodanego kontrahenta

Zachowania:
- Gdy `isDisabled=true` — `Select` jest disabled
- Gdy `isLoading` — placeholder „Ładowanie kontrahentów..."
- Po zmianie selekcji wywołuje `onChange(id)` lub `onChange(null)` dla opcji pustej

### 2. Modyfikacja `src/components/CostTracker/CostForm.tsx`

Dodać do `CostFormProps`:
```typescript
tenantId: string;
canQuickAdd?: boolean;
```

Zastąpić pole wykonawcy:
```tsx
// Usunąć:
<FormControl>
  <FormLabel>Wykonawca</FormLabel>
  <Input value={values.contractor ?? ""} onChange={(e) => set({ contractor: e.target.value })} maxLength={300} />
</FormControl>

// Dodać:
<FormControl isInvalid={!!errors.contractorId}>
  <FormLabel>Wykonawca</FormLabel>
  <ContractorPicker
    tenantId={tenantId}
    value={values.contractorId ?? null}
    onChange={(id) => set({ contractorId: id })}
    canQuickAdd={canQuickAdd}
    isDisabled={isSubmitting}
  />
</FormControl>
```

### 3. Modyfikacja `src/components/CostTracker/CostFormDrawer.tsx`

- Dodać `tenantId` i `canQuickAdd` do propów (odczytać z kontekstu projektu lub przekazać z parenta)
- EMPTY_FORM: zmienić `contractor: ""` → `contractorId: null`
- Init z istniejącego kosztu: `contractor: cost.contractor ?? ""` → `contractorId: cost.contractorId ?? null`
- Submit: zmienić pole `contractor: values.contractor || null` → `contractorId: values.contractorId || null`
- Przekazać `tenantId={tenantId}` i `canQuickAdd={canQuickAdd}` do `<CostForm />`

Logika `canQuickAdd`: sprawdź czy user jest PROJECT.ADMIN lub TENANT.ADMIN:
```typescript
// Wzorzec (dostosuj do istniejących hooków):
const { roleCode } = useProjectPermissions(projectId);
const { canEdit: isTenantAdmin } = useTenantPermissions();
const canQuickAdd = isTenantAdmin || roleCode === 'PROJECT.ADMIN';
```

### 4. Modyfikacja `src/components/CostTracker/CostFormModal.tsx`

- EMPTY_FORM: zmienić `contractor: ""` → `contractorId: null`
- Submit: zmienić `contractor: values.contractor` → `contractorId: values.contractorId`
- Przekazać `tenantId` do `<CostForm />` (powinna być dostępna w propsach lub kontekście)
- Dodać `canQuickAdd` analogicznie jak w CostFormDrawer

### 5. Aktualizacja wyświetlania nazwy kontrahenta w komponentach read-only

#### `src/components/CostTracker/CostListDrawer.tsx`
Zmienić: `cost.contractor` → `cost.contractorName`

#### `src/components/CostTracker/ProjectAdditionalCostsSection.tsx`
Zmienić: `cost.contractor ?? "—"` → `cost.contractorName ?? "—"`

#### `src/components/ExpenseCard.tsx`
Zmienić: `cost.contractor` → `cost.contractorName`

#### `src/pages/ProjectSimpleCosts.tsx`
- Linie wyświetlające: `cost.contractor` → `cost.contractorName` (3 miejsca)
- Init formularza edycji: `contractor: cost.contractor` → `contractorId: cost.contractorId`
- Stan formularza: `contractor: ""` → `contractorId: null`
- Submit formularza: `contractor: form.contractor` → `contractorId: form.contractorId`

## Wymagania jakościowe
- Brak `any` w TypeScript
- Brak inline styles
- `ContractorPicker` nie sprawdza roli wewnątrz — `canQuickAdd` przekazywane z parenta
- Gdy `ContractorQuickAddModal` jeszcze nie istnieje (przed fix-04) — pomiń prop `canQuickAdd` lub zaimplementuj jako `false`

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-Object -Last 20
npm run build 2>&1 | Select-Object -Last 10
```
Brak błędów TypeScript i build powinien przejść.
