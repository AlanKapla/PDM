# UI Audit — Feature: file-directories

**Data audytu:** 2026-06-01  
**Agent:** ui-audit-agent  
**Feature spec:** `.opencode/features/file-directories.md`

---

## Kontekst feature

Zmiana modelu płaskiego `Paczka → Pliki` na hierarchiczny `Katalog → Podkatalogi → Pliki`.  
Kluczowe zasady:
- `ParentId = null` → katalog główny (root)
- Nieograniczone zagnieżdżenie podkatalogów
- Pliki mogą leżeć bezpośrednio w katalogu nadrzędnym (obok podkatalogów)
- Udostępnienie katalogu = kaskadowe udostępnienie wszystkich podkatalogów
- UI: "paczka" → "katalog" (kod API pozostaje `Package`)

---

## BLOK 1 — Stan obecny UI

| Komponent/Plik | Lokalizacja | Opis | Powiązane z feature |
|---|---|---|---|
| `ProjectFiles.tsx` | `src/pages/ProjectFiles.tsx` | Strona główna plików. 3 zakładki (All/Mine/Shared), każda renderuje `FilesTab` → `Accordion` płaski z paczkami | ✅ Główna strona do przebudowy |
| `FilesTab` (inline) | `ProjectFiles.tsx` ~L632 | Renderuje `Accordion` z listą paczek. Każda paczka ma `AccordionItem` → `PackageFiles` (lazy load). Brak rekurencji | ✅ Wymaga zagnieżdżonego accordion |
| `PackageFiles` (inline) | `ProjectFiles.tsx` ~L490 | Lazy-load plików paczki przez `usePackageFiles`. Renderuje `FileRow` | ⚠️ Musi renderować też podkatalogi |
| `FileRow` (inline) | `ProjectFiles.tsx` ~L235 | Wiersz pliku z wersjami i komentarzami | ✅ Bez zmian struktury |
| `UploadFilesModal.tsx` | `src/components/UploadFilesModal.tsx` | Modal upload plików. Tryby: "nowa paczka" / "istniejąca paczka". RadioGroup + Select | ✅ Wymaga pola "katalog nadrzędny" |
| `ShareFilesModal.tsx` | `src/components/ShareFilesModal.tsx` | Modal grupowego udostępniania. Checkbox lista paczek + lista użytkowników | ⚠️ Wymaga informacji o kaskadowości |
| `useProjectFiles.ts` | `src/hooks/queries/useProjectFiles.ts` | Hooki RQ: `useFilePackages`, `usePackageFiles`, `useFileVersions`, `useVersionComments` | ✅ Wymaga nowego hooka |
| `projectApi.ts` | `src/api/projectApi.ts` | Klient API: `createPackageAndUploadFiles`, `addFilesToPackage`, `getProjectFilePackages`, `sharePackages` | ✅ Wymaga nowych funkcji |
| `project.types.ts` | `src/types/project.types.ts` | `ProjectFilePackageWeb` — płaska struktura, brak `parentId`/`subCatalogs` | ✅ Wymaga rozszerzenia |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|---|---|---|---|
| `parentId` + `subCatalogs` w typie | typ TypeScript | 🔴 Krytyczny | `ProjectFilePackageWeb` nie ma pól hierarchii |
| Rekurencyjny rendering katalogów | komponent | 🔴 Krytyczny | `FilesTab` renderuje płaski accordion, nie obsługuje drzewa |
| Wybór katalogu nadrzędnego w UploadModal | komponent | 🔴 Krytyczny | Brak UI do wskazania `ParentId` przy upload |
| Tworzenie katalogu bez plików | komponent | 🟡 Wysoki | Brak przycisku "Dodaj katalog" i odpowiedniego modala |
| `createDirectory` w API client | API | 🟡 Wysoki | Brak funkcji POST `/file/directories` |
| `parentId` w `createPackageAndUploadFiles` | API | 🔴 Krytyczny | FormData nie wysyła `ParentId` |
| Kaskadowość w ShareFilesModal (info UX) | komponent | 🟡 Wysoki | Brak info że udostępnienie katalogu = udostępnienie podkatalogów |
| Hook dla drzewa katalogów | hook | 🟡 Wysoki | `useFilePackages` zwraca flat list — po API zmianie zwróci drzewo, hook nie musi się zmieniać strukturalnie, ale typy muszą |
| Odróżnienie root vs sub-katalog w UI | komponent | 🟡 Wysoki | Brak indentacji/ikony dla podkatalogów |
| Zmiana etykiet "paczka" → "katalog" | i18n/strings | 🟢 Niski | Teksty w UI nadal używają "paczka" |

