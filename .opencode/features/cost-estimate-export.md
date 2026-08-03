# Feature: Eksport kosztorysu (Cost Estimate) do PDF i XLSX

## Typ zmiany

**Full-stack** — API (ClosedXML + QuestPDF, CQRS Query → FileResult) + UI (menu „Akcje” na stronie edycji kosztorysu)

## Cel

Użytkownik z poziomu edycji kosztorysu (`CostEstimateEditPage`) może pobrać kompletny kosztorys jako plik **PDF** lub **XLSX**. Generowanie plików odbywa się **wyłącznie po stronie serwera**.

## Zatwierdzone decyzje produktowe

| Decyzja | Wartość |
|--------|---------|
| Domena | Tylko **Cost Estimate** (nie Cost Tracker) |
| Formaty | PDF **i** XLSX w v1 |
| Architektura | Server-side (opcja B) |
| Biblioteki | **ClosedXML** (XLSX), **QuestPDF** (PDF) |
| Wejście UI | Menu **„Akcje”** w `CostEstimateToolbar` |
| Migracje DB | Brak |

## Decyzje domyślne v1 (nie doprecyzowane przez użytkownika)

| Temat | Default v1 | Uzasadnienie |
|-------|------------|--------------|
| Zawartość | Pełne drzewo: grupy → pozycje główne → opcje → komponenty | Odpowiada modelu domenowemu i widokowi Tree |
| `IsSelected` | Eksportuj **wszystkie** wiersze; kolumna/oznaczenie „Zaznaczono”; sumy w nagłówku = `TotalNet/Gross/Vat` z encji (jak w UI) | Transparentność oferty; sumy spójne z API |
| Pola dodatkowe | **Tak** — kolumny dynamiczne wg `AdditionalFields` (Order) | Często kluczowe w kosztorysach budowlanych |
| Załączniki pozycji | **Nie** (tylko nazwy/liczba opcjonalnie w przyszłości) | Poza zakresem v1 |
| Liczby / kultura | Format **pl-PL** (`1 234,56`); waluta z `SelectedCurrencyCode` / Symbol | Aplikacja PL |
| Nazwa pliku | `{SanitizedName}_{yyyyMMdd}.{pdf\|xlsx}` | Czytelne w downloadach |
| Uprawnienia | Jak odczyt details: `PROJECT.ESTIMATES` + `AccessLevel != None` | Shared też może eksportować |
| Opcje UI (modal) | **Brak** w v1 — jeden klik = pełny eksport | Prostszy UX; opcje później |
| Limity rozmiaru | Synchroniczny FileResult; soft-warn w logach jeśli > ~5k wierszy; bez async job w v1 | Wystarczy na typowe kosztorysy |
| Branding PDF | Nagłówek: nazwa kosztorysu, projekt (jeśli łatwo), data eksportu, waluta; bez logo tenanta | Logo = follow-up |

---

## Zakres

### API

- NuGet: `ClosedXML`, `QuestPDF` w projekcie `Business` (lub osobny warstwa jeśli skills wymagają — preferuj Business + DI)
- `ICostEstimateExportService` — wspólny flatten hierarchii → wiersze eksportu
- `CostEstimateExcelExporter` / `CostEstimatePdfExporter` (lub metody w jednym serwisie)
- CQRS:
  - `ExportCostEstimateQuery` (lub dwa query: Pdf / Xlsx) → `CostEstimateExportFileResult` (bytes, contentType, fileName)
- Endpoint(y) w `CostEstimateController`:
  - `GET .../cost-estimate/{id}/export/xlsx`
  - `GET .../cost-estimate/{id}/export/pdf`
- Auth: `PermissionCodes.ProjectEstimates` + access check jak `GetCostEstimateDetails`
- Reuse: `ICostEstimateCacheService`, `ICostEstimateAccessService`, additional fields, calculation totals
- Testy: handler (auth/not found), flatten unit tests, smoke exporter (niepusty plik)

### UI

- `CostEstimateToolbar`: pozycje menu „Eksportuj do Excel”, „Eksportuj do PDF”
- `CostEstimateEditPage`: handlery download (loading per format, toast błędu)
- `costEstimateApi.exportXlsx` / `exportPdf` — `responseType: 'blob'`, nazwa z `Content-Disposition` lub fallback
- Helper pobierania bloba (jeśli nie istnieje wspólny)
- a11y: `aria-label`, disabled podczas pobierania

