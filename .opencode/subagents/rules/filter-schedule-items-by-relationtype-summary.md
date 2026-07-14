# Feature wdrożony: filter-schedule-items-by-relationtype

**Data:** 2026-06-10

## Co zostało zrobione

Wprowadzono filtrowanie po `ItemRelationType.None` podczas synchronizacji harmonogramu z kosztorysu. Od teraz tylko pozycje główne kosztorysu (z `RelationType = None`) trafiają do harmonogramu jako elementy prac (`WorkScheduleStageWork`). Pozycje typu `Option` i `Component` są pomijane, nawet jeśli mają ustawioną flagę `IsWorkScope = true`.

## Zmodyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `src/Business/Implementation/Services/WorkScheduleSyncService.cs` | Dodano `&& item.RelationType == ItemRelationType.None` w metodzie `IsWorkScopeItem` |
| `tests/Business.Tests/Services/WorkScheduleSyncServiceTests.cs` | Dodano 6 nowych testów jednostkowych pokrywających filtrowanie |

## Kryteria akceptacji — pokrycie

| # | Kryterium | Status |
|---|-----------|--------|
| 1 | Pozycja z `RelationType=None` i `IsWorkScope=true` → trafia do harmonogramu | ✅ test #1 |
| 2 | Pozycja z `RelationType=Option` i `IsWorkScope=true` → NIE trafia | ✅ test #2 |
| 3 | Pozycja z `RelationType=Component` i `IsWorkScope=true` → NIE trafia | ✅ test #3 |
| 4 | Pozycja z `RelationType=None` i `IsWorkScope=false` → NIE trafia | ✅ test #4 |
| 5 | Istniejące pozycje z `RelationType!=None` usuwane (soft delete) przy resecie | ✅ test #5 |

## Nowe testy (6)

1. `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeNone_CreatesWork`
2. `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeOption_SkipsWork`
3. `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeComponent_SkipsWork`
4. `SyncFromCostEstimateAsync_NonWorkScopeItemWithRelationTypeNone_SkipsWork`
5. `SyncFromCostEstimateAsync_ExistingWorkForNonMainItem_SoftDeletedOnResync`
6. `SyncFromCostEstimateAsync_OnlyNonMainItems_NoWorksCreated`

## Blokery

Brak.

## Następne kroki

Po zdeployowaniu zmiany, przy pierwszej synchronizacji (resync) istniejące niechciane pozycje harmonogramu (utworzone wcześniej z `RelationType != None`) zostaną automatycznie soft-deleted. Nie jest wymagana migracja DB.
