# API Fix 03 — ParseCostDocumentQuery + Handler (CQRS) + PDF konwersja (Docnet.Core)

## Cel
Stwórz query CQRS do parsowania dokumentów kosztowych przez AI.
Handler obsługuje: konwersję PDF→PNG (Docnet.Core), wywołanie AI serwisu, wyszukanie kontrahenta.

## Krok 1 — Przeczytaj przed implementacją

Przeczytaj:
- `src/CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommand.cs` — wzorzec Command
- `src/CQRS/CostTrackers/Shared/CostTrackerCommandBase.cs` — jak definiuje IAuthorizableRequest
- `src/CQRS/CostTrackers/GetCostLinkOptions/GetCostLinkOptionsQuery.cs` — wzorzec Query (IRequestQuery<T>)
- `src/CQRS/CostTrackers/GetCostLinkOptions/GetCostLinkOptionsQueryHandler.cs` — wzorzec handlera query
- `src/CQRS/CQRS.csproj` — żeby dodać ProjectReference do Business.AIAgent

## Krok 2 — Dodaj NuGet Docnet.Core do Business.AIAgent.csproj

W pliku `src/Business.AIAgent/Business.AIAgent.csproj` dodaj w `<ItemGroup>` z PackageReference:
```xml
<PackageReference Include="Docnet.Core" Version="2.6.0" />
```

## Krok 3 — Stwórz enum CostDocumentType

Plik: `src/CQRS/AI/ParseCostDocument/CostDocumentType.cs`

```csharp
namespace CQRS.AI.ParseCostDocument
{
    public enum CostDocumentType
    {
        TrackedCost = 0,
        ProjectCost = 1
    }
}
```

## Krok 4 — Stwórz ParseCostDocumentQuery

Plik: `src/CQRS/AI/ParseCostDocument/ParseCostDocumentQuery.cs`

Wzoruj się na istniejących queryach (IRequestQuery<T> + IAuthorizableRequest).

```csharp
using Business.Interfaces.WebModels.AI;
using Microsoft.AspNetCore.Http;

namespace CQRS.AI.ParseCostDocument
{
    public sealed record ParseCostDocumentQuery : IRequestQuery<ParsedCostDto>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required IFormFile File { get; init; }
        public CostDocumentType CostType { get; init; } = CostDocumentType.TrackedCost;

        public string PermissionCode => CostType == CostDocumentType.ProjectCost
            ? PermissionCodes.ProjectCosts
            : PermissionCodes.ProjectDashboardTracker;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
```

Uwaga: użyj DOKŁADNIE tych samych nazw `IRequestQuery<T>`, `IAuthorizableRequest`, `ResourceRef`, `PermissionCodes` jakie są w istniejących queryach (sprawdź namespace).

## Krok 5 — Stwórz ParseCostDocumentQueryHandler

Plik: `src/CQRS/AI/ParseCostDocument/ParseCostDocumentQueryHandler.cs`

### Wymagania:

1. Implementuje `IRequestHandler<ParseCostDocumentQuery, ParsedCostDto>`
2. Wstrzykuje:
   - `IDocumentParserService` (z `Business.Interfaces.Services`)
   - `IContractorService` (z `Business.Interfaces.Services`)
3. Obsługuje konwersję PDF → PNG przez Docnet.Core
4. Wywołuje `IDocumentParserService.ParseAsync()`
5. Wyszukuje kontrahenta przez `IContractorService.SearchByProfileAsync()`
6. Uzupełnia `ParsedCostDto` o dane kontrahenta

### Implementacja:

```csharp
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Docnet.Core;
using Docnet.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp; // lub System.Drawing — sprawdź co jest w projekcie
using SixLabors.ImageSharp.PixelFormats;

namespace CQRS.AI.ParseCostDocument
{
    public sealed class ParseCostDocumentQueryHandler
        : IRequestHandler<ParseCostDocumentQuery, ParsedCostDto>
    {
        private readonly IDocumentParserService _parserService;
        private readonly IContractorService _contractorService;
        private readonly ILogger<ParseCostDocumentQueryHandler> _logger;

        public ParseCostDocumentQueryHandler(
            IDocumentParserService parserService,
            IContractorService contractorService,
            ILogger<ParseCostDocumentQueryHandler> logger)
        {
            _parserService = parserService;
            _contractorService = contractorService;
            _logger = logger;
        }

        public async Task<ParsedCostDto> Handle(
            ParseCostDocumentQuery request,
            CancellationToken cancellationToken)
        {
            using MemoryStream ms = new();
            await request.File.CopyToAsync(ms, cancellationToken);
            byte[] fileBytes = ms.ToArray();

            string mediaType = request.File.ContentType.ToLowerInvariant();
            byte[] imageBytes;

            // Konwersja PDF → PNG (pierwsza strona)
            if (mediaType == "application/pdf")
            {
                imageBytes = ConvertPdfFirstPageToPng(fileBytes);
                mediaType = "image/png";
            }
            else
            {
                imageBytes = fileBytes;
            }

            // Parsowanie przez AI
            ParsedCostDto result = await _parserService.ParseAsync(
                imageBytes, mediaType, cancellationToken);

            // Wyszukanie kontrahenta w bazie
            result = await EnrichWithContractorAsync(result, request.TenantId, cancellationToken);

            return result;
        }

        private async Task<ParsedCostDto> EnrichWithContractorAsync(
            ParsedCostDto dto,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.ContractorName) &&
                string.IsNullOrWhiteSpace(dto.ContractorNip))
            {
                return dto;
            }

            try
            {
                Contractor? found = await _contractorService.SearchByProfileAsync(
                    dto.ContractorName,
                    dto.ContractorNip,
                    tenantId,
                    cancellationToken);

                if (found is not null)
                {
                    return dto with
                    {
                        ContractorId = found.Id,
                        ContractorFound = true,
                        SuggestedContractor = null
                    };
                }

                // Kontrahent nie znaleziony — buduj sugestię
                if (!string.IsNullOrWhiteSpace(dto.ContractorName))
                {
                    return dto with
                    {
                        ContractorFound = false,
                        SuggestedContractor = new SuggestedContractorDto
                        {
                            Name = dto.ContractorName,
                            Nip = dto.ContractorNip,
                            Address = dto.ContractorAddress
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search contractor for name={Name}, nip={Nip}",
                    dto.ContractorName, dto.ContractorNip);
            }

            return dto;
        }

        private static byte[] ConvertPdfFirstPageToPng(byte[] pdfBytes)
        {
            using IDocLib docLib = DocLib.Instance;
            using IDocReader docReader = docLib.GetDocReader(
                pdfBytes,
                new PageDimensions(1080, 1440));

            using IPageReader pageReader = docReader.GetPageReader(0);

            int width = pageReader.GetPageWidth();
            int height = pageReader.GetPageHeight();
            byte[] rawBytes = pageReader.GetImage(); // BGRA format

            // Konwertuj BGRA → PNG przez SixLabors.ImageSharp lub System.Drawing
            // Jeśli SixLabors.ImageSharp jest dostępne:
            using Image<Bgra32> image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
            using MemoryStream output = new();
            image.SaveAsPng(output);
            return output.ToArray();
        }
    }
}
```

### Ważne uwagi:
- **Docnet.Core** zwraca BGRA pixel data z `GetImage()` — nie PNG. Trzeba skonwertować.
- Sprawdź czy `SixLabors.ImageSharp` jest już w projekcie (szukaj w `.csproj` pliku CQRS lub WebApi). Jeśli jest → użyj. Jeśli nie ma → dodaj do `Business.AIAgent.csproj`: `<PackageReference Include="SixLabors.ImageSharp" Version="3.1.7" />`
- Jeśli nie chcesz ImageSharp → możesz użyć `System.Drawing.Common` (tylko Windows) lub napisać prosty BGRA→PNG encoder przez `BinaryWriter` (pomijamy alpha). Najlepiej sprawdź co już jest w projekcie.
- `Contractor` — sprawdź namespace gdzie jest encja i dodaj using
- `IContractorService.SearchByProfileAsync` — dostosuj typy parametrów do tego co zostało zaimplementowane w api-fix-02

## Krok 6 — Dodaj ProjectReference do CQRS.csproj

W pliku `src/CQRS/CQRS.csproj` dodaj (tylko jeśli Business.AIAgent jest potrzebny do IDocumentParserService):

Uwaga: `IDocumentParserService` jest w `Business.Interfaces.Services` (projekt `Business`), a `DocumentParserService` (implementacja) jest w `Business.AIAgent`. Handler CQRS wstrzykuje TYLKO interfejs → referencja do `Business` powinna już być w CQRS.csproj. Sprawdź — jeśli jest → nie dodawaj nic. Jeśli nie ma → dodaj:
```xml
<ProjectReference Include="..\Business\Business.csproj" />
```

**NIE dodawaj** referencji z CQRS do Business.AIAgent (architektura: CQRS → Business, Business.AIAgent → Business).

## Krok 7 — Weryfikacja

```
dotnet build src/CQRS/CQRS.csproj
dotnet build src/Business.AIAgent/Business.AIAgent.csproj
```
