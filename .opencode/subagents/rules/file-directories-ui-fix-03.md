# UI Fix 03 — CreateDirectoryModal + modyfikacja UploadFilesModal

## Cel
Stworzenie modala "Dodaj katalog" i aktualizacja `UploadFilesModal` o pole wyboru katalogu nadrzędnego.

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skill
Przeczytaj: `.opencode/skills/ui/skill-ui-forms-modals.md`
Przeczytaj: `.opencode/skills/ui/skill-ui-components.md`
Przeczytaj: `.opencode/skills/ui/skill-ui-accessibility.md`

## Kontekst

Przeczytaj najpierw:
- `src/components/UploadFilesModal.tsx` — obecna implementacja
- Jeden z istniejących modali jako wzorzec (np. sprawdź jak inne modale są zbudowane)

## Zadanie 1: Nowy plik `src/components/CreateDirectoryModal.tsx`

Modal do tworzenia nowego katalogu (bez uploadu plików).

Wymagania:
- Używa `AppModal` (pattern z projektu)
- Formularz z jednym polem: `Nazwa katalogu` (wymagane, max 200 znaków)
- Opcjonalny `Select` "Katalog nadrzędny" — lista katalogów dostępnych dla użytkownika (scope='mine')
  - Placeholder: "Brak — utwórz jako katalog główny"
  - Opcje budowane przez `flattenCatalogsForSelect(catalogs)` — helper do zaimplementowania w pliku
  - Format opcji: prefiksy `└─ ` dla podkatalogów (głębokość * 2 spacje + prefix)
- Przycisk "Utwórz katalog" wywołujący `useCreateDirectory`
- Po sukcesie: zamknięcie modala + `onSuccess()` callback
- Obsługa błędów: `handleApiError`

Props:
```typescript
interface CreateDirectoryModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  tenantId: string;
  projectId: string;
  catalogs: ProjectFilePackageWeb[]; // drzewo katalogów (dla Select)
  defaultParentId?: string;           // pre-uzupełniony przy "Dodaj podkatalog"
}
```

Helper `flattenCatalogsForSelect`:
```typescript
interface FlatCatalogOption {
  id: string;
  label: string; // z wcięciem i prefixem
  depth: number;
}

function flattenCatalogsForSelect(
  catalogs: ProjectFilePackageWeb[],
  depth = 0
): FlatCatalogOption[] {
  const result: FlatCatalogOption[] = [];
  for (const cat of catalogs) {
    const prefix = depth === 0 ? '' : '  '.repeat(depth) + '└─ ';
    result.push({ id: cat.id, label: prefix + cat.name, depth });
    if (cat.subCatalogs?.length) {
      result.push(...flattenCatalogsForSelect(cat.subCatalogs, depth + 1));
    }
  }
  return result;
}
```

Wzorzec modal — `aria-hidden="true"` na dekoracyjnych ikonach.

## Zadanie 2: Modyfikacja `src/components/UploadFilesModal.tsx`

### Zmiana etykiet "paczka" → "katalog":
- "Nowa paczka" → "Nowy katalog"
- "Istniejąca paczka" → "Istniejący katalog"
- "Nazwa paczki" → "Nazwa katalogu"
- Dowolne inne wystąpienia "paczka"/"Paczka" → "katalog"/"Katalog"

### Dodanie pola "Katalog nadrzędny" (opcjonalne, w trybie "nowy katalog"):

Dodać stan:
```typescript
const [parentDirectoryId, setParentDirectoryId] = useState<string>('');
```

Po polu "Nazwa katalogu" dodać `FormControl`:
```tsx
<FormControl>
  <FormLabel>Katalog nadrzędny (opcjonalnie)</FormLabel>
  <Select
    value={parentDirectoryId}
    onChange={(e) => setParentDirectoryId(e.target.value)}
    placeholder="Brak — utwórz jako katalog główny"
    isDisabled={uploading}
  >
    {flattenCatalogsForSelect(packages).map(item => (
      <option key={item.id} value={item.id}>
        {item.label}
      </option>
    ))}
  </Select>
  <FormHelperText>Jeśli nie wybierzesz, katalog zostanie dodany jako główny</FormHelperText>
</FormControl>
```

Dodać `flattenCatalogsForSelect` (ten sam helper co w `CreateDirectoryModal` — rozważ wyciągnięcie do `src/utils/`).

Przy submit dodać `parentId` do formData:
```typescript
if (parentDirectoryId) {
  formData.append('ParentId', parentDirectoryId);
}
```

**Czyste up po zamknięciu modala:** reset `parentDirectoryId` do `''` przy onClose/onSuccess.

## Dostępność WCAG AA
- Dekoracyjne ikony: `aria-hidden="true"`
- Nie zagnieżdżać `<button>` w `<button>`
- Używać `FormControl` + `FormLabel` + `FormHelperText` (Chakra zarządza ARIA automatycznie)

## Weryfikacja
```
npx tsc --noEmit
```
Brak błędów TypeScript.
