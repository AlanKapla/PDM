# Feature: Filtrowanie pozycji głównych przy tworzeniu harmonogramu z kosztorysu

## Opis
Podczas synchronizacji harmonogramu z kosztorysu (w tym generowania z AI), tylko pozycje główne kosztorysu (`ItemRelationType = None`) powinny być brane pod uwagę jako elementy harmonogramu. Obecnie wszystkie pozycje oznaczone jako work scope (`ItemSystemIsWorkScope = true`) są dodawane do harmonogramu, niezależnie od ich `RelationType`. Powinny być dodawane tylko te, które dodatkowo mają `RelationType = None`.

## Domeny
- API (CQRS, Business/Services)

## Zmiany

### WorkScheduleSyncService
W metodzie `SyncWorksFromItemsAsync`, w miejscu filtrowania pozycji kosztorysu do harmonogramu, dodać warunek `i.RelationType == ItemRelationType.None`.

### Generowane testy
Testy jednostkowe dla `WorkScheduleSyncService` powinny być zaktualizowane (jeśli istnieją) — sprawdzić czy `RelationType != None` nie jest mapowany do pozycji harmonogramu.

## Nie zmienia się
- UI — nie wymaga zmian, backend zwróci odpowiednio przefiltrowane dane
- Kontrolery — nie wymagają zmian
- Handler CQRS — nie wymaga zmian

## Kryteria akceptacji
1. Pozycja kosztorysu z `RelationType = None` i `IsWorkScope = true` → trafia do harmonogramu
2. Pozycja kosztorysu z `RelationType = Option` i `IsWorkScope = true` → NIE trafia do harmonogramu
3. Pozycja kosztorysu z `RelationType = Component` i `IsWorkScope = true` → NIE trafia do harmonogramu
4. Pozycja kosztorysu z `RelationType = None` i `IsWorkScope = false` → NIE trafia do harmonogramu
5. Pozycje już istniejące w harmonogramie z `RelationType != None` są usuwane (soft delete) podczas resync
