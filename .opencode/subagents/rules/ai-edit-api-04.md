# Prompt: ai-edit-api-04 — Endpointy w CostEstimateController + DI registration

## Cel

Dodać 2 nowe endpointy w `CostEstimateController.cs` oraz zarejestrować nowy serwis `ICostEstimateAIEditService` w DI.

## Pliki do modyfikacji

### 1. `WebApi/Controllers/CostEstimateController.cs`

Dodać dwa nowe endpointy po istniejących endpointach AI (po `CreateFromAIPreview`):

```csharp
/// <summary>
/// Generuje propozycję edycji kosztorysu przez AI.
/// </summary>
[HttpPost("{id:guid}/ai/edit-preview")]
[ProducesResponseType(typeof(AICostEditPreviewWeb), 200)]
[ProducesResponseType(400)]
[ProducesResponseType(403)]
[ProducesResponseType(404)]
public async Task<ActionResult<AICostEditPreviewWeb>> GenerateAIEditPreview(
    Guid tenantId,
    Guid projectId,
    Guid id,
    [FromBody] AICostEditRequestWeb request,
    CancellationToken cancellationToken)
{
    GenerateCostEstimateAIEditCommand command = new()
    {
        TenantId = tenantId,
        ProjectId = projectId,
        CostEstimateId = id,
        UserRequest = request.UserRequest
    };

    AICostEditPreviewWeb preview = await mediator.Send(command, cancellationToken);
    return Ok(preview);
}

/// <summary>
/// Aplikuje zatwierdzone zmiany edycji AI do kosztorysu.
/// </summary>
[HttpPost("{id:guid}/ai/apply-edit")]
[ProducesResponseType(204)]
[ProducesResponseType(400)]
[ProducesResponseType(403)]
[ProducesResponseType(404)]
public async Task<ActionResult> ApplyAIEdit(
    Guid tenantId,
    Guid projectId,
    Guid id,
    [FromBody] ApplyCostEstimateAIEditWeb body,
    CancellationToken cancellationToken)
{
    ApplyCostEstimateAIEditCommand command = new()
    {
        TenantId = tenantId,
        ProjectId = projectId,
        CostEstimateId = id,
        Preview = body.Preview
    };

    await mediator.Send(command, cancellationToken);
    return NoContent();
}
```

### 2. Web model dla body apply: `Business/Interfaces/WebModels/AI/ApplyCostEstimateAIEditWeb.cs` (NOWY)

```csharp
namespace Business.Interfaces.WebModels.AI;

/// <summary>
/// Body requestu do aplikowania edycji AI.
/// </summary>
public sealed record ApplyCostEstimateAIEditWeb
{
    public AICostEditPreviewWeb Preview { get; init; } = default!;
}
```

### 3. DI Registration

W `02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/ServiceCollectionExtensions.cs` (lub `Business.AIAgent/Registration/AIAgentServiceExtensions.cs`):

Dodać rejestrację serwisu:
```csharp
services.AddScoped<ICostEstimateAIEditService, CostEstimateAIEditService>();
```

Upewnij się że serwis jest zarejestrowany w odpowiednim miejscu (tam gdzie inne serwisy AI/biznesowe).

## Weryfikacja

1. Endpointy istnieją w `CostEstimateController.cs`
2. Plik `ApplyCostEstimateAIEditWeb.cs` istnieje
3. Serwis zarejestrowany w DI
4. Build API przechodzi
5. Endpointy mają poprawne atrybuty `[Authorize]` (dostają z `PermissionCodes.ProjectEstimates` z commanda)