---

## BLOK 3 — Typy TypeScript

### Obecny stan `ProjectFilePackageWeb` (projekt.types.ts L92–100):

```typescript
export interface ProjectFilePackageWeb {
  id: string;
  name: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  files: ProjectFileWeb[];
  totalFiles: number;
}
```

### Wymagane zmiany:

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|---|---|---|---|
| `ProjectFilePackageWeb` | `src/types/project.types.ts` | Modyfikacja | Dodać `parentId: string \| null`, `subCatalogs: ProjectFilePackageWeb[]` |
| `CreateDirectoryCommand` | `src/types/project.types.ts` | Nowy | `{ name: string; parentId?: string }` — payload dla POST /file/directories |

### Docelowy interfejs:

```typescript
export interface ProjectFilePackageWeb {
  id: string;
  name: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  files: ProjectFileWeb[];
  totalFiles: number;
  parentId: string | null;           // 🆕 null = katalog główny (root)
  subCatalogs: ProjectFilePackageWeb[]; // 🆕 rekurencja
}
```

> **Uwaga:** `subCatalogs` to recursive type — TypeScript obsługuje to natywnie w interface (nie w type alias bez `interface`).

---

## BLOK 4 — Serwisy API (src/api/projectApi.ts)

### Obecne funkcje (linie ~81–200):

| Funkcja | Endpoint | Opis |
|---|---|---|
| `createPackageAndUploadFiles` | `POST /file/packages/create` | Tworzy paczkę + upload plików. FormData bez `ParentId` |
| `addFilesToPackage` | `POST /file` | Dodaje pliki do istniejącej paczki. FormData z `ProjectFilePackageId` |
| `getProjectFilePackages` | `GET /file/packages/{scope}` | Pobiera listę paczek per scope. Zwróci drzewo po zmianie API |
| `sharePackages` | `POST /file/packages/share` | Udostępnia paczki wielu użytkownikom |

### Wymagane zmiany:

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|---|---|---|---|---|
| `createPackageAndUploadFiles` | `projectApi.ts` | Modyfikacja | `POST /file/packages/create` | Dodać `ParentId` do FormData (opcjonalne) |
| `createDirectory` | `projectApi.ts` | Nowa | `POST /file/directories` | Tworzy katalog bez plików: `{ name, parentId? }` |

### Docelowe sygnatury:

```typescript
// Modyfikacja — dodać parentId
createPackageAndUploadFiles: async (
  tenantId: string,
  projectId: string,
  packageName: string,
  files: Array<{ file: File; displayName?: string; comment?: string }>,
  parentId?: string   // 🆕 opcjonalny ParentId
) => {
  const formData = new FormData();
  formData.append('PackageName', packageName);
  if (parentId) {
    formData.append('ParentId', parentId);  // 🆕
  }
  // ... reszta bez zmian
}

// Nowa funkcja
createDirectory: async (
  tenantId: string,
  projectId: string,
  name: string,
  parentId?: string
) => {
  return axiosClient.post(
    `/tenants/${tenantId}/projects/${projectId}/file/directories`,
    { name, parentId: parentId ?? null }
  );
},
```

---

## BLOK 5 — Hooki React Query

### Obecny stan `useProjectFiles.ts`:

| Hook | Query key | Opis |
|---|---|---|
| `useFilePackages` | `['project-files', tenantId, projectId, 'packages', scope]` | Zwraca `ProjectFilePackageWeb[]` — po API zmianie zwróci drzewo |
| `usePackageFiles` | `['project-files', ..., 'package-files', packageId, scope]` | Lazy load plików paczki |
| `useFileVersions` | `['project-files', ..., 'versions', fileId, scope]` | Wersje pliku |
| `useVersionComments` | `['project-files', ..., 'comments', fileId, versionId, scope]` | Komentarze |

