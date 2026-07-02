# api-fix-07 — Faza 5: Verification (DiffEngine + Agent C)

## Cel i zakres

`ExtractionDiffEngine` — diff A vs B per grupa z detekcją critical fields. `ExtractionVerificationAgentService` (Agent C) — **zawsze** przy critical diff (vision). Nowy `VerificationPipelineAgent`.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `ExtractionDiffEngine.cs` | **NOWY** |
| `ExtractionVerificationAgentService.cs` | **NOWY** — Agent C |
| `Pipeline/VerificationPipelineAgent.cs` | **NOWY** |
| `extraction_verification_agent.md` | **NOWY** prompt |
| `ComparatorAgentService.cs` | Refaktor lub delegacja |
| `ExtractionDiffEngineTests.cs` | **NOWY** |

## Wymagania techniczne

- Skills: `api-services`, `api-unit-tests`
- Critical fields (propozycja — potwierdzić w implementacji): `totalMassKg`, `totalVolumeM3`, `areaM2`, `concreteClass`, `reinforcement`
- Agent C: `CompleteWithImagesAsync` z obrazami grupy
- OUT: zweryfikowany JSON per grupa + wpisy do `warnings[]`

## Kryteria akceptacji

- [ ] Critical diff → Agent C wywołany (test mock)
- [ ] Non-critical diff → merge deterministyczny bez Agent C
- [ ] Testy DiffEngine dla pól krytycznych
- [ ] `dotnet test Business.Tests` — green

## Zależności

- Po: **api-fix-02**, **api-fix-06**
- Przed: **api-fix-08**
