# Audyt UI — admin-cold-mail

Data: 2026-07-16  
Źródło: ui-audit-agent + feature `.opencode/features/admin-cold-mail.md`  
Powiązany audyt API: `.opencode/subagents/rules/admin-cold-mail-api-audit.md`

## Podsumowanie

Feature wpisuje się w hub SuperAdmin. Cold-mail UI **nie istnieje** — dodać kartę, stronę, formularz, tabelę historii, hooki, typy, route.

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 5 + 1 page |
| Zmodyfikowane pliki | ~5 |
| Nowe hooki | 2 |
| Nowe API calls | 2 |

## Co reuse'ować

1. **Karta hub** — wzorzec `UsersAdminPanel` / `DemoModePanel` na `AdminPage`
2. **Strona** — wzorzec `AdminUsersPage` (layout, toast, loading)
3. **`adminApi.ts` + `admin.types.ts`** — rozszerzyć
4. **React Query** — wzorzec `useSendWelcomeEmails` / `useAdminUsers`
5. **Filtr** — debounce ~300 ms jak na listach (np. Contractors)
6. **`SuperAdminRoute`** — `/admin/*` już chronione; dodać route `/admin/cold-mails`
7. **Klikalny wiersz** — `cursor="pointer"` + modal szczegółów (body jako plain text, **nie** `dangerouslySetInnerHTML`)

## Pliki do utworzenia

| Plik | Rola |
|------|------|
| `src/pages/AdminColdMailsPage.tsx` | Strona: form + historia |
| `src/components/admin/ColdMailsAdminPanel.tsx` | Karta na AdminPage → navigate |
| `src/components/admin/ColdMailSendForm.tsx` | Textarea maile, subject, body, submit |
| `src/components/admin/ColdMailHistoryTable.tsx` | Tabela historii |
| `src/components/admin/ColdMailHistoryFilter.tsx` | Input filtr email |
| `src/components/admin/ColdMailHistoryDetailsModal.tsx` | Modal: pełne body (plain text) |
| `src/hooks/useColdMailHistory.ts` | Query GET |
| `src/hooks/useSendColdMails.ts` | Mutation POST |

## Pliki do zmiany

| Plik | Zmiana |
|------|--------|
| `src/pages/AdminPage.tsx` | Dodać `ColdMailsAdminPanel` do siatki |
| `src/routes/AppRouter.tsx` | Route `/admin/cold-mails` + SuperAdminRoute |
| `src/api/adminApi.ts` | `sendColdMails`, `getColdMails` |
| `src/types/admin.types.ts` | typy Request/Result/History |
| `src/api/mock/mockHandlers.ts` | mocki jeśli wymagane przez demo |

## Decyzje UI (przyjęte do implementacji)

1. **Confirm przed wysyłką** — tak (dialog potwierdzenia z liczbą adresów)
2. **Body w historii** — w tabeli: email, subject, status, data; pełne body w modalu (plain text / `<pre>` / whiteSpace pre-wrap)
3. **Walidacja client** — trim, dedupe, max 50, podstawowy regex email; FormHelperText
4. **Status PL** — „W kolejce” / „Błąd”; ErrorMessage w modalu lub Tooltip

## Toast / copy

Nie „wysłano / dostarczono” — **„zakolejkowano”** / „X w kolejce, Y błędów” (API = enqueue).

## Accessibility

- Label + FormControl dla wszystkich pól
- `aria-label` na IconButton
- AXE testy dla `ColdMailSendForm` i `ColdMailHistoryTable` (zalecane)
- Komunikaty błędów: `role="alert"` / Chakra Alert

## Poza zakresem UI v1

- WYSIWYG
- Zapisywanie szablonów
- Pagination historii (hard cap po stronie API)
