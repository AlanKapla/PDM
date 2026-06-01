# API Fix 01 — ParsedCostDto + IDocumentParserService + DocumentParserService

## Cel
Stwórz warstwę parsowania dokumentów przez GPT-4o Vision:
1. DTO `ParsedCostDto` w `Business/Interfaces/WebModels/AI/`
2. Interfejs `IDocumentParserService` w `Business/Interfaces/Services/`
3. Implementacja `DocumentParserService` w `Business.AIAgent/Services/`
4. Rejestracja w DI

## Krok 1 — Przeczytaj przed implementacją

Przeczytaj:
- `src/Business.AIAgent/Configuration/AzureAIAgentOptions.cs` — opcje (Endpoint, ApiKey, DefaultDeployment)
- `src/Business.AIAgent/Core/AgentRunner.cs` — wzorzec `BuildChatClient()` do skopiowania
- `src/Business.AIAgent/Registration/AIAgentServiceExtensions.cs` — gdzie dodać rejestrację DI
- `src/Business.AIAgent/Business.AIAgent.csproj` — żeby dodać `<ProjectReference>` do Business
- Jeden przykładowy plik z `src/Business/Interfaces/Services/` — wzorzec interfejsu
- Jeden przykładowy plik z `src/Business/Interfaces/WebModels/` — wzorzec DTO

## Krok 2 — Stwórz ParsedCostDto

Plik: `src/Business/Interfaces/WebModels/AI/ParsedCostDto.cs`

```csharp
namespace Business.Interfaces.WebModels.AI
{
    public sealed record ParsedCostDto
    {
        /// Nazwa kosztu — co zostało zakupione (np. "Materiały budowlane")
        public string Name { get; init; } = string.Empty;

        /// Rozszerzony opis z detalami pozycji
        public string? Description { get; init; }

        /// Numer faktury/rachunku
        public string? Number { get; init; }

        /// Suma netto całego dokumentu
        public decimal? Net { get; init; }

        /// Suma brutto całego dokumentu
        public decimal? Gross { get; init; }

        /// Data wystawienia dokumentu (ISO 8601)
        public DateTime? Date { get; init; }

        /// GUID kontrahenta — wypełniony tylko gdy ContractorFound = true
        public Guid? ContractorId { get; init; }

        /// Nazwa kontrahenta wyciągnięta z dokumentu
        public string? ContractorName { get; init; }

        /// NIP kontrahenta wyciągnięty z dokumentu
        public string? ContractorNip { get; init; }

        /// Adres kontrahenta wyciągnięty z dokumentu
        public string? ContractorAddress { get; init; }

        /// Czy kontrahent znaleziony w bazie danych
        public bool ContractorFound { get; init; }

        /// Sugestia nowego kontrahenta gdy ContractorFound = false
        public SuggestedContractorDto? SuggestedContractor { get; init; }

        /// Pewność AI (0.0 – 1.0)
        public double Confidence { get; init; }

        /// Surowy tekst z dokumentu (do debugowania)
        public string? RawText { get; init; }
    }

    public sealed record SuggestedContractorDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Nip { get; init; }
        public string? Address { get; init; }
    }
}
```

## Krok 3 — Stwórz IDocumentParserService

Plik: `src/Business/Interfaces/Services/IDocumentParserService.cs`

Namespace zgodny z innymi interfejsami w tym katalogu.

```csharp
using Business.Interfaces.WebModels.AI;

namespace Business.Interfaces.Services
{
    public interface IDocumentParserService
    {
        /// <summary>
        /// Parsuje dokument (JPG/PNG/PDF jako bitmap) przez GPT-4o Vision.
        /// Zwraca wyciągnięte dane kosztu. NIE zapisuje do bazy danych.
        /// </summary>
        Task<ParsedCostDto> ParseAsync(
            byte[] fileBytes,
            string mediaType,
            CancellationToken cancellationToken);
    }
}
```

## Krok 4 — Stwórz DocumentParserService

Plik: `src/Business.AIAgent/Services/DocumentParserService.cs`

Namespace: `Business.AIAgent.Services`

### Wymagania implementacji:

1. **Konstruktor**: wstrzyknij `IOptions<AzureAIAgentOptions>` i `ILogger<DocumentParserService>`.