### Wymagane zmiany:

| Hook | Plik | Nowy/Modyfikacja | Opis |
|---|---|---|---|
| `useFilePackages` | `useProjectFiles.ts` | Modyfikacja typów | Zwracany typ `ProjectFilePackageWeb[]` — po dodaniu `subCatalogs` do interfejsu hook zwróci drzewo automatycznie. Brak zmian logiki. |
| `useCreateDirectory` | `useProjectFiles.ts` | Nowy | `useMutation` wywołujący `projectApi.createDirectory`. Invaliduje `fileKeys.packages` |

### Docelowy nowy hook:

```typescript
export function useCreateDirectory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      tenantId,
      projectId,
      name,
      parentId,
    }: {
      tenantId: string;
      projectId: string;
      name: string;
      parentId?: string;
    }) => projectApi.createDirectory(tenantId, projectId, name, parentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all });
    },
  });
}
```

---

## BLOK 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|---|---|---|---|
| `DirectoryNode` | `ProjectFiles.tsx` (inline lub osobny plik) | Rekurencyjny `AccordionItem` renderujący katalog + jego `subCatalogs` i pliki. Zastępuje `PackageFiles` jako sub-component | `ProjectFilePackageWeb` z `subCatalogs`, `usePackageFiles` |
| `CreateDirectoryModal` | `src/components/CreateDirectoryModal.tsx` | Modal z formularzem: `Nazwa katalogu` + opcjonalny `Select` "Katalog nadrzędny". Przycisk "Dodaj katalog" | `useCreateDirectory`, `ProjectFilePackageWeb[]` |

### `DirectoryNode` — szkic struktury:

```tsx
interface DirectoryNodeProps {
  catalog: ProjectFilePackageWeb;
  depth: number;           // dla indentacji (pl: 0, 1, 2...)
  // ...reszta props jak PackageFiles
}

const DirectoryNode: React.FC<DirectoryNodeProps> = ({ catalog, depth, ...rest }) => {
  const hasSubCatalogs = catalog.subCatalogs?.length > 0;
  const isExpanded = expandedPackageIds.has(catalog.id);

  return (
    <AccordionItem key={catalog.id} ml={depth * 4} /* indentacja */ >
      <AccordionButton onClick={() => onTogglePackage(catalog.id)}>
        <Icon as={FolderOpen} /> {catalog.name}
        {/* badge z totalFiles */}
        {/* przycisk "Dodaj podkatalog" */}
      </AccordionButton>
      <AccordionPanel>
        {/* Pliki bezpośrednio w tym katalogu */}
        <PackageFiles packageId={catalog.id} ... />

        {/* Podkatalogi — rekurencja */}
        {hasSubCatalogs && catalog.subCatalogs.map(sub => (
          <DirectoryNode key={sub.id} catalog={sub} depth={depth + 1} {...rest} />
        ))}
      </AccordionPanel>
    </AccordionItem>
  );
};
```

---

## BLOK 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|---|---|---|---|
| `FilesTab` | `ProjectFiles.tsx` | Refaktor renderowania | Zamiast flat `packages.map(...)` → `rootCatalogs.map(pkg => <DirectoryNode depth={0} .../>)`. Root katalogi to te z `parentId === null` |
| `UploadFilesModal` | `UploadFilesModal.tsx` | Dodanie sekcji "Katalog nadrzędny" | W trybie "nowy katalog": dodać opcjonalny `Select` "Katalog nadrzędny" (lista płaska/drzewo katalogów użytkownika). W trybie "istniejący katalog": bez zmian (już wybiera katalog). Przekazać `parentId` do `createPackageAndUploadFiles` |
| `ShareFilesModal` | `ShareFilesModal.tsx` | Zmiana info w `Alert` | Zmienić tekst `Alert` na: "Udostępniasz katalogi wraz z podkatalogami. Wszyscy wybrani członkowie otrzymają dostęp do wszystkich plików i podkatalogów w zaznaczonych katalogach." |
| `ShareFilesModal` | `ShareFilesModal.tsx` | Zmiana labeli | "paczka" → "katalog" we wszystkich tekstach |
| `UploadFilesModal` | `UploadFilesModal.tsx` | Zmiana labeli | "paczka" → "katalog", "Nowa paczka" → "Nowy katalog", "Istniejąca paczka" → "Istniejący katalog" |
| `FilesTab` (SCOPE_CONFIG) | `ProjectFiles.tsx` | Zmiana labeli | Ikony folderów zamiast `FileText` dla `packageIcon` |
| `ProjectFiles` (page header) | `ProjectFiles.tsx` | Dodanie przycisku | Przycisk "Dodaj katalog" (obok "Dodaj pliki") otwierający `CreateDirectoryModal` |

