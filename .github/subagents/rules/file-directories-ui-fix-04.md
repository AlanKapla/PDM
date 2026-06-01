# UI Fix 04 — ProjectFiles page: zagnieżdżony accordion + nowe przyciski

## Cel
Przebudowa strony `ProjectFiles.tsx` — zagnieżdżony accordion dla drzewa katalogów, zmiana etykiet "paczka" → "katalog", nowy przycisk "Dodaj katalog" per katalog.

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skill
Przeczytaj: `.github/skills/ui/skill-ui-components.md`
Przeczytaj: `.github/skills/ui/skill-ui-accessibility.md`

## Kontekst

Przeczytaj najpierw plik `src/pages/ProjectFiles.tsx` — zrozum aktualną strukturę komponentów.

## Kluczowe decyzje architektoniczne

### 1. Accordion — zmiana na uncontrolled
Obecny `FilesTab` używa `useAccordionIndex` do mapowania płaskiej listy paczek na indeksy accordion. Ta logika psuje się przy rekurencji.

**Zmiana:** Użyć `expandedPackageIds: Set<string>` (może już istnieć) zamiast indeksów. Chakra `Accordion` bez `index` prop (uncontrolled) lub z własnym state per katalog. Sprawdź co już istnieje w kodzie.

### 2. Zagnieżdżony `<button>` — NIEDOZWOLONE w HTML
Przycisk "Dodaj podkatalog" MUSI być **poza** `<AccordionButton>`, nie wewnątrz. 

Rozwiązanie: umieść przycisk "Dodaj podkatalog" w `HStack` wewnątrz `AccordionButton` używając `stopPropagation`:
```tsx
<AccordionButton>
  <Box flex="1" textAlign="left">
    {/* nazwa katalogu */}
  </Box>
  {/* Przycisk musi być wewnątrz AccordionButton ale jako IconButton z stopPropagation */}
  <IconButton
    as="span"  // LUB użyj onClick ze stopPropagation
    aria-label="Dodaj podkatalog"
    icon={<FolderPlus size={14} />}
    size="xs"
    variant="ghost"
    onClick={(e) => {
      e.stopPropagation(); // nie toggle accordion
      setCreateDirParentId(catalog.id);
      onCreateDirOpen();
    }}
  />
  <AccordionIcon />
</AccordionButton>
```

Alternatywa: przenieś przyciski akcji do `AccordionPanel` poza `<Table>`.

### 3. Podkatalogi renderowane poza `<Table>`
Podkatalogi renderować w `AccordionPanel` **przed** tabelą plików:
```tsx
<AccordionPanel>
  {/* Podkatalogi */}
  {catalog.subCatalogs?.length > 0 && (
    <Box ml={4} mb={2}>
      {catalog.subCatalogs.map(sub => (
        <DirectoryNode key={sub.id} catalog={sub} depth={depth + 1} {...props} />
      ))}
    </Box>
  )}
  
  {/* Pliki bezpośrednio w tym katalogu */}
  <PackageFiles packageId={catalog.id} ... />
</AccordionPanel>
```

## Zmiany w `ProjectFiles.tsx`

### A. Nowy komponent `DirectoryNode` (inline lub w osobnym pliku)

Rekurencyjny komponent zastępujący obecny wzorzec renderowania `AccordionItem` per pakiet.

Props:
```typescript
interface DirectoryNodeProps {
  catalog: ProjectFilePackageWeb;
  depth: number;
  // ...wszystkie inne props potrzebne do renderowania plików, wersji, akcji
}
```

Każdy `DirectoryNode`:
- Renderuje `AccordionItem` z `ml={depth * 6}` (indentacja)
- W `AccordionButton`: ikona katalogu (FolderOpen/Folder), nazwa, badge totalFiles, przycisk "Dodaj podkatalog" (ze stopPropagation)
- W `AccordionPanel`: najpierw `subCatalogs.map(sub => <DirectoryNode depth+1>)`, potem pliki

