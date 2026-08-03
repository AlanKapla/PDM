# Prompt: admin-cold-mail-ui-fix-02 — Formularz wysyłki + historia + hooki

## Cel
Pełna strona cold mail: formularz, potwierdzenie, historia z filtrem i modalem szczegółów.

## Spec / audyt
- `.opencode/features/admin-cold-mail.md`
- `.opencode/subagents/rules/admin-cold-mail-ui-audit.md`
- Skills: `ui-components`, `ui-hooks`, `ui-forms-modals`, `ui-accessibility`
- Zależność: **admin-cold-mail-ui-fix-01**

## Hooki
- `useColdMailHistory(emailFilter?: string)` — React Query, queryKey z filtrem, debounce 300ms na poziomie strony/filtra
- `useSendColdMails()` — mutation; onSuccess: invalidate history + toast z Queued/Failed counts („zakolejkowano”, nie „dostarczono”)

## Komponenty
1. **ColdMailSendForm** — FormControl: textarea maile (1/linia), Input subject, Textarea body; FormHelperText (max 50); walidacja client (trim, dedupe, regex, max 50); przycisk Wyślij
2. **Confirm** — przed submit: dialog z liczbą adresów (AppModal lub wzorzec confirm z repo)
3. **ColdMailHistoryFilter** — Input filtr email
4. **ColdMailHistoryTable** — kolumny: RecipientEmail, Subject, Status (PL: W kolejce / Błąd), SentAt; klikalny wiersz
5. **ColdMailHistoryDetailsModal** — pełne dane + Body jako plain text (`whiteSpace="pre-wrap"`, **bez** dangerouslySetInnerHTML); ErrorMessage jeśli Failed
6. **AdminColdMailsPage** — składa form + filter + table; stany loading/error/empty

## Konwencje
- Logika w hookach; komponenty tylko render
- Brak `any`; return `React.ReactElement`
- Kolory: Chakra tokens / appColors
- Named exports dla komponentów domenowych
- Accessibility: labels, aria-label, AXE jeśli standardowo dodawane

## Definition of done
- SuperAdmin może wysłać cold mail i zobaczyć historię z filtrem
- Toast komunikuje kolejkowanie
- Build / lint OK dla zmienionych plików