### Szczegóły zmiany `UploadFilesModal` — sekcja "Katalog nadrzędny":

```tsx
// Nowy stan
const [parentDirectoryId, setParentDirectoryId] = useState<string>("");

// W FormData
if (parentDirectoryId) {
  formData.append('ParentId', parentDirectoryId);  // tylko jeśli wybrano
}

// Nowy UI element (w trybie "new", po polu "Nazwa katalogu"):
<FormControl>
  <FormLabel>Katalog nadrzędny (opcjonalnie)</FormLabel>
  <Select
    value={parentDirectoryId}
    onChange={(e) => setParentDirectoryId(e.target.value)}
    placeholder="Brak — utwórz katalog główny"
    isDisabled={uploading || loadingPackages}
  >
    {/* flatten drzewa dla Select — wszystkie katalogi z wcięciem nazwy */}
    {flattenCatalogsForSelect(packages).map(item => (
      <option key={item.id} value={item.id}>
        {"  ".repeat(item.depth)}{item.name}
      </option>
    ))}
  </Select>
  <Text fontSize="xs" color="neutral.500" mt={1}>
    Jeśli nie wybierzesz, katalog zostanie dodany jako główny
  </Text>
</FormControl>
```

> **Problem:** HTML `<option>` nie obsługuje wcięć tekstowych dobrze. Alternatywa: Chakra `Select` z prefixem `└─` dla podkatalogów lub użyć dedykowanego `TreeSelect` (third-party). Rekomendacja: prefiksy tekstowe `└─` lub `  ` (spaces) — wystarczające dla MVP.

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---|---|---|
| Accordion z lazy load | `FilesTab` / `PackageFiles` — accordion z lazy load na expand | ✅ Tak — `DirectoryNode` musi stosować ten sam wzorzec |
| Modal upload | `UploadFilesModal` — `size="full"` z `maxW={{ base: "100%", md: "600px" }}` | ✅ Tak — `CreateDirectoryModal` i modyfikacja `UploadFilesModal` |
| Modal shareshare | `ShareFilesModal` — Checkbox lista + Divider + Alert info | ✅ Tak — zmiana tylko tekstu Alert |
| `useDisclosure` + conditional render | `ProjectFiles` używa `isOpen && <Modal ...>` dla upload/version | ✅ Tak — `CreateDirectoryModal` analogicznie |
| React Query invalidation | `refreshData()` invaliduje `fileKeys.all` | ✅ `useCreateDirectory` powinien invalidować `fileKeys.all` |
| Error handling | `handleApiError` + `showError` | ✅ Tak — `CreateDirectoryModal` |
| Separator paczki/katalogu | `mb={3}` na `AccordionItem` | ✅ Zachować |
| Responsive labels | `display={{ base: "none", md: "table-cell" }}` | ✅ Zachować w `FileRow` |
| Naming: `pkg` dla paczki | Wszystkie zmienne: `pkg`, `packages`, `myPackages` | ⚠️ Można zostawić wewnętrznie (kod API), UI labels zmienić |

---

## BLOK 9 — Dostępność (WCAG AA / AXE) — OBOWIĄZKOWY

### Kontrast kolorów

