# Prompt API-05: Kontroler + Rejestracja DI

## Cel
Dodaj 2 nowe endpointy do `CostEstimateController` oraz zarejestruj `ICostEstimateAIGeneratorService` w DI.

---

## Krok 1: Nowe endpointy w kontrolerze

### Plik: `src/WebApi/Controllers/CostEstimateController.cs`

Dodaj na górze pliku nowe `using`:
```csharp
using CQRS.CostEstimates.GenerateCostEstimateAIPreview;
using CQRS.CostEstimates.CreateCostEstimateFromAIPreview;
using Business.Interfaces.WebModels.AI;
```

Dodaj dwie nowe metody do klasy `CostEstimateController` **po** metodzie `CreateCostEstimate`:

```csharp
/// <summary>
/// Generuje podgląd kosztorysu przez AI na podstawie opisu inwestycji i wybranego szablonu.
/// Nie zapisuje niczego do bazy danych — zwraca podgląd do zatwierdzenia przez użytkownika.
/// </summary>
/// <param name="tenantId">Tenant ID</param>
/// <param name="projectId">Project ID</param>
/// <param name="request">Opis inwestycji i ID szablonu</param>
[HttpPost("generate-ai-preview")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
[ProducesResponseType(typeof(AICostEstimatePreviewWeb), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GenerateCostEstimateAIPreview(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromBody] AICostEstimateRequestWeb request)
{
    GenerateCostEstimateAIPreviewCommand command = new GenerateCostEstimateAIPreviewCommand
    {
        TenantId = tenantId,
        ProjectId = projectId,
        Request = request
    };
    return Ok(await Send(command));
}

/// <summary>
/// Zapisuje kosztorys zatwierdzony przez użytkownika z podglądu wygenerowanego przez AI.
/// Atomowo tworzy kosztorys z grupami, pozycjami i wartościami pól.
/// Zwraca ID nowo utworzonego kosztorysu.
/// </summary>
/// <param name="tenantId">Tenant ID</param>
/// <param name="projectId">Project ID</param>
/// <param name="body">Nazwa, opis i podgląd AI</param>
[HttpPost("create-from-ai-preview")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> CreateCostEstimateFromAIPreview(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromBody] CreateCostEstimateFromAIPreviewWeb body)
{
    CreateCostEstimateFromAIPreviewCommand command = new CreateCostEstimateFromAIPreviewCommand
    {
        TenantId = tenantId,
        ProjectId = projectId,
        Name = body.Name,
        Description = body.Description,
        Preview = body.Preview
    };
    Guid id = await Send(command);
    return CreatedAtAction(
        nameof(GetCostEstimateDetails),
        new { tenantId, projectId, id },
        id);
}
```

---

## Krok 2: Rejestracja DI

### Plik: `src/Business.AIAgent/Registration/AIAgentServiceExtensions.cs`

Dodaj do metody `AddAIAgent` rejestrację nowego serwisu:

```csharp
services.AddScoped<ICostEstimateAIGeneratorService, CostEstimateAIGeneratorService>();
```

Dodaj brakujące `using`:
```csharp
using Business.AIAgent.Services;
using Business.Interfaces.Services;
```

---

## Krok 3: Rejestracja `CostEstimateFieldValueValidator` (jeśli jeszcze nie ma)

W istniejącym miejscu rejestracji serwisów CQRS (sprawdź `WebApi/Extensions/` lub plik DI dla CQRS):

```csharp
services.AddScoped<CostEstimateFieldValueValidator>();
```

**Uwaga:** `CostEstimateFieldValueValidator` jest już używany przez `UpsertCostEstimateItemFieldCommandHandler` — sprawdź czy jest już zarejestrowany w DI. Jeśli tak, nie dodawaj ponownie.

---

## Weryfikacja

```
dotnet build src/WebApi/WebApi.csproj
```
Oczekiwany wynik: Build succeeded, 0 errors.

Zweryfikuj też endpointy w Swagger:
- `POST /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/generate-ai-preview`
- `POST /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/create-from-ai-preview`