2. **Budowanie klienta**: DOKŁADNIE ten sam wzorzec co w `AgentRunner.BuildChatClient()`:
   - Jeśli `ApiKey` jest null/empty → `DefaultAzureCredential`
   - Jeśli `ApiKey` jest ustawiony → `ApiKeyCredential`
   - Model: `_options.DefaultDeployment` (domyślnie `"gpt-4o"`)

3. **System prompt** (surowy string, nie resource file):
```
Jesteś ekspertem od odczytywania faktur i rachunków. 
Twoim zadaniem jest wyciągnięcie danych kosztowych z dostarczonego dokumentu.
Zawsze odpowiadaj WYŁĄCZNIE w formacie JSON, bez żadnych dodatkowych komentarzy.
Zwróć JSON z następującymi polami:
{
  "name": "nazwa tego co zostało zakupione (krótka, np. Materiały budowlane)",
  "description": "rozszerzony opis z drobnymi detalami co konkretnie zostało zakupione",
  "number": "numer faktury lub rachunku",
  "net": numer (suma netto całego dokumentu),
  "gross": numer (suma brutto całego dokumentu),
  "date": "data w formacie YYYY-MM-DD",
  "contractorName": "pełna nazwa firmy/osoby wystawiającej",
  "contractorNip": "NIP bez kresek",
  "contractorAddress": "pełny adres (ulica, kod, miasto)",
  "confidence": liczba od 0 do 1 (pewność odczytu)
}
Jeśli nie możesz odczytać danego pola, ustaw null.
Kwoty net i gross to SUMY całego dokumentu, nie poszczególnych pozycji.
```

4. **Budowanie wiadomości**:
   - `SystemChatMessage` z powyższym promptem
   - `UserChatMessage` z `ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(fileBytes), mediaType)`

5. **Wywołanie**: `await client.CompleteChatAsync(messages, cancellationToken: cancellationToken)`

6. **Deserializacja**: użyj `System.Text.Json.JsonSerializer.Deserialize<JsonDocument>()` do parsowania odpowiedzi. Obsługuj:
   - Odpowiedź może zawierać markdown fences (` ```json ``` `) — usuń je przed deserializacją
   - Jeśli deserializacja się nie powiedzie → zwróć `ParsedCostDto` z `Confidence = 0`, `Name = "Nieznany koszt"`, zaloguj błąd na `_logger.LogWarning`
   - Parsuj pola jeden po drugim (JsonDocument.RootElement.TryGetProperty) — nie deserializuj całości do klasy (bezpieczniej)

7. **Mapowanie na ParsedCostDto**: wypełnij wszystkie pola `ParsedCostDto`. `RawText` = surowa odpowiedź od AI (pierwsze 500 znaków).

8. **Brak obsługi PDF w tym serwisie** — PDF jest konwertowany do bitmap PRZED wywołaniem ParseAsync (w handlerze CQRS). Ten serwis otrzymuje zawsze JPG/PNG bytes.

### Przykładowa struktura:

```csharp
public async Task<ParsedCostDto> ParseAsync(byte[] fileBytes, string mediaType, CancellationToken cancellationToken)
{
    try
    {
        AzureOpenAIClient azureClient = BuildClient();
        ChatClient client = azureClient.GetChatClient(_options.DefaultDeployment);

        List<ChatMessage> messages = [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(fileBytes), mediaType))
        ];

        ChatCompletion response = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        string rawJson = response.Content[0].Text;
        return MapToDto(rawJson);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to parse cost document via AI");
        return new ParsedCostDto { Name = "Nieznany koszt", Confidence = 0 };
    }
}
```

## Krok 5 — Dodaj ProjectReference w Business.AIAgent.csproj

W pliku `src/Business.AIAgent/Business.AIAgent.csproj` dodaj:
```xml
<ProjectReference Include="..\Business\Business.csproj" />
```
(w istniejącej `<ItemGroup>` z `<ProjectReference>`)

## Krok 6 — Zarejestruj w DI

W `src/Business.AIAgent/Registration/AIAgentServiceExtensions.cs` w metodzie `AddAIAgent()` dodaj:
```csharp
services.AddScoped<IDocumentParserService, DocumentParserService>();
```

## Weryfikacja
Po implementacji uruchom:
```
dotnet build src/Business.AIAgent/Business.AIAgent.csproj
```
Nie powinno być błędów kompilacji.
