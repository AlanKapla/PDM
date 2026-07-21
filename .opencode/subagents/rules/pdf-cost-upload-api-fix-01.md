# pdf-cost-upload-api-fix-01 — Docnet + ImageSharp + IPdfToImageConverter

## Kontekst

Feature: `.opencode/features/pdf-cost-upload.md`  
Audyt: `.opencode/subagents/rules/pdf-cost-upload-api-audit.md`  
Skills: `.opencode/skills/api-services/SKILL.md`

## Cel

Dodać zależności NuGet i serwis konwersji PDF → uporządkowana kolekcja bajtów JPG (in-memory).

## Zadania

1. W `src/Business/Business.csproj` dodaj:
   - `Docnet.Core` (aktualna stabilna wersja kompatybilna z .NET 10)
   - `SixLabors.ImageSharp` (aktualna stabilna)

2. Utwórz interfejs `src/Business/Interfaces/Services/IPdfToImageConverter.cs`:
```csharp
Task<IReadOnlyList<byte[]>> ConvertAllPagesToJpegAsync(
    byte[] pdfBytes,
    CancellationToken cancellationToken);
```

3. Utwórz `src/Business/Implementation/Services/AI/PdfToImageConverter.cs` (`sealed`):
   - DPI = **175** (stała lub z opcji)
   - Soft cap = **20** stron — jeśli więcej: rzuć wyjątek domenowy z komunikatem po angielsku dla logów + właściwość/kod pozwalający UI/workerowi zmapować na PL: „Plik PDF ma zbyt wiele stron (maks. 20).”
   - Docnet: render każdej strony do raw BGRA/bitmap → ImageSharp → JPEG quality ~85
   - In-memory (MemoryStream), bez plików tymczasowych na dysku
   - Password-protected: wykryj i rzuć wyjątek mapowalny na: „Plik PDF jest zabezpieczony hasłem i nie może zostać przetworzony”
   - Corrupt/invalid: „Nie udało się odczytać pliku PDF – plik może być uszkodzony”
   - Preferuj typed exceptions np. `PdfConversionException` z enumem Reason (PasswordProtected, Corrupt, TooManyPages)
   - Async: sync Docnet owijaj w `Task.Run` respektując `CancellationToken`
   - Kolejność stron zachowana (index 0 = strona 1)

4. Zarejestruj DI w `ServiceCollectionExtensions.cs`:  
   `services.AddScoped<IPdfToImageConverter, PdfToImageConverter>();`

5. Testy jednostkowe w `tests/Business.Tests` (Moq nie potrzebny dla pure converter jeśli masz fixture PDF; minimum: mock/stub lub testy wyjątku na pustych/niepoprawnych bajtach).

## Poza zakresem

- Integracja z DocumentParserService / Vision (prompt 02)
- Kontrolery, walidatory (prompt 03)

## Kryteria done

- [ ] Pakiety dodane, solution buduje się
- [ ] Interfejs + implementacja + DI
- [ ] Password / corrupt / too many pages obsługiwane
- [ ] Build Release OK
