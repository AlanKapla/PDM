# contractors-api-fix-04 — ContractorController

## Cel
Implementacja kontrolera REST dla endpointów CRUD kontrahentów.

## Skill
Przeczytaj `.github/skills/api/skill-api-controllers.md` przed implementacją.

## Kontekst
- Raport audytu: `.github/subagents/rules/contractors-api-audit.md`
- CQRS istnieje po `contractors-api-fix-03`
- Wzorzec: `src/WebApi/Controllers/TenantController.cs`
- Routing: `/api/tenants/{tenantId:guid}/contractors`
- Permissions: GET → `TenantView`, POST/PUT/DELETE → `TenantEdit`

## Nowy plik: `src/WebApi/Controllers/ContractorController.cs`

```csharp
[ApiController]
[Route("api/tenants/{tenantId:guid}/contractors")]
public class ContractorController : BaseApiController
{
    public ContractorController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.TenantView)]
    [ProducesResponseType(typeof(IEnumerable<ContractorWeb>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractors(
        [FromRoute] Guid tenantId,
        [FromQuery] string? search)
    {
        // command = new GetContractorsQuery { TenantId = tenantId, Search = search };
        // return Ok(await Send(query));
    }

    [HttpGet("{contractorId:guid}")]
    [Authorize(Policy = PermissionCodes.TenantView)]
    [ProducesResponseType(typeof(ContractorWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractor(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contractorId)
    {
        // ...
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.TenantEdit)]
    [ProducesResponseType(typeof(ContractorWeb), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateContractor(
        [FromRoute] Guid tenantId,
        [FromBody] CreateContractorCommand command)
    {
        command = command with { TenantId = tenantId };
        ContractorWeb result = await Send(command);
        return Created(string.Empty, result);
    }

    [HttpPut("{contractorId:guid}")]
    [Authorize(Policy = PermissionCodes.TenantEdit)]
    [ProducesResponseType(typeof(ContractorWeb), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateContractor(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contractorId,
        [FromBody] UpdateContractorCommand command)
    {
        command = command with { TenantId = tenantId, Id = contractorId };
        ContractorWeb result = await Send(command);
        return Ok(result);
    }

    [HttpDelete("{contractorId:guid}")]
    [Authorize(Policy = PermissionCodes.TenantEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContractor(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contractorId)
    {
        DeleteContractorCommand command = new DeleteContractorCommand
        {
            TenantId = tenantId,
            Id = contractorId,
        };
        await Send(command);
        return NoContent();
    }
}
```

## Wymagania szczegółowe
- `[FromBody]` dla CreateContractor i UpdateContractor (nie `[FromForm]` — brak plików)
- `[FromQuery] string? search` dla GetContractors
- Nie dodawać własnej obsługi wyjątków — middleware robi to globalnie
- Wszystkie endpointy wymagają `[Authorize(Policy = ...)]`

## Weryfikacja
```
dotnet build ProductDataManagementWebAPI.sln --nologo 2>&1 | Select-Object -Last 10
dotnet test tests\WebApi.Tests\WebApi.Tests.csproj --nologo --verbosity minimal 2>&1 | Select-Object -Last 15
dotnet test tests\Business.Tests\Business.Tests.csproj --nologo --verbosity minimal 2>&1 | Select-Object -Last 15
```
Wszystkie testy muszą przejść.
