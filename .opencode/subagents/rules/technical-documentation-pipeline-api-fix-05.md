# api-fix-05 — Faza 3: Grouping (DrawingThematicGroupResolver)

## Cel i zakres

Zaimplementować `DrawingThematicGroupResolver` z 7+1 grupami tematycznymi i dual membership K-06 (`reinforcement` + `foundations`).

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `DrawingThematicGroupResolver.cs` | **NOWY** |
| `ThematicDrawingGroup.cs` | **NOWY** model wewnętrzny |
| `DrawingThematicGroupResolverTests.cs` | **NOWY** — K-06 dual, mapowanie drawingType |
| `TechnicalDocumentationOptions.cs` | Użycie `DrawingTypeToThematicGroups` |

## Wymagania techniczne

- Skills: `api-services`, `api-unit-tests`
- Grupy: `reinforcement`, `roof_structure`, `floor_plans`, `sections`, `elevations`, `foundations`, `site`, `other`
- K-06 (`detale_konstrukcyjne`): ten sam obraz w `reinforcement` (schemat k06) i `foundations` (k06_foundations)
- Zastępuje logikę `MaterialDrawingGroupResolver` w group pipeline (legacy zostaje)

## Kryteria akceptacji

- [ ] Test: K-06 → 2 grupy z tym samym imageId
- [ ] Test: każdy drawingType z planu mapuje się na ≥1 grupę
- [ ] `dotnet test` — nowe testy green

## Zależności

- Po: **api-fix-01**, **api-fix-04**
- Przed: **api-fix-06**
