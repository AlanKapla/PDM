# api-fix-14 — CompletedWithWarnings + migracja EF

## Cel i zakres

Dodać `CompletedWithWarnings = 4` do enum, migracja EF, logika statusu w `TechnicalDocumentationProcessingService`, CQRS Details dla warnings status.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `Entities/Enums/TechnicalDocumentationStatus.cs` | `CompletedWithWarnings = 4` |
| `Entities/Migrations/*` | `add-technical-documentation-completed-with-warnings` |
| `TechnicalDocumentationProcessingService.cs` | Logika wyboru statusu |
| `GetTechnicalDocumentationDetailsQueryHandler.cs` | `Completed || CompletedWithWarnings` → deserialize Details |
| `RetryTechnicalDocumentationCommandValidator.cs` | Weryfikacja: retry tylko `Failed` (warnings OK) |
| `TechnicalDocumentationProcessingResultDto.cs` | Auto via enum |

## Wymagania techniczne

- Skills: `api-entities`, `api-cqrs`, `api-validators`
- Logika statusu:
  - `Failed` — wyjątek pipeline lub wszystkie grupy failed
  - `CompletedWithWarnings` — OK ale: `FailedPages` niepuste LUB `warnings[]` niepuste LUB critical diff nierozwiązany LUB audit warnings
  - `Completed` — czysty sukces
- EF migration: `dotnet ef migrations add add-technical-documentation-completed-with-warnings --startup-project ../WebApi`
- Pin `dotnet-ef` 10.0.1

## Kryteria akceptacji

- [ ] Migracja EF wygenerowana i stosuje się
- [ ] Details endpoint zwraca JSON dla status=4
- [ ] SignalR event wysyła status=4
- [ ] Test(y) ProcessingService dla scenariuszy warnings
- [ ] Blokuje **ui-fix-01** (enum musi być w API przed UI)

## Zależności

- Po: **api-fix-09** (audit warnings źródłem statusu) lub częściowo równolegle z api-fix-10
- Przed: **ui-fix-01**