| Element | Kolor tekstu | Kolor tła | Status |
|---|---|---|---|
| Nazwa paczki w AccordionButton | `fontWeight="semibold"` domyślny (neutral.800) | white | ✓ |
| Badge `totalFiles` | Chakra colorScheme badge | badge bg | ✓ |
| `ownerLabel` w AccordionButton | `neutral.500` | white | ⚠️ Sprawdź kontrast (~4.5:1 granica) |
| `Text fontSize="xs"` `color="neutral.500"` w `UploadFilesModal` | neutral.500 | white | ⚠️ Sprawdź — xs + neutral.500 może być za niski |
| `color="gray.500"` w `ShareFilesModal` | gray.500 | white | ⚠️ Użyto `gray.500` zamiast tokenów — sprawdź kontrast |

### Atrybuty ARIA

| Komponent | Problem | Rekomendacja |
|---|---|---|
| `AccordionButton` z `onClick={() => onTogglePackage(pkg.id)}` | Chakra Accordion zarządza ARIA automatycznie (`aria-expanded`) | ✓ OK |
| `Package` icon w `ShareFilesModal` obok tekstu nazwy paczki | Brak `aria-hidden="true"` na ikonie | Dodać `aria-hidden="true"` na `<Package size={16} />` |
| `Share2` icon w `ShareFilesModal` nagłówku | Dekoracyjna ikona obok tekstu bez `aria-hidden` | Dodać `aria-hidden="true"` |
| Nowy `DirectoryNode` — `AccordionButton` z przyciskiem "Dodaj podkatalog" | Zagnieżdżony `<button>` w `<button>` (AccordionButton) — niedozwolone w HTML | Przycisk "Dodaj podkatalog" musi być **poza** `AccordionButton`, np. w `HStack` na końcu, użyć `e.stopPropagation()` |
| `CreateDirectoryModal` (nowy) — `Select` dla katalogu nadrzędnego | Brak `aria-describedby` jeśli używamy pomocniczego tekstu | Użyć `FormControl` z `FormHelperText` (Chakra zarządza ARIA) |

### Zarządzanie fokusem

- `UploadFilesModal` — używa Chakra `Modal` z `FocusTrap` ✓
- `ShareFilesModal` — używa Chakra `Modal` z `FocusTrap` ✓
- `CreateDirectoryModal` (nowy) — musi używać Chakra `Modal` ✓ (wzorzec wymuszony przez `AppModal` lub Chakra)
- `DirectoryNode` — `AccordionButton` z zagnieżdżonym przyciskiem akcji wymaga `stopPropagation` i `tabIndex` handling

### Testy AXE

Żaden z istniejących komponentów nie ma testów AXE. Do dodania dla nowych/zmodyfikowanych:
- `DirectoryNode` — nowy komponent
- `UploadFilesModal` — po modyfikacji
- `CreateDirectoryModal` — nowy komponent

### Podsumowanie dostępności

