# Remove inline "Dodaj komponent / Dodaj opcję" row

## File to modify
`01-Applications/ProjectDataManagementUI/src/components/CostEstimate/TreeView/TreeViewRow.tsx`

## Change description
Usuń wiersz inline z przyciskami "Dodaj komponent" i "Dodaj opcję" znajdujący się pod rozszerzoną zawartością pozycji (ItemRow).

Te przyciski są już dostępne w kolumnie Akcje (jako GhostActionButton), więc duplikacja w osobnym wierszu jest zbędna.

## What to remove
Usuń blok kodu znajdujący się po sekcji Options (po linijce zawierającej `</Box>` zamykający `{hasOptions && (...)})`, a przed `</Box>` zamykającym główny `ItemRow`:

```tsx
      {/* Inline "add component / add option" row */}
      {isEditMode && !isOption && (
        <Flex
          align="center"
          minH="40px"
          minW={`${totalColumnsWidth}px`}
          borderBottom={isLast ? 'none' : '1px solid'}
          borderColor="neutral.100"
          px={3.5}
          py={1.5}
          role="row"
          _hover={{ bg: 'neutral.25' }}
        >
          <Flex
            flex="0 0 auto"
            w="270px"
            position="sticky"
            left={0}
            zIndex={2}
            bg="white"
            align="center"
            gap={2}
            pl={`${(level + 1) * 28}px`}
          >
            {isComponent ? (
              <AddInlineButton onClick={onAddOption}>Dodaj opcję</AddInlineButton>
            ) : (
              <>
                {!hasOptions && !hasComponents && (
                  <AddInlineButton onClick={onAddComponent}>Dodaj komponent</AddInlineButton>
                )}
                {!hasComponents && (
                  <AddInlineButton onClick={onAddOption}>Dodaj opcję</AddInlineButton>
                )}
              </>
            )}
          </Flex>
        </Flex>
      )}
```

## What to keep
Przyciski w kolumnie Akcje (sekcja `{/* Actions */}` z GhostActionButton) pozostają bez zmian — to one są docelowym jedynym miejscem dostępu do funkcji "Dodaj komponent" i "Dodaj opcję".

## Verification
1. Po zmianie plik powinien nadal się kompilować (`npm run build` lub sprawdzenie TypeScript)
2. Przyciski "Dodaj komponent" i "Dodaj opcję" powinny być widoczne tylko w kolumnie Akcje na hover wiersza
3. Brak osobnego wiersza inline pod pozycją z tymi przyciskami
