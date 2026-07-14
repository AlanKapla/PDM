# API Fix 04 — AICostController + Rejestracja DI

## Cel
Stwórz endpoint HTTP `POST /ai/cost/parse` w nowym `AICostController`.
Kontroler przyjmuje multipart/form-data (plik + costType), wywołuje query przez MediatR, zwraca `ParsedCostDto`.

## Krok 1 — Przeczytaj przed implementacją

Przeczytaj:
- `src/WebApi/Controllers/CostTrackerController.cs` — PEŁNA treść (wzorzec: BaseApiController, route, authorize, FromForm, Send())
- `src/WebApi/Controllers/ProjectCostController.cs` — sprawdź wzorzec autoryzacji i routingu
- Jeden z prostszych kontrolerów — żeby zrozumieć BaseApiController i `Send()`

## Krok 2 — Stwórz AICostController

Plik: `src/WebApi/Controllers/AICostController.cs`

### Wymagania:
- Dziedziczy z `BaseApiController` (lub cokolwiek dziedziczą inne kontrolery)
- Route: `api/tenants/{tenantId:guid}/projects/{projectId:guid}/ai/cost`
- Endpoint: `POST parse`
- Autoryzacja — sprawdź jak inne kontrolery realizują autoryzację (policy/attribute) — dostosuj do wzorca projektu
- Limit rozmiaru pliku: 20 MB
- Zwraca `ActionResult<ParsedCostDto>` lub `IActionResult`

```csharp
using Business.Interfaces.WebModels.AI;
using CQRS.AI.ParseCostDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/ai/cost")]
    public sealed class AICostController : BaseApiController  // BaseApiController z projektu
    {
        // Konstruktor wzorowany na innych kontrolerach (IMediator przez BaseApiController)

        /// <summary>
        /// Parsuje dokument kosztowy (JPG, PNG, PDF) przez GPT-4o Vision.
        /// Zwraca sugestię danych kosztu do zatwierdzenia przez użytkownika.
        /// NIE zapisuje kosztu — tylko parsuje.
        /// </summary>
        [HttpPost("parse")]
        [RequestSizeLimit(20_971_520)]          // 20 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 20_971_520)]
        [ProducesResponseType(typeof(ParsedCostDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ParseDocument(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] IFormFile file,
            [FromForm] CostDocumentType costType = CostDocumentType.TrackedCost,
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest("Plik jest wymagany.");
            }

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".pdf"))
            {
                return BadRequest("Dozwolone formaty: JPG, PNG, PDF.");
            }

            ParseCostDocumentQuery query = new()
            {
                TenantId = tenantId,
                ProjectId = projectId,
                File = file,
                CostType = costType
            };

            ParsedCostDto result = await Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
```

### Ważne:
- `BaseApiController` — sprawdź jak konstruktor jest zdefiniowany w istniejących kontrolerach. Niektóre wstrzykują `IMediator` w konstruktorze, inne przez primary constructor (C# 12). Dopasuj wzorzec.
- `Send()` — jeśli BaseApiController ma metodę `Send<T>()` wokół Mediator.Send(), użyj jej. Jeśli nie — `await _mediator.Send(query, cancellationToken)`
- Autoryzacja — sprawdź jak inne kontrolery stosują autoryzację (np. `[Authorize]`, `[RequirePermission]` lub custom attribute). **Nie stosuj `[Authorize(Policy = ...)]` jeśli projekt tego nie używa.** Dopasuj do wzorca.
- Obsługa CancellationToken — jeśli inne kontrolery go przyjmują w akcji, dodaj go. Jeśli nie — pomiń.

## Krok 3 — Weryfikacja

```
dotnet build src/WebApi/WebApi.csproj
```

Nie powinno być błędów. Jeśli są błędy z referencją do CQRS.AI — sprawdź czy CQRS.csproj jest referowany przez WebApi.csproj (powinien być).
