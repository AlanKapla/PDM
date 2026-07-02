# API Fix 05 — Docnet.Core + konwersja PDF→JPG + stałe blob/kolejka

## Cel
Pakiet NuGet Docnet.Core, serwis konwersji PDF (wszystkie strony — bez limitu w MVP), rozszerzenie enumów infrastrukturalnych.

## Decyzje MVP
- **Wszystkie strony PDF** — `ConvertAllPagesAsync` bez twardego limitu stron
- Osobny kontener blob `TechnicalDocumentation` (nie `Documentation` używany przez ProjectFile)

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
- `.cursor/skills/api-services/SKILL.md`

## Zależności
- **api-fix-01** (encje)
- Może być równolegle z **api-fix-03/04** jeśli stałe zdefiniowane w fix-04

## Pliki referencyjne
- `src/Business/Interfaces/Configurations/BlobContainerNames.cs`
- `src/Business/Interfaces/Constants/QueueNames.cs`
- `src/Business/Interfaces/Configurations/BlobStorageSettings.cs` — `GetContainerName`

---

## 1. Pakiet NuGet

W `src/Business/Business.csproj`:
```xml
<PackageReference Include="Docnet.Core" Version="2.6.0" />
```

## 2. Enumy (jeśli nie dodane w fix-04)

### `BlobContainerNames.cs`
```csharp
TechnicalDocumentation
```

### `QueueNames.cs`
```csharp
public const string TechnicalDocumentationProcess = "technical-documentation-process";
```

### `BlobStorageSettings.GetContainerName`
Dodaj mapowanie:
```csharp
BlobContainerNames.TechnicalDocumentation => "technicaldocumentation",
```

## 3. `IPdfToImageConverterService`

Plik: `src/Business/Interfaces/Services/IPdfToImageConverterService.cs`

```csharp
public interface IPdfToImageConverterService
{
    /// <summary>
    /// Renderuje wszystkie strony PDF do tablic bajtów JPG.
    /// </summary>
    Task<IReadOnlyList<byte[]>> ConvertAllPagesToJpegAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken);
}
```

## 4. `PdfToImageConverterService`

Plik: `src/Business/Implementation/Services/PdfToImageConverterService.cs`

Implementacja z Docnet.Core:
```csharp
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
```

Logika:
1. `using IDocLib docLib = DocLib.Instance;`
2. `using IDocReader reader = docLib.GetDocReader(pdfBytes, new PageDimensions(2480, 3508));` — DPI odpowiedni dla Vision
3. Pętla `for (int i = 0; i < reader.GetPageCount(); i++)` — **wszystkie strony**
4. `reader.GetPageReader(i).GetImage()` → konwersja do JPEG bytes (SkiaSharp lub wbudowane API Docnet — sprawdź dokumentację Docnet 2.6)
5. Zwróć `List<byte[]>`

Obsługa błędów:
- Niepoprawny PDF → log + `throw` z czytelnym komunikatem (worker ustawi `Failed`)

## 5. Rejestracja DI

```csharp
services.AddScoped<IPdfToImageConverterService, PdfToImageConverterService>();
```

## 6. Uwagi Docker (linux-x64)

Docnet wymaga natywnych bibliotek PDFium. Zweryfikuj build w Release dla `linux-x64` (target API Dockerfile). Jeśli build pada — udokumentuj w komentarzu w serwisie.

## Weryfikacja
```powershell
dotnet build src/Business/Business.csproj
dotnet build --configuration Release
```

## Następny krok
Serwis używany w **api-fix-07** (`TechnicalDocumentationProcessingService`).
