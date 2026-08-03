# cost-estimate-export-api-fix-03 — QuestPDF (PDF)

## Kontekst

- Feature: `.opencode/features/cost-estimate-export.md`
- Audyt: `.opencode/subagents/rules/cost-estimate-export-api-audit.md`
- Wymaga: `cost-estimate-export-api-fix-01` (modele + flatten); idealnie po fix-02 (ten sam Export entrypoint)
- Skills: `.opencode/skills/api-services/SKILL.md`

## Cel

Zaimplementować generowanie PDF przez QuestPDF.

## Zadania

1. Skonfiguruj QuestPDF zgodnie z licencją Community (ustawienie licencji w static ctor / startup serwisu — sprawdź docs aktualnej wersji).

2. `BuildPdf(...)`:
   - Strona A4 landscape **lub** portrait — wybierz landscape jeśli dużo kolumn additional fields; w przeciwnym razie portrait
   - Nagłówek: nazwa kosztorysu, waluta, data eksportu, TotalNet / TotalGross / TotalVat
   - Tabela: kolumny kluczowe (typ wiersza lub wcięcie, nazwa, ilość, jm, ceny, wartości, zaznaczono) + additional (skrócone nagłówki jeśli długie)
   - Hierarchia: wcięcie tekstu wg `Level` / RowType
   - Stopka: numer strony
   - Content-Type: `application/pdf`
   - Bez logo tenanta (spec v1)

3. Podłącz w `Export(..., Pdf)`.

4. Test smoke: niepusty PDF (`%PDF` magic lub Length > 100).

## Poza zakresem

- Branding / logo
- Osadzanie załączników
- Controller (fix-04)

## Kryteria done

- [ ] PDF generuje się bez wyjątku dla typowego drzewa testowego
- [ ] Nagłówek zawiera sumy z meta
- [ ] Build OK
