# api-fix-01 — TechnicalDocumentationOptions + konfiguracja

## Cel i zakres

Rozszerzyć `TechnicalDocumentationOptions` o flagę `UseGroupPipeline`, limity multi-image i mapowanie grup tematycznych. Zaktualizować `appsettings` (dev: `true`, prod: `false`).

## Pliki do modyfikacji

| Plik | Akcja |
|------|-------|
| `src/Business/.../TechnicalDocumentationOptions.cs` | Modyfikacja |
| `src/WebApi/appsettings.json` | Modyfikacja |
| `src/WebApi/appsettings.Development.json` | Modyfikacja |

## Wymagania techniczne

- Skills: `api-services`, `api-cqrs`
- Bez `var`; `is null` / `is not null`
- Nowe pola:
  - `UseGroupPipeline` (bool, default `false`)
  - `MaxImagesPerGroup` (int, default `6`)
  - `CompressionThresholdBytes` (long, default `3_145_728`)
  - `DrawingTypeToThematicGroups` (`Dictionary<string, string[]>`) — mapowanie z planu § grupy

## Kryteria akceptacji

- [ ] Opcje rejestrowane w DI (`IOptions<TechnicalDocumentationOptions>`)
- [ ] `appsettings.Development.json`: `UseGroupPipeline: true`
- [ ] `appsettings.json`: `UseGroupPipeline: false`
- [ ] `dotnet build` — Release bez błędów
- [ ] Istniejące testy przechodzą (brak zmiany zachowania gdy flag=false)

## Zależności

- **Brak** — pierwszy krok w kolejności implementacji
- Blokuje: api-fix-03–10 (używają opcji)
