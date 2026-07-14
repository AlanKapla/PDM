# ce-ui-fix-02 — Kolumny Sumuj/IsSelected, Zakres harmonogramu, Plik + widoczność + ghost row

## Cel
Dodać brakujące kolumny: Sumuj (IsSelected), Zakres harmonogramu (IsStageWork), Plik.
Przenieść IsSelected i IsStageWork z kolumny Nazwa do dedykowanych kolumn.
Naprawić ghost row — dodać "Dodaj podetap", usunąć "Dodaj opcję/komponent" z ghost row.
Dodać persystowanie widoczności kolumn w sessionStorage.

## Przeczytaj skill przed implementacją
`.github/skills/ui-components/SKILL.md`

---

## Plik: `src/components/CostEstimate/TreeView/CostEstimateTreeView.tsx`

### 1. Dodaj kolumny do BASE_COLUMNS

Po kolumnie `grossValue` dodaj:
```typescript
{
  id: 'vatValue',
  label: 'Wartość VAT',
  fieldType: 'numeric',
  appliesTo: ['item'],
  width: '110px',
  isSortable: false,
  textAlign: 'right' as const,
},
{
  id: 'isSelected',
  label: 'Sumuj',
  fieldType: 'boolean',
  appliesTo: ['item'],
  width: '65px',
  isSortable: false,
  textAlign: 'center' as const,
},
{
  id: 'isStageWork',
  label: 'Zakres\nharmon.',
  fieldType: 'boolean',
  appliesTo: ['item'],
  width: '75px',
  isSortable: false,
  textAlign: 'center' as const,
},
{
  id: 'files',
  label: 'Plik',
  fieldType: 'string',
  appliesTo: ['item'],
  width: '55px',
  isSortable: false,
  textAlign: 'center' as const,
},
```

### 2. Widoczność kolumn — sessionStorage

Dodaj interface i hook:

```typescript
const VISIBLE_COLS_KEY = (userId: string) => `ce-visible-cols-${userId}`;

function loadVisibleCols(userId: string, allColIds: string[]): Set<string> {
  try {
    const raw = sessionStorage.getItem(VISIBLE_COLS_KEY(userId));
    if (raw) {
      const parsed: string[] = JSON.parse(raw);
      return new Set(parsed.filter((id) => allColIds.includes(id)));
    }
  } catch {}
  return new Set(allColIds); // domyślnie wszystkie widoczne
}

function saveVisibleCols(userId: string, cols: Set<string>): void {
  try {
    sessionStorage.setItem(VISIBLE_COLS_KEY(userId), JSON.stringify([...cols]));
  } catch {}
}
```

W komponencie:
- Pobierz `userId` z kontekstu (MSAL lub aplikacji)
- Stan: `const [visibleColIds, setVisibleColIds] = useState(() => loadVisibleCols(userId, allColIds))`
- `handleToggleFieldVisibility` uaktualnia stan i wywołuje `saveVisibleCols`
- Przefiltruj `baseColumns` i `additionalColumns` przez `visibleColIds` przed renderowaniem

### 3. Przekazywanie do TreeViewRow

Dodaj `visibleColIds: Set<string>` do `CostEstimateTreeViewProps` i do `TreeViewRowProps`. Filtruj widoczne kolumny przy budowaniu `baseColumns` i `additionalColumns` przekazywanych do TreeViewRow.

---

## Plik: `src/components/CostEstimate/TreeView/TreeViewRow.tsx`

### 1. Usuń IsSelected i IsStageWork z kolumny Nazwa

W sekcji "Name column" w `ItemRow` — usuń `<Checkbox isChecked={item.isSelected} ...>` i `<Checkbox isChecked={item.isStageWork} ...>` i przycisk radio dla opcji. Zostaw tylko pole `<PrototypeTextInput value={item.name} ...>` i przycisk pliku.

### 2. Renderuj IsSelected w dedykowanej kolumnie

W `renderBaseFieldCells`, dodaj case dla `col.id === 'isSelected'`:
```tsx
if (col.id === 'isSelected') {
  // Opcja → radio button; Pozycja/Komponent → checkbox
  if (isOption) {
    return (
      <Flex key="isSelected" flex="0 0 auto" w={w} justify="center" align="center">
        <Box
          as="button"
          w="16px" h="16px"
          borderRadius="50%"
          border="2px solid"
          borderColor={item.isSelected ? 'primary.500' : 'neutral.300'}
          bg={item.isSelected ? 'primary.500' : 'white'}
          display="flex" alignItems="center" justifyContent="center"
          onClick={() => onSelectOption(item.id)}
          disabled={!isEditMode}
          aria-label="Wybierz opcję"
          _hover={{ borderColor: 'primary.500' }}
          flexShrink={0}
        >
          {item.isSelected && <Box w="6px" h="6px" borderRadius="50%" bg="white" />}
        </Box>
      </Flex>
    );
  }
  return (
    <Flex key="isSelected" flex="0 0 auto" w={w} justify="center" align="center">
      <Checkbox
        isChecked={item.isSelected}
        onChange={(e) => {
          const v = e.target.checked;
          onFieldChange(groupId, item.id, 'isSelected', v);
          triggerBaseAutosave('isSelected', 'boolean', v ? 'true' : 'false');
        }}
        isDisabled={!isEditMode}
        colorScheme="primary"
        size="sm"
        aria-label="Sumuj"
      />
    </Flex>
  );
}
```