### B. Zmiana `FilesTab` — używa `DirectoryNode`

Zamiast `packages.map(pkg => <AccordionItem>...)`:
```tsx
{packages.map(pkg => (
  <DirectoryNode key={pkg.id} catalog={pkg} depth={0} {...sharedProps} />
))}
```

API zwraca już tylko root nodes (ParentId=null) — nie trzeba filtrować.

### C. Nowy state i `useDisclosure` dla `CreateDirectoryModal`

```typescript
const { isOpen: isCreateDirOpen, onOpen: onCreateDirOpen, onClose: onCreateDirClose } = useDisclosure();
const [createDirParentId, setCreateDirParentId] = useState<string | undefined>(undefined);
```

### D. Nowy przycisk "Dodaj katalog" w nagłówku FilesTab

Obok istniejącego przycisku "Dodaj pliki" dodać:
```tsx
<Button
  leftIcon={<FolderPlus size={16} aria-hidden="true" />}
  onClick={() => {
    setCreateDirParentId(undefined); // nowy katalog główny
    onCreateDirOpen();
  }}
  variant="outline"
  size="sm"
>
  Dodaj katalog
</Button>
```

### E. Render `CreateDirectoryModal`

```tsx
{isCreateDirOpen && (
  <CreateDirectoryModal
    isOpen={isCreateDirOpen}
    onClose={onCreateDirClose}
    onSuccess={() => {
      onCreateDirClose();
      refreshData(); // invalidate queries
    }}
    tenantId={tenantId}
    projectId={projectId}
    catalogs={packages}
    defaultParentId={createDirParentId}
  />
)}
```

### F. Zmiana etykiet "paczka" → "katalog"

Przeszukaj cały plik i zamień:
- "paczek" → "katalogów"
- "Paczek" → "Katalogów"
- "paczka" → "katalog"
- "Paczka" → "Katalog"
- "paczkę" → "katalog"
- Zmienne JS/TS: `packages`, `pkg`, `packageId` — NIE zmieniaj (to kod wewnętrzny)
- Ikony: rozważ `FolderOpen`/`Folder` zamiast `FileText` dla catalog icon

### G. Zmiana w `ShareFilesModal` — cascade info

W `src/components/ShareFilesModal.tsx`:
- Zmienić tekst `Alert` info na: "Udostępniasz katalogi wraz z podkatalogami. Wybrani członkowie otrzymają dostęp do wszystkich plików i podkatalogów w zaznaczonych katalogach."
- Zmienić "paczek" / "paczka" → "katalogów" / "katalog"
- Dodać `aria-hidden="true"` do dekoracyjnych ikon

### H. Cascade checkboxes w ShareFilesModal (UI feedback)

Gdy użytkownik zaznacza checkbox katalogu, automatycznie zaznaczać checkboxy wszystkich jego podkatalogów.

Sprawdź jak `ShareFilesModal` zarządza listą zaznaczonych paczek. Dodać logikę:
```typescript
const toggleWithDescendants = (catalogId: string, catalogs: ProjectFilePackageWeb[]) => {
  // zebranie ID katalogu i wszystkich potomków
  const idsToToggle = collectAllIds(catalogId, catalogs);
  // toggle wszystkich naraz
  setSelectedIds(prev => {
    const isSelected = prev.has(catalogId);
    const next = new Set(prev);
    idsToToggle.forEach(id => isSelected ? next.delete(id) : next.add(id));
    return next;
  });
};
```

Przy "Zaznacz wszystko" — flatten całego drzewa.

## Dostępność WCAG AA
- `aria-hidden="true"` na ikonach dekoracyjnych (FolderOpen, FolderPlus)
- Nie zagnieżdżać `<button>` w `<button>` — stopPropagation + IconButton as="span" lub przeniesienie poza AccordionButton
- Indentacja wizualna podkatalogów: `ml` props lub `pl` — nie tylko `color` (dla daltonistów)

## Weryfikacja
```
npx tsc --noEmit
```
Brak błędów TypeScript.