### Poza zakresem (v1)

- Cost Tracker / dashboard budżetu
- Import zwrotny XLSX
- Załączniki w ZIP / osadzone w PDF
- Logo tenanta / szablony firmowe
- Modal opcji eksportu (filtry IsSelected, wybór kolumn)
- Async job / SignalR progress dla ogromnych plików
- Eksport z listy kosztorysów (tylko ze strony szczegółów/edycji)

---

## Przepływ danych

```
UI: Akcje → Eksportuj PDF|XLSX
        │
        ▼
GET /api/tenants/{t}/projects/{p}/cost-estimate/{id}/export/{pdf|xlsx}
        │
        ├─ AuthorizationBehavior (PROJECT.ESTIMATES)
        ├─ AccessLevel != None (Forbidden jeśli None)
        ├─ Cache: CostEstimate + Groups + Items + AdditionalFields
        │
        ▼
ICostEstimateExportService
        ├─ Flatten tree → ExportRow[]
        ├─ format=xlsx → ClosedXML → byte[]
        └─ format=pdf  → QuestPDF → byte[]
        │
        ▼
File(contentType, fileName) → browser download
```

---

## Model wiersza eksportu (logical)

| Kolumna (logiczna) | Opis |
|--------------------|------|
| Level / Path | Wcięcie lub ścieżka grupy |
| RowType | Group / Item / Option / Component |
| Name | Nazwa |
| Quantity, Unit | Pozycje |
| UnitPriceNet, VatRate, UnitPriceGross | Pozycje |
| NetValue, VatValue, GrossValue | Pozycje / sumy grup |
| IsSelected | Tak/Nie |
| AdditionalFields… | Dynamicznie, Order |

PDF: tabela z hierarchicznym wcięciem + stopka ze stronami.  
XLSX: jeden arkusz „Kosztorys” (+ opcjonalnie arkusz „Podsumowanie” z metadanymi).

---

## Endpointy

```
GET /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{id}/export/xlsx
GET /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{id}/export/pdf

→ 200 application/vnd.openxmlformats-officedocument.spreadsheetml.sheet | application/pdf
→ Content-Disposition: attachment; filename="..."
→ 404 NotFound, 403 Forbidden
```

---

## Kryteria akceptacji

- [ ] Z menu „Akcje” na stronie edycji kosztorysu można pobrać XLSX
- [ ] Z tego samego menu można pobrać PDF
- [ ] Pliki zawierają hierarchię grup i pozycji (w tym opcje/komponenty)
- [ ] Sumy w nagłówku/podsumowaniu zgodne z `TotalNet/Gross/Vat` z details
- [ ] Pola dodatkowe obecne jako kolumny (jeśli zdefiniowane)
- [ ] Użytkownik z dostępem Shared (nie Full) może eksportować
- [ ] Brak dostępu → 403; brak kosztorysu → 404
- [ ] Nazwa pliku zawiera nazwę kosztorysu + datę
- [ ] Brak regresji edycji kosztorysu / trackerów
- [ ] Testy jednostkowe flatten + smoke eksportu

---

## Plan implementacji (prompty)

### API (kolejność)

1. `cost-estimate-export-api-fix-01` — NuGet + interfejsy + model ExportRow + flatten
2. `cost-estimate-export-api-fix-02` — ClosedXML exporter
3. `cost-estimate-export-api-fix-03` — QuestPDF exporter
4. `cost-estimate-export-api-fix-04` — CQRS Query/Handler/Validator + Controller + DI
5. `cost-estimate-export-api-fix-05` — Testy jednostkowe

### UI (kolejność; po API lub z mockiem)

1. `cost-estimate-export-ui-fix-01` — API client blob download
2. `cost-estimate-export-ui-fix-02` — Toolbar + page handlers + a11y

---

## Skills do przeczytania przed implementacją

- `.opencode/skills/api-cqrs/SKILL.md`
- `.opencode/skills/api-controllers/SKILL.md`
- `.opencode/skills/api-services/SKILL.md`
- `.opencode/skills/api-unit-tests/SKILL.md`
- `.opencode/skills/ui-components/SKILL.md`
- `.opencode/skills/ui-api-client/SKILL.md`
- `.opencode/skills/ui-hooks/SKILL.md`
- `.opencode/skills/ui-accessibility/SKILL.md`
