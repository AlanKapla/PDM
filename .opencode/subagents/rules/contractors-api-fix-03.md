# contractors-api-fix-03 — CQRS: Create/Update/Delete/GetList/GetById Contractor

## Cel
Implementacja pełnego CQRS dla encji `Contractor` (tenant-scoped).
5 operacji: GetContractors (lista), GetContractor (jeden), CreateContractor, UpdateContractor, DeleteContractor.

## Skill
Przeczytaj `.opencode/skills/api/skill-api-cqrs.md` i `.opencode/skills/api/skill-api-validators.md` przed implementacją.

## Kontekst
- Raport audytu: `.opencode/subagents/rules/contractors-api-audit.md`
- Encja `Contractor` istnieje po `contractors-api-fix-01`
- Wzorzec tenant-scoped: `src/CQRS/Tenants/UpdateTenant/` — przeczytaj ten folder jako wzorzec
- Permission: `PermissionCodes.TenantEdit` dla zapisu, `PermissionCodes.TenantView` dla odczytu
- `GetResource()` zwraca tylko `new ResourceRef(TenantId: TenantId)` — brak ProjectId

## ContractorWeb DTO
Najpierw utwórz plik: `src/Business/Interfaces/WebModels/Contractors/ContractorWeb.cs`

```csharp
public sealed class ContractorWeb
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## Struktura katalogów do stworzenia

```
src/CQRS/Contractors/
├── GetContractors/
│   ├── GetContractorsQuery.cs
│   └── GetContractorsQueryHandler.cs
├── GetContractor/
│   ├── GetContractorQuery.cs
│   └── GetContractorQueryHandler.cs
├── CreateContractor/
│   ├── CreateContractorCommand.cs
│   ├── CreateContractorCommandHandler.cs
│   └── CreateContractorCommandValidator.cs
├── UpdateContractor/
│   ├── UpdateContractorCommand.cs
│   ├── UpdateContractorCommandHandler.cs
│   └── UpdateContractorCommandValidator.cs
└── DeleteContractor/
    ├── DeleteContractorCommand.cs
    └── DeleteContractorCommandHandler.cs
```

## Implementacja

### GetContractorsQuery
```csharp
public sealed record GetContractorsQuery : IRequest<IEnumerable<ContractorWeb>>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public string? Search { get; init; }   // filtr po nazwie/NIP/mieście

    public string PermissionCode => PermissionCodes.TenantView;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
```

Handler: Pobierz z `IReadRepository<Contractor>` wszystkich kontrahentów tenanta (`TenantId == request.TenantId && !IsDeleted`).
Jeśli `Search` nie jest null/empty — filtruj po `Name.Contains(search) || TaxId.Contains(search) || City.Contains(search)` (case-insensitive).
Mapuj na `ContractorWeb`. Sortuj po `Name`.

### GetContractorQuery
```csharp
public sealed record GetContractorQuery : IRequest<ContractorWeb>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ContractorId { get; init; }

    public string PermissionCode => PermissionCodes.TenantView;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
```

Handler: Pobierz po `Id == ContractorId && TenantId == TenantId && !IsDeleted`. Jeśli null → rzuć `NotFoundException` (wzorzec z innych handlerów).

### CreateContractorCommand + Validator + Handler
```csharp
public sealed record CreateContractorCommand : IRequestCommand<ContractorWeb>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required string Name { get; init; }
    public string? TaxId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Notes { get; init; }

    public string PermissionCode => PermissionCodes.TenantEdit;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
```

Validator:
- `TenantId`: RequiredId()
- `Name`: NotEmpty, MaximumLength(500)
- `TaxId`: MaximumLength(50), When not null
- `Email`: MaximumLength(200), EmailAddress, When not null
- `PhoneNumber`: MaximumLength(20), When not null
- `Street`: MaximumLength(300), When not null
- `City`: MaximumLength(100), When not null
- `PostalCode`: MaximumLength(20), When not null
- `Country`: MaximumLength(100), When not null
- `Notes`: MaximumLength(2000), When not null

Handler: Twórz nową encję `Contractor`, ustaw `CreatedAt = DateTime.UtcNow`. Insert + SaveChanges. Mapuj na `ContractorWeb`.

### UpdateContractorCommand + Validator + Handler
```csharp
public sealed record UpdateContractorCommand : IRequestCommand<ContractorWeb>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? TaxId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Notes { get; init; }

    public string PermissionCode => PermissionCodes.TenantEdit;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
```

Validator: analogiczny do Create + `Id`: RequiredId().

Handler: Pobierz encję (`Id && TenantId && !IsDeleted`), jeśli null → NotFoundException. Zaktualizuj wszystkie pola. Ustaw `UpdatedAt = DateTime.UtcNow`. Update + SaveChanges. Mapuj na `ContractorWeb`.

### DeleteContractorCommand + Handler
```csharp
public sealed record DeleteContractorCommand : IRequest, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid Id { get; init; }

    public string PermissionCode => PermissionCodes.TenantEdit;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
```

Handler: Pobierz encję (`Id && TenantId && !IsDeleted`). Jeśli null → NotFoundException. Soft-delete: `contractor.IsDeleted = true; contractor.DeletedAt = DateTime.UtcNow`. Update + SaveChanges.
Loguj info: `"Deleted Contractor {ContractorId} for tenant {TenantId}"`.

## Zasada mapowania ContractorWeb

Utwórz prywatną metodę w każdym handlerze (lub statyczną w ContractorWeb) do mapowania encji → DTO:
```csharp
private static ContractorWeb MapToWeb(Contractor contractor) => new ContractorWeb
{
    Id = contractor.Id,
    TenantId = contractor.TenantId,
    Name = contractor.Name,
    TaxId = contractor.TaxId,
    Email = contractor.Email,
    PhoneNumber = contractor.PhoneNumber,
    Street = contractor.Street,
    City = contractor.City,
    PostalCode = contractor.PostalCode,
    Country = contractor.Country,
    Notes = contractor.Notes,
    CreatedAt = contractor.CreatedAt,
    UpdatedAt = contractor.UpdatedAt,
};
```

## Weryfikacja
```
dotnet build ProductDataManagementWebAPI.sln --nologo 2>&1 | Select-Object -Last 10
```
Build musi zakończyć się `0 Error(s)`.
