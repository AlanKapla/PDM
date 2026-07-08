using Business.AIAgent.Services;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Business.Implementation.Services.AI;

public sealed class DocumentParserService : IDocumentParserService
{
    private readonly IAICompletionService _completionService;
    private readonly ILogger<DocumentParserService> _logger;

    private const string SystemPrompt =
        """
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
          "categoryName": "kategoria wydatku (np. Materiały budowlane, Robocizna, Transport)",
          "confidence": liczba od 0 do 1 (pewność odczytu)
        }
        Jeśli nie możesz odczytać danego pola, ustaw null.
        Kwoty net i gross to SUMY całego dokumentu, nie poszczególnych pozycji.
        """;

    public DocumentParserService(
        IAICompletionService completionService,
        ILogger<DocumentParserService> logger)
    {
        _completionService = completionService;
        _logger = logger;
    }

    public async Task<ParsedCostDto> ParseAsync(
        byte[] fileBytes,
        string mediaType,
        CancellationToken cancellationToken)
    {
        try
        {
            string rawJson = await _completionService.CompleteWithImageAsync(SystemPrompt, fileBytes, mediaType, cancellationToken);
            return MapToDto(rawJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse cost document via AI");
            return new ParsedCostDto { Name = "Nieznany koszt", Confidence = 0 };
        }
    }

    private ParsedCostDto MapToDto(string rawJson)
    {
        string raw = rawJson?.Length > 500 ? rawJson[..500] : rawJson ?? string.Empty;

        string jsonToParse = rawJson ?? string.Empty;

        if (jsonToParse.Contains("```"))
        {
            int start = jsonToParse.IndexOf('{');
            int end = jsonToParse.LastIndexOf('}');
            if (start >= 0 && end >= start)
            {
                jsonToParse = jsonToParse[start..(end + 1)];
            }
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonToParse);
            JsonElement root = doc.RootElement;

            string name = root.TryGetProperty("name", out JsonElement nameProp) && nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString() ?? "Nieznany koszt"
                : "Nieznany koszt";

            string? description = root.TryGetProperty("description", out JsonElement descProp) && descProp.ValueKind == JsonValueKind.String
                ? descProp.GetString()
                : null;

            string? number = root.TryGetProperty("number", out JsonElement numProp) && numProp.ValueKind == JsonValueKind.String
                ? numProp.GetString()
                : null;

            decimal? net = root.TryGetProperty("net", out JsonElement netProp) && netProp.ValueKind == JsonValueKind.Number
                ? netProp.GetDecimal()
                : null;

            decimal? gross = root.TryGetProperty("gross", out JsonElement grossProp) && grossProp.ValueKind == JsonValueKind.Number
                ? grossProp.GetDecimal()
                : null;

            DateTime? date = root.TryGetProperty("date", out JsonElement dateProp) && dateProp.ValueKind == JsonValueKind.String
                ? DateTime.TryParse(dateProp.GetString(), out DateTime parsedDate) ? parsedDate : null
                : null;

            string? contractorName = root.TryGetProperty("contractorName", out JsonElement cnProp) && cnProp.ValueKind == JsonValueKind.String
                ? cnProp.GetString()
                : null;

            string? contractorNip = root.TryGetProperty("contractorNip", out JsonElement cnipProp) && cnipProp.ValueKind == JsonValueKind.String
                ? cnipProp.GetString()
                : null;

            string? contractorAddress = root.TryGetProperty("contractorAddress", out JsonElement caProp) && caProp.ValueKind == JsonValueKind.String
                ? caProp.GetString()
                : null;

            string? categoryName = root.TryGetProperty("categoryName", out JsonElement catProp) && catProp.ValueKind == JsonValueKind.String
                ? catProp.GetString()
                : null;

            double confidence = root.TryGetProperty("confidence", out JsonElement confProp) && confProp.ValueKind == JsonValueKind.Number
                ? confProp.GetDouble()
                : 0;

            SuggestedContractorDto? suggestedContractor = null;
            if (!string.IsNullOrWhiteSpace(contractorName))
            {
                suggestedContractor = new SuggestedContractorDto
                {
                    Name = contractorName,
                    Nip = contractorNip,
                    Address = contractorAddress
                };
            }

            return new ParsedCostDto
            {
                Name = name,
                Description = description,
                Number = number,
                Net = net,
                Gross = gross,
                Date = date,
                ContractorName = contractorName,
                ContractorNip = contractorNip,
                ContractorAddress = contractorAddress,
                ContractorFound = false,
                SuggestedContractor = suggestedContractor,
                CategoryName = categoryName,
                CategoryFound = false,
                Confidence = confidence,
                RawText = raw
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize AI response: {Raw}", raw);
            return new ParsedCostDto { Name = "Nieznany koszt", Confidence = 0, RawText = raw };
        }
    }
}
