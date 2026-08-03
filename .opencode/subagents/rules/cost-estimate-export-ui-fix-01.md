# cost-estimate-export-ui-fix-01 — Typy, downloadBlob, costEstimateApi, mock

## Kontekst

- Feature: `.opencode/features/cost-estimate-export.md`
- Audyt: `.opencode/subagents/rules/cost-estimate-export-ui-audit.md`
- Skills: `.opencode/skills/ui-api-client/SKILL.md`, `ui-types/SKILL.md`
- API (wymagane lub mock): `GET .../cost-estimate/{id}/export/xlsx|pdf`

## Cel

Warstwa kontraktu UI do pobierania plików binarnych (pierwszy wzorzec axios blob w projekcie).

## Zadania

1. Typy w `src/types/costEstimate.types.new.ts` (lub osobny mały plik jeśli czytelniej):
   - `CostEstimateExportFormat = 'pdf' | 'xlsx'`
   - `CostEstimateExportFile = { blob: Blob; fileName: string; contentType: string }`

2. Utwórz `src/utils/downloadBlob.ts`:
   - `parseContentDispositionFileName(header: string | undefined): string | null`
   - `downloadBlob(blob: Blob, fileName: string): void` — createObjectURL → `<a download>` → revoke
   - Bez `any`

3. W `costEstimateApi.ts` dodaj:
```ts
exportXlsx(tenantId, projectId, id): Promise<CostEstimateExportFile>
exportPdf(tenantId, projectId, id): Promise<CostEstimateExportFile>
```
   - `axiosClient.get(..., { responseType: 'blob' })`
   - Wyciągnij `content-disposition` z headers
   - Fallback fileName: `kosztorys_{id}.{ext}` (page nadpisze nazwą z details jeśli chce)
   - **Obsługa błędów:** jeśli `response.status >= 400` i data jest Blob — `await data.text()` → JSON → rzuć/parsuj jak reszta API (`handleApiError` / axios interceptor). Dostosuj lokalnie w metodach export jeśli interceptor nie obsługuje blob.

4. Mock demo:
   - W `mockHandlers.ts` dopasuj ścieżki `.../export/xlsx` i `.../export/pdf`
   - Zwróć niepusty `Blob` + nagłówek Content-Disposition
   - Rozszerz `applyMockAdapter` / `index.ts` jeśli dziś wymusza zawsze `application/json` — pozwól na binary response dla tych ścieżek

5. Opcjonalnie: dłuższy timeout tylko dla export (np. 120s) jeśli axios ma niski default.

## Poza zakresem

- Toolbar / EditPage (fix-02)

## Kryteria done

- [ ] Metody API typowane, bez `any`
- [ ] downloadBlob działa w izolacji (można unit-testować parse CD)
- [ ] Demo mode nie crashuje na ścieżce export (jeśli mock dodany)
