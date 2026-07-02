# api-fix-08 — Faza 6: Consolidation (LLM text-only)

## Cel i zakres

Centralna faza Consolidation — merge wyników 7+1 grup w `ProjectModel` (spec §8.1). Text-only LLM. `ProjectModelFallbackBuilder` jako safety net. Wchłania CrossReference/SharedState/Rooms/Openings.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `Pipeline/ConsolidationPipelineAgent.cs` | **NOWY** |
| `ConsolidationAgentService.cs` | **NOWY** |
| `consolidation_agent.md` | **NOWY** prompt |
| `ProjectModelFallbackBuilder.cs` | Wywołanie gdy LLM fail |
| Testy consolidation / fallback | **NOWY** |

## Wymagania techniczne

- Skills: `api-services`, `api-unit-tests`
- Wejście: zweryfikowane JSON per grupa (text)
- Wyjście: `ProjectModel` w context (bez legacy summaries)
- **Bez obrazów** — tylko `CompleteAsync` / text completion
- Usunąć zależność od `CrossReferencePipelineAgent`, `RoomsPipelineAgent`, `OpeningsPipelineAgent` w group runner

## Kryteria akceptacji

- [ ] Consolidation produkuje `ProjectModel` z floors, walls, foundations, site, roof
- [ ] Fallback builder używany przy wyjątku LLM
- [ ] Test: partial group results → sensowny fallback model
- [ ] `dotnet build` OK

## Zależności

- Po: **api-fix-07**, **api-fix-13** (model §8.1 — **musi być gotowy wcześniej**)
- Przed: **api-fix-09**
