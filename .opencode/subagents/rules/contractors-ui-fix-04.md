# contractors-ui-fix-04 — ContractorQuickAddModal + integracja z CostModal (dashboard)

## Cel
1. Stworzenie `ContractorQuickAddModal` — mały modal do szybkiego dodania kontrahenta z poziomu formularza kosztu
2. Integracja z `ContractorPicker` (włączenie `canQuickAdd`)
3. Aktualizacja `CostModal` w dashboard (niezależna implementacja formularza kosztów)

## Skill
Przeczytaj `.opencode/skills/ui/skill-ui-forms-modals.md` i `.opencode/skills/ui/skill-ui-components.md` przed implementacją.

## Kontekst
- Raport audytu UI: `.opencode/subagents/rules/contractors-ui-audit.md`
- `ContractorPicker` istnieje po `contractors-ui-fix-03`
- `AppModal` wzorzec: `src/components/ui/AppModal.tsx`
- `CostModal` (dashboard): `src/features/dashboard/components/CostModal.tsx` — NIEZALEŻNA implementacja, nie reużywa CostForm

## Zmiany do wykonania

### 1. Nowy komponent: `src/components/ContractorQuickAddModal.tsx`

Props:
```typescript
interface ContractorQuickAddModalProps {
  tenantId: string;
  isOpen: boolean;
  onClose: () => void;
  onCreated: (contractorId: string, contractorName: string) => void;
}
```

Implementacja:
- Używa `AppModal` (nie własna implementacja)
- Tytuł modala: „Dodaj kontrahenta"
- Pola formularza (tylko podstawowe — szybki add):
  - `name` — wymagane, `FormLabel`: „Nazwa *"
  - `taxId` — opcjonalne, `FormLabel`: „NIP"
  - `email` — opcjonalne
  - `phoneNumber` — opcjonalne, `FormLabel`: „Telefon"
- Przyciski: „Anuluj" (zamknij) i „Dodaj" (submit)
- Walidacja: `name` nie może być puste (inline, bez biblioteki)
- Po zapisaniu: wywołać `useCreateContractor(tenantId)` → po sukcesie wywołać `onCreated(result.id, result.name)` i zamknąć modal
- Błąd API: toast error z `useToastNotification`
- `isLoading` na przycisku Dodaj podczas mutacji

### 2. Integracja `ContractorQuickAddModal` z `ContractorPicker`

Plik: `src/components/ContractorPicker.tsx`

Dodać logikę quick-add:
```tsx
const [isQuickAddOpen, setIsQuickAddOpen] = useState(false);

// Przycisk + (tylko gdy canQuickAdd=true):
<IconButton
  aria-label="Dodaj nowego kontrahenta"
  icon={<AddIcon />}
  size="sm"
  variant="ghost"
  onClick={() => setIsQuickAddOpen(true)}
/>

// Modal na końcu komponentu:
{canQuickAdd && (
  <ContractorQuickAddModal
    tenantId={tenantId}
    isOpen={isQuickAddOpen}
    onClose={() => setIsQuickAddOpen(false)}
    onCreated={(id) => {
      onChange(id);
      setIsQuickAddOpen(false);
    }}
  />
)}
```

### 3. Komunikat dla non-admin użytkowników

W miejscach gdzie `canQuickAdd=false` ale user chce dodać nowego kontrahenta:
- Nie pokazuj przycisku + (ukryty warunkowo przez `canQuickAdd`)
- W `ContractorPicker` gdy `canQuickAdd=false` — brak przycisku +, żadnego komunikatu
- Komunikat „Aby dodać nowego kontrahenta, zgłoś się do administratora." wyświetlaj jako `Text` pomocniczy poniżej selecta — TYLKO w kontekście formularzy kosztów projektu, gdy user nie jest adminem

Implementacja w `CostFormDrawer.tsx` lub `CostFormModal.tsx`:
```tsx
{!canQuickAdd && (
  <Text fontSize="xs" color="gray.500" mt={1}>
    Aby dodać nowego kontrahenta, zgłoś się do administratora.
  </Text>
)}
```

### 4. Modyfikacja `src/features/dashboard/components/CostModal.tsx`

**UWAGA**: Ten komponent jest NIEZALEŻNY od `CostForm.tsx` — ma własny stan formularza.

Zmiany:
- Znajdź `CostFormState` lub `formState` z polem `contractor: string`
- Zmienić: `contractor: string` → `contractorId: string | null`
- Init z istniejącego kosztu: `contractor: cost?.contractor ?? ""` → `contractorId: cost?.contractorId ?? null`
- Zastąpić `<Input>` dla wykonawcy przez `<ContractorPicker>`:
  ```tsx
  <FormControl>
    <FormLabel>Wykonawca</FormLabel>
    <ContractorPicker
      tenantId={tenantId}
      value={formState.contractorId}
      onChange={(id) => setFormState(prev => ({ ...prev, contractorId: id }))}
      canQuickAdd={canQuickAdd}
      isDisabled={isSubmitting}
    />
  </FormControl>
  ```
- `tenantId` — odczytaj z kontekstu projektu/tenanta (sprawdź co jest dostępne w `CostModal`)
- `canQuickAdd` — analogicznie jak w innych formularzach (PROJECT.ADMIN lub TENANT.ADMIN)
- Submit: `contractor: form.contractor || null` → `contractorId: form.contractorId || null`
- W API call: zmienić pole `contractor` na `contractorId`

## Wymagania jakościowe
- Brak `any` w TypeScript
- `AppModal` zamiast własnej implementacji
- `useToastNotification` dla błędów
- Formularz w `ContractorQuickAddModal` musi mieć `isInvalid` na polu `name`
- Komunikat dla non-admin tylko w kontekście formularzy kosztu (nie w `ContractorPicker` samym)

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-Object -Last 20
npm run build 2>&1 | Select-Object -Last 10
```
Brak błędów TypeScript i build powinien przejść.
