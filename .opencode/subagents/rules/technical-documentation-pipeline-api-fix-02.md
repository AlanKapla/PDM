# api-fix-02 — Multi-image w IAICompletionService

## Cel i zakres

Dodać `CompleteWithImagesAsync` do `IAICompletionService` i zaimplementować w `AzureAICompletionService` (wiele `CreateImagePart` w jednym `UserChatMessage`). Wrapper w `TechnicalDocumentationAgentInvoker` z walidacją max obrazów.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `src/Business.AIAgent/Services/IAICompletionService.cs` | Modyfikacja |
| `src/Business.AIAgent/Services/AzureAICompletionService.cs` | Modyfikacja |
| `src/Business/.../TechnicalDocumentationAgentInvoker.cs` | Modyfikacja |
| Testy jednostkowe invokera / completion (jeśli istnieją) | Modyfikacja lub nowe |

## Wymagania techniczne

- Skills: `api-services`
- Sygnatura:
```csharp
Task<string> CompleteWithImagesAsync(
    string systemPrompt,
    string? userText,
    IReadOnlyList<(byte[] ImageBytes, string MediaType)> images,
    CancellationToken cancellationToken,
    int maxOutputTokens = 8192,
    float? temperature = null,
    bool jsonMode = false);
```
- Kolejność partów: text → images (lub zgodnie z best practice OpenAI vision)
- Limit 6 obrazów enforced w callerze (GroupExtraction), nie w serwisie AI
- Zachować istniejące metody single-image bez zmian

## Kryteria akceptacji

- [ ] Metoda zaimplementowana i zarejestrowana
- [ ] `TechnicalDocumentationAgentInvoker` deleguje z opcjonalną walidacją count
- [ ] Test(y) jednostkowe: mock z 2+ obrazami lub weryfikacja budowy message
- [ ] `dotnet build` + `dotnet test Business.Tests` — bez regresji

## Zależności

- Po: **api-fix-01** (opcjonalnie MaxImagesPerGroup w invokerze)
- Blokuje: **api-fix-06**, **api-fix-07** (Agent C vision)
