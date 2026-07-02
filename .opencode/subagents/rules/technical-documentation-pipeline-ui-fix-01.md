# ui-fix-01 — CompletedWithWarnings (status UI + SignalR)

## Cel i zakres

Obsługa nowego statusu `CompletedWithWarnings = 4` w całym UI dokumentacji technicznej: enum TS, badge, strona szczegółów, toast SignalR, testy AXE.

## Pliki do modyfikacji

| Plik | Akcja |
|------|-------|
| `src/types/technicalDocumentation.types.ts` | `CompletedWithWarnings: 4` |
| `src/components/technicalDocumentation/TechnicalDocumentationStatusBadge.tsx` | Config orange |
| `src/pages/ProjectTechnicalDocumentationDetailsPage.tsx` | Terminal success + Alert warning |
| `src/hooks/useTechnicalDocumentationHub.ts` | `showInfo` toast |
| `src/components/technicalDocumentation/__tests__/TechnicalDocumentationStatusBadge.axe.test.tsx` | Nowy case |

## Wymagania techniczne

- Skills: `ui-types`, `ui-components`, `ui-hooks`
- Badge: `orange.800` / `orange.100`, label „Ukończono z ostrzeżeniami”
- DetailsPage:
```typescript
const isTerminalSuccess =
  documentation.status === TechnicalDocumentationStatus.Completed
  || documentation.status === TechnicalDocumentationStatus.CompletedWithWarnings;
```
- Alert `status="warning"` gdy `CompletedWithWarnings` (nad DetailsView)
- Hub: `showInfo('Przetwarzanie zakończone z ostrzeżeniami', ...)`
- **Brak** przycisku retry dla warnings (tylko Failed)
- `interface` props, `React.ReactElement`, bez `any`

## Kryteria akceptacji

- [ ] Status 4 renderuje badge (nie `undefined`)
- [ ] DetailsView widoczny przy status=4 (gdy `details` present)
- [ ] Toast info po SignalR event status=4
- [ ] `npm run test:run` — StatusBadge axe green
- [ ] `npm run build` — bez błędów TS

## Zależności

- Po: **api-fix-14** (enum w API + migracja EF)
- Przed: **ui-fix-03** (details view może używać statusu do bannera)
