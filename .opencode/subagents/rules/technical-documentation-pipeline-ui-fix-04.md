# ui-fix-04 — Rozszerzenie ProjectModelSection (slab, elevations, warnings, metadata)

## Cel i zakres

Uzupełnić `TechnicalDocumentationProjectModelSection` o panele spec §8.1: `slab`, `elevations`, `warnings[]`, `extractionMetadata`. Opcjonalna sekcja metadanych pipeline.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `TechnicalDocumentationProjectModelSection.tsx` | Nowe AccordionItem |
| `TechnicalDocumentationExtractionMetadataSection.tsx` | **NOWY** (collapsible, dev-friendly) |

## Wymagania techniczne

- Skills: `ui-components`, `ui-theme`
- `slab` — tabela: beton, grubość, stal dolna/górna (analogicznie do CeilingsTable)
- `elevations` — tabela: orientacja, okna/drzwi, wykończenie (pola z `ProjectModelElevationWeb`)
- `warnings` — lista z Badge severity (`warning`/`error` colorScheme)
- `extractionMetadata` — SimpleGrid: pipelineVersion, processedAt, tokenUsage, lista grup
- Zachować `ceilings` panel dla backward compat (legacy projectModel z PDM)
- Mapowanie `conflicts`/`missingData` → wyświetl w panelu warnings jeśli `warnings[]` puste (legacy model)

## Kryteria akceptacji

- [ ] Panele renderują się tylko gdy dane present (`hasContent` rozszerzone)
- [ ] AXE: brak nowych naruszeń (uruchomić DetailsView test z rozszerzonym mockiem)
- [ ] Kontrast badge warnings ≥ 4.5:1
- [ ] `npm run build` OK

## Zależności

- Po: **ui-fix-02**
- Może być równolegle z **ui-fix-03**
