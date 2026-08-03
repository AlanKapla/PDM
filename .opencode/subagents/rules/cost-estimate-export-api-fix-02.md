# cost-estimate-export-api-fix-02 — ClosedXML (XLSX)

## Kontekst

- Feature: `.opencode/features/cost-estimate-export.md`
- Audyt: `.opencode/subagents/rules/cost-estimate-export-api-audit.md`
- Wymaga: `cost-estimate-export-api-fix-01` (modele + flatten)
- Skills: `.opencode/skills/api-services/SKILL.md`

## Cel

Zaimplementować generowanie pliku XLSX przez ClosedXML na podstawie `CostEstimateExportRow[]` + meta.

## Zadania

1. W `CostEstimateExportService` zaimplementuj `BuildXlsx(...)`:
   - Arkusz **„Podsumowanie”** (lub sekcja na górze arkusza głównego): nazwa kosztorysu, waluta, TotalNet/Gross/Vat, data eksportu
   - Arkusz **„Kosztorys”**: nagłówki kolumn stałych + dynamiczne additional fields
   - Wiersze z flatten; dla Group — pogrubienie / wcięcie (kolumna Level lub prefix spacji w Name)
   - IsSelected jako „Tak”/„Nie”
   - Liczby: kultura **pl-PL** lub wartości decimal w komórkach numerycznych (preferuj typy liczbowe w Excel, format `#,##0.00`)
   - Content-Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

2. Podłącz w `Export(..., Xlsx)` → `CostEstimateExportFile`.

3. Test smoke: `Export(..., Xlsx)` zwraca `Content.Length > 0` i poprawne rozszerzenie w `FileName`.

## Poza zakresem

- PDF (fix-03)
- Endpoint HTTP (fix-04)

## Kryteria done

- [ ] XLSX otwieralny (smoke test niepusty)
- [ ] Kolumny additional fields obecne gdy schema niepusta
- [ ] Build OK
