# api-fix-06 — Faza 4: Group Extraction A/B

## Cel i zakres

Nowa faza ekstrakcji per grupa tematyczna: równoległe call'e Agent A i B z multi-image (`CompleteWithImagesAsync`), sub-batch po 6 obrazów, merge JSON w C#.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `Pipeline/GroupExtractionPipelineAgent.cs` | **NOWY** |
| `GroupExtractionAgentService.cs` | **NOWY** — A/B parallel |
| `GroupExtractionJsonMerger.cs` | **NOWY** — sub-batch merge |
| `Resources/.../group_extraction_*_a.md`, `*_b.md` | **NOWY** per grupa |
| `Resources/.../schemas/k06.json`, `k06_foundations.json` | **NOWY** |
| Testy merge + pipeline | **NOWY** |

## Wymagania techniczne

- Skills: `api-services`, `api-unit-tests`
- Max 6 obrazów per call; chunk >6 → osobne call'e A/B → `GroupExtractionJsonMerger.Merge`
- K-06: **2 osobne call'e** per grupa (reinforcement vs foundations) — decyzja zatwierdzona
- `jsonMode: true` dla ekstrakcji
- Prompty per grupa tematyczna (nie per drawingType)

## Kryteria akceptacji

- [ ] Grupa z 8 obrazami → 2 sub-batch'e, merge przed Verification
- [ ] K-06 generuje wyniki w obu grupach
- [ ] Test jednostkowy merge overlapping keys
- [ ] `dotnet build` OK

## Zależności

- Po: **api-fix-02**, **api-fix-05**
- Przed: **api-fix-07**