| Kategoria | Status | Uwagi |
|---|---|---|
| Kontrast kolorów | ⚠️ | `gray.500` / `neutral.500` przy small text do weryfikacji |
| Atrybuty ARIA | ⚠️ | Dekoracyjne ikony bez `aria-hidden`, ryzyko zagnieżdżonych buttonów |
| Klawiatura / fokus | ✓ | Chakra Accordion + Modal obsługują focus automatycznie |
| Testy AXE | ✗ | Brak testów AXE — do dodania dla nowych komponentów |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---|---|---|---|
| 1 | `Accordion allowMultiple` z `index={expandedIndices}` — `useAccordionIndex` mapuje płaską tablicę paczek na indeksy. Z rekurencją ta logika się psuje | `FilesTab`, `useAccordionIndex` hook | 🔴 Wysoki | Zmienić model na `expandedPackageIds: Set<string>` (już istnieje) bez kontrolowanego `index`. Usunąć `useAccordionIndex` z `FilesTab`, używać `uncontrolled` Accordion lub zarządzać per-node. |
| 2 | `UploadFilesModal` ma duplikat `formatFileSize` — jest zdefiniowany lokalnie i importowany z `utils/formatters` | `UploadFilesModal.tsx` | 🟡 Średni | Usunąć lokalną definicję (issue niezwiązane z feature, ale warto naprawić przy okazji) |
| 3 | `ShareFilesModal` nie buduje drzewa — `myPackages` props to flat list | `ShareFilesModal.tsx` | 🔴 Wysoki | Po zmianie API, `ProjectFilePackageWeb[]` będzie zawierać `subCatalogs`. `ShareFilesModal` musi wyświetlać katalogi z wizualnym zaznaczeniem że zaznaczenie parent = zaznaczenie children (info w Alert) lub implementować cascade checkbox. |
| 4 | Zagnieżdżony `AccordionItem` w Chakra UI — `Accordion` nie jest oficjalnie zaprojektowany na rekurencję wewnątrz `AccordionPanel` | `DirectoryNode` (nowy) | 🟡 Średni | Testować głębokość 2–3 poziomów. Alternatywa: custom Tree component zamiast Accordion dla zagnieżdżeń > 1. Dla MVP Accordion zagnieżdżony w AccordionPanel działa. |
| 5 | `PackageFiles` używa `<Table>` z `<Tbody>` — zagnieżdżona tabela lub accordion w `<Td>` może być niestandardowy HTML | `PackageFiles`, `FileRow` | 🟡 Średni | Podkatalogi renderować **przed** wierszami plików lub jako osobną sekcję poza `<Table>`, np. w `<Box>` przed tabelą. |
| 6 | `getProjectFilePackages` scope='mine' w `UploadFilesModal` — po zmianie API zwróci drzewo zamiast flat list. `Select` w UploadModal dla "istniejący katalog" musi flattenować drzewo | `UploadFilesModal.tsx` | 🟡 Średni | Dodać helper `flattenCatalogsForSelect(tree: ProjectFilePackageWeb[]): FlatItem[]` |
| 7 | Duplikat walidacji nazwy katalogu z `parentId` — unikalność zmienia się na `(parentId, name)` zamiast `name` | `UploadFilesModal.tsx` | 🟡 Średni | Walidacja duplikatu w UI musi uwzględniać scope `parentId`: sprawdzać `packages` tylko w wybranym katalogu nadrzędnym |
| 8 | `ShareFilesModal` — `onSuccess` message używa "paczek" hardcoded | `ShareFilesModal.tsx` L108 | 🟢 Niski | Zmienić `paczek` → `katalogów` |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---|---|
| Nowe komponenty | 2 (`DirectoryNode`, `CreateDirectoryModal`) |
| Zmodyfikowane komponenty | 3 (`FilesTab`, `UploadFilesModal`, `ShareFilesModal`) |
| Nowe hooki | 1 (`useCreateDirectory`) |
| Nowe typy TypeScript | 1 (`CreateDirectoryCommand`) + modyfikacja `ProjectFilePackageWeb` |
| Nowe wywołania API | 1 (`createDirectory`) + modyfikacja `createPackageAndUploadFiles` |
| Naruszenia WCAG AA | 3 (dekoracyjne ikony bez aria-hidden, brak testów AXE) |
| Pytania domenowe | 4 |

---

## Pytania domenowe wymagające decyzji

1. **Checkbox kaskadowy w ShareFilesModal** — Czy zaznaczenie katalogu nadrzędnego ma wizualnie zaznaczać też checkboxy podkatalogów (cascade UI), czy tylko info w Alert że udostępnienie jest kaskadowe po stronie API? MVP: tylko Alert info jest prostszy.

2. **Select dla katalogu nadrzędnego w UploadModal** — Czy używamy płaskiego `<Select>` z prefixami (`└─ NazwaPodkat`), czy dedykowanego komponentu drzewa? Flat Select wystarczy dla płytkiej hierarchii (1–2 poziomy), ale może być nieczytelny przy 3+ poziomach.

3. **Widoczność podkatalogów w FilesTab scope=All** — Czy admin (scope=All) widzi cudze podkatalogi jako odrębne węzły drzewa, czy zagnieżdżone pod właścicielem? Feature spec mówi tylko o własnych podkatalogach dla scope=Mine.

4. **"Dodaj podkatalog" in-place** — Czy przycisk "Dodaj podkatalog" w AccordionButton katalogu otwiera `CreateDirectoryModal` z pre-uzupełnionym `parentId`, czy osobne pole inline? In-line edit jest bardziej UX-friendly ale bardziej złożony.

