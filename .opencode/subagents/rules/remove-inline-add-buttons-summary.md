# Summary: Remove inline "Dodaj komponent / Dodaj opcję" row

## What was done
Usunięto duplikację przycisków "Dodaj opcję" i "Dodaj komponent" w widoku drzewa kosztorysu (TreeView).

## Problem
Przyciski pojawiały się w dwóch miejscach:
1. Kolumna Akcje (na hover wiersza) — jako GhostActionButton ✅
2. Osobny wiersz inline pod rozszerzoną pozycją — jako AddInlineButton ❌

## Change
Usunięto wiersz inline (blok `{/* Inline "add component / add option" row */}`) z pliku `TreeViewRow.tsx`.

## Files modified
- `01-Applications/ProjectDataManagementUI/src/components/CostEstimate/TreeView/TreeViewRow.tsx` — usunięto ~40 linii (wiersz inline)

## Files unchanged
- `01-Applications/ProjectDataManagementUI/src/components/CostEstimate/CardView/PositionCard.tsx` — już wcześniej działał poprawnie, bez zmian

## Verification
- `npm run build` — passed ✅
- Przyciski w kolumnie Akcje — nadal obecne ✅
- Brak osobnego wiersza inline z AddInlineButton ✅

## Blokery
Brak.