### 3. Renderuj IsStageWork w dedykowanej kolumnie

```tsx
if (col.id === 'isStageWork') {
  // Tylko dla pozycji głównych (nie opcja, nie komponent)
  if (isOption || isComponent) {
    return <EmptyCell key="isStageWork" width={w} />;
  }
  return (
    <Flex key="isStageWork" flex="0 0 auto" w={w} justify="center" align="center">
      <Checkbox
        isChecked={item.isStageWork}
        onChange={(e) => {
          const v = e.target.checked;
          onFieldChange(groupId, item.id, 'isStageWork', v);
          triggerBaseAutosave('isStageWork', 'boolean', v ? 'true' : 'false');
        }}
        isDisabled={!isEditMode}
        colorScheme="orange"
        size="sm"
        aria-label="Zakres pracy harmonogramu"
      />
    </Flex>
  );
}
```

### 4. Renderuj Files w dedykowanej kolumnie

```tsx
if (col.id === 'files') {
  return (
    <Flex key="files" flex="0 0 auto" w={w} justify="center" align="center">
      <Tooltip label={hasFiles ? `${item.files?.length} plik(ów)` : 'Dodaj plik'}>
        <IconButton
          aria-label="Pliki"
          icon={hasFiles ? <FileText size={13} /> : <Upload size={13} />}
          size="xs"
          variant="ghost"
          colorScheme={hasFiles ? 'primary' : 'gray'}
          onClick={onUploadFiles}
          opacity={hasFiles ? 1 : 0.4}
          _hover={{ opacity: 1 }}
        />
      </Tooltip>
    </Flex>
  );
}
```

### 5. Usuń przycisk pliku z kolumny Nazwa

W `ItemRow`, z sekcji nazwy usuń IconButton dla pliku (teraz jest w dedykowanej kolumnie).

### 6. Napraw ghost row — dodaj "Dodaj podetap"

W `TreeViewRow`, sekcja `{/* Inline "add item" row */}`:

```tsx
{isEditMode && (
  <Flex
    align="center"
    minH="46px"
    borderBottom={isLast ? 'none' : '1px solid'}
    borderColor="neutral.100"
    px={3.5}
    py={2}
    role="row"
    _hover={{ bg: 'neutral.25' }}
  >
    <Flex flex={1} minW="270px" align="center" gap={2} pl={`${(level + 1) * 28}px`}>
      <AddInlineButton onClick={onAddItem}>Dodaj pozycję</AddInlineButton>
      {level < 2 && (
        <AddInlineButton onClick={onAddSubGroup}>Dodaj podetap</AddInlineButton>
      )}
    </Flex>
    <Flex w="120px" justify="flex-end" gap={0.5} />
  </Flex>
)}
```

### 7. Akcje w kolumnie akcji — zawsze widoczne

Zmień: usuń `{isEditMode && (...)}` z sekcji Actions w ItemRow i GroupRow. Przyciski Akcji mają być zawsze widoczne dla każdego poziomu. Wyłącz (isDisabled) przyciski modyfikujące gdy `!isEditMode`.

```tsx
<Flex w="120px" justify="flex-end" gap={0.5}>
  {/* Zawsze widoczne — disabled gdy nie w trybie edycji */}
  {!isComponent && !isOption && !hasOptions && (
    <GhostActionButton
      label="Dodaj komponent"
      icon={<Plus size={15} />}
      variant="add"
      onClick={onAddComponent}
      isDisabled={!isEditMode}
    />
  )}
  {!isOption && !hasComponents && (
    <GhostActionButton
      label="Dodaj opcję"
      icon={<Plus size={15} />}
      variant="add"
      onClick={onAddOption}
      isDisabled={!isEditMode}
    />
  )}
  <GhostActionButton
    label="Usuń"
    icon={<Trash2 size={15} />}
    variant="delete"
    onClick={onDeleteItem}
    isDisabled={!isEditMode}
  />
</Flex>
```

Analogicznie dla GroupRow.

## Plik: `src/components/CostEstimate/PrototypeActionButtons.tsx`

Upewnij się że `GhostActionButton` obsługuje prop `isDisabled?: boolean` i stosuje odpowiedni styl opacity gdy disabled.

---

## Weryfikacja

1. Kolumny Sumuj, Zakres harmonogramu, Plik widoczne w nagłówku
2. Checkbox Sumuj w każdej pozycji/komponencie; radio dla opcji
3. Checkbox Zakres harmonogramu tylko w pozycjach głównych
4. Plik — ikona upload/file w każdej pozycji
5. Ghost row ma "Dodaj pozycję" + "Dodaj podetap"
6. Akcje widoczne zawsze (disabled gdy view mode)
7. Widoczność kolumn zapisywana po odświeżeniu strony (sessionStorage)