---

## Pełna zawartość kluczowych plików (snapshot)

### `src/types/project.types.ts` — fragment (L92–100)

```typescript
export interface ProjectFilePackageWeb {
  id: string;
  name: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  files: ProjectFileWeb[];
  totalFiles: number;
  // BRAK: parentId, subCatalogs
}
```

### `src/api/projectApi.ts` — istniejące funkcje plików (L81–200)

```typescript
// Tworzy paczkę + upload plików (BRAK parentId)
createPackageAndUploadFiles: async (tenantId, projectId, packageName, files) => {
  const formData = new FormData();
  formData.append('PackageName', packageName);
  files.forEach((item, index) => {
    formData.append(`Files[${index}].File`, item.file);
    if (item.displayName) formData.append(`Files[${index}].DisplayName`, item.displayName);
    if (item.comment) formData.append(`Files[${index}].Comment`, item.comment);
  });
  return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/file/packages/create`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  });
},

// Dodaje pliki do istniejącej paczki
addFilesToPackage: async (tenantId, projectId, packageId, files) => { ... },

// Pobiera paczki per scope (zwróci drzewo po zmianie API)
getProjectFilePackages: async (tenantId, projectId, scope) => {
  const scopeRoute = resourceScopeToRoute(scope);
  return axiosClient.get(`/tenants/${tenantId}/projects/${projectId}/file/packages/${scopeRoute}`);
},

// Udostępnia paczki wielu użytkownikom
sharePackages: async (tenantId, projectId, packageIds, sharedWithUserIds) => {
  return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/file/packages/share`, {
    tenantId, projectId, packageIds, sharedWithUserIds,
  });
},
// BRAK: createDirectory
```

### `src/hooks/queries/useProjectFiles.ts` — obecne hooki

```typescript
export const fileKeys = {
  all: ['project-files'] as const,
  packages: (tenantId, projectId, scope) => ['project-files', tenantId, projectId, 'packages', scope] as const,
  packageFiles: (tenantId, projectId, packageId, scope) => [...] as const,
  fileVersions: (...) => [...] as const,
  versionComments: (...) => [...] as const,
};

export function useFilePackages(tenantId, projectId, scope, enabled = true) {
  return useQuery<ProjectFilePackageWeb[]>({
    queryKey: fileKeys.packages(tenantId ?? '', projectId ?? '', scope),
    queryFn: async () => (await projectApi.getProjectFilePackages(tenantId!, projectId!, scope)).data,
    enabled: Boolean(tenantId && projectId) && enabled,
  });
}
// BRAK: useCreateDirectory
```

### `src/pages/ProjectFiles.tsx` — struktura AccordionItem (L694–730)

```tsx
<Accordion allowMultiple index={expandedIndices}>
  {packages.map((pkg) => {
    const isPackageExpanded = expandedPackageIds.has(pkg.id);
    return (
      <AccordionItem key={pkg.id} bg="white" borderWidth="1px" borderColor="neutral.200" rounded="md" mb={3}>
        <AccordionButton py={4} _hover={{ bg: 'neutral.50' }} onClick={() => onTogglePackage(pkg.id)}>
          <HStack flex="1" spacing={3}>
            <Icon as={config.packageIcon} boxSize={5} color={config.packageIconColor} />
            <Text fontWeight="semibold" fontSize="md">{pkg.name}</Text>
            <Badge colorScheme={config.badgeColor} fontSize="sm">{pkg.totalFiles}</Badge>
            {config.showOwnerInPackage && pkg.ownerName && (
              <Text fontSize="sm" color="neutral.500">{config.ownerLabel}: {pkg.ownerName}</Text>
            )}
          </HStack>
          <AccordionIcon />
        </AccordionButton>
        <AccordionPanel pb={4}>
          {/* PackageFiles + FileRow table */}
          <PackageFiles packageId={pkg.id} isExpanded={isPackageExpanded} ... />
        </AccordionPanel>
      </AccordionItem>
    );
  })}
</Accordion>
// Problem: flat map — nie obsługuje subCatalogs
```

### `src/components/UploadFilesModal.tsx` — sekcja wyboru trybu (L174–230)

```tsx
<RadioGroup value={mode} onChange={(value) => setMode(value as "new" | "existing")}>
  <Stack direction="row" spacing={4}>
    <Radio value="new">Nowa paczka</Radio>      // → zmienić na "Nowy katalog"
    <Radio value="existing">Istniejąca paczka</Radio>  // → "Istniejący katalog"
  </Stack>
</RadioGroup>

{mode === "new" ? (
  <FormControl isRequired isInvalid={!!packageNameError}>
    <FormLabel>Nazwa paczki</FormLabel>   // → "Nazwa katalogu"
    <Input ... />
    // 🆕 TU dodać Select dla katalogu nadrzędnego
  </FormControl>
) : (
  <FormControl isRequired>
    <FormLabel>Wybierz paczkę</FormLabel>  // → "Wybierz katalog"
    <Select ...>
      {packages.map(pkg => <option key={pkg.id}>{pkg.name} ({pkg.totalFiles})</option>)}
      // 🆕 Flatten drzewa dla opcji
    </Select>
  </FormControl>
)}
```

### `src/components/ShareFilesModal.tsx` — sekcja Alert (L153)

```tsx
<Alert status="info" fontSize="xs">
  <AlertIcon />
  Udostępniasz całe paczki plików. Członkowie będą mieli dostęp do wszystkich plików i wersji w wybranych paczkach.
  // 🆕 Zmienić na: "Udostępniasz katalogi wraz z podkatalogami. Wszyscy wybrani 
  // członkowie otrzymają dostęp do wszystkich plików i podkatalogów w zaznaczonych katalogach."
</Alert>
```

---

## Rekomendacje implementacyjne

### Kolejność prac (sugerowana)

1. **Typy** — Dodać `parentId` i `subCatalogs` do `ProjectFilePackageWeb`
2. **API client** — Dodać `parentId` do `createPackageAndUploadFiles`, dodać `createDirectory`
3. **Hook** — Dodać `useCreateDirectory` mutation
4. **`CreateDirectoryModal`** — Nowy prosty modal z 2 polami
5. **`DirectoryNode`** — Rekurencyjny AccordionItem, zastąpić flat map w `FilesTab`
6. **`UploadFilesModal`** — Dodać pole katalogu nadrzędnego + flatten helper
7. **`ShareFilesModal`** — Zmiana tekstów Alert + labels
8. **Etykiety** — Zamiana "paczka" → "katalog" w całym UI
9. **Testy AXE** — Dla nowych komponentów

### Krytyczne implementacyjne wskazówki

- **`useAccordionIndex` problem:** Obecny `FilesTab` używa `index={expandedIndices}` w `Accordion`. Z rekurencją to nie działa. Rozwiązanie: zmienić `Accordion` na `uncontrolled` (bez `index` prop), a otwieranie/zamykanie obsługiwać tylko przez `expandedPackageIds: Set<string>` z `defaultIndex` lub w ogóle nie używać controlled mode.

- **Flatten helper dla Select:** Potrzebna helper function:
  ```typescript
  function flattenCatalogsForSelect(
    catalogs: ProjectFilePackageWeb[],
    depth = 0
  ): Array<{ id: string; name: string; depth: number }> {
    return catalogs.flatMap(cat => [
      { id: cat.id, name: cat.name, depth },
      ...flattenCatalogsForSelect(cat.subCatalogs ?? [], depth + 1),
    ]);
  }
  ```

- **Zagnieżdżony Accordion:** Chakra UI `Accordion` można zagnieżdżać w `AccordionPanel` — każdy `Accordion` jest osobny. Nie ma wspólnego `allowMultiple` — każdy poziom zarządza się niezależnie. Najlepiej używać jednego globalnego `expandedPackageIds: Set<string>` z `useReducer`/`useState` w `ProjectFiles` page.

- **ShareFilesModal cascade:** Nie potrzeba zmiany struktury komponentu — wystarczy zmiana tekstu w `Alert`. API robi cascade po stronie serwera. UI nie musi implementować cascade checkboxów w MVP.
