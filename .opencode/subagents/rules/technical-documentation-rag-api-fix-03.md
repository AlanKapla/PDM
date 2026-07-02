# API Fix 03 — CQRS Queries (lista, szczegóły, count)

## Cel
Implementacja odczytu dokumentacji technicznej: 3 Queries + Handlery + brak walidatorów poza standardowym pipeline (opcjonalnie lightweight validators dla Guid).

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
- `.cursor/skills/api-cqrs/SKILL.md`
- `.cursor/skills/api-repositories/SKILL.md`
- `.cursor/skills/api-validators/SKILL.md` (opcjonalnie)

## Zależności
- **api-fix-01** — encje
- **api-fix-02** — web modele + `TechnicalDocumentationDetailsSerializer`

## Pliki referencyjne
- `src/CQRS/Notifications/GetUnreadCounter/GetUnreadCounterQuery.cs` — wzorzec count
- `src/CQRS/CostEstimates/GetCostEstimates/GetCostEstimatesQueryHandler.cs` — wzorzec listy
- `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs` — wzorzec `GenerateSasUri`

---

## 1. `GetTechnicalDocumentationListQuery`

Katalog: `src/CQRS/TechnicalDocumentation/GetTechnicalDocumentationList/`

```csharp
public sealed record GetTechnicalDocumentationListQuery(Guid TenantId, Guid ProjectId)
    : IRequestQuery<List<TechnicalDocumentationListItemWeb>>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Handler:**
- `IReadRepository<ProjectTechnicalDocumentation>`
- Predykat: `d.TenantId == request.TenantId && d.ProjectId == request.ProjectId`
- Sortowanie: `CreatedAt` desc
- `FileCount` = `d.Files.Count` (Include Files lub osobne count)
- Mapuj na `TechnicalDocumentationListItemWeb`

## 2. `GetTechnicalDocumentationDetailsQuery`

Katalog: `src/CQRS/TechnicalDocumentation/GetTechnicalDocumentationDetails/`

```csharp
public sealed record GetTechnicalDocumentationDetailsQuery(
    Guid TenantId, Guid ProjectId, Guid DocumentationId)
    : IRequestQuery<TechnicalDocumentationDetailsWeb>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Handler:**
- `IReadRepository<ProjectTechnicalDocumentation>`, `IBlobStorageService`
- Predykat: `TenantId + ProjectId + DocumentationId`
- Brak rekordu → `NotFoundApiException`
- Deserializuj `DetailsJson` → `ProjectTechnicalDocumentationDetails?` (tylko gdy `Status == Completed`)
- Dla każdego pliku: `GenerateSasUri` z kontenera `BlobContainerNames.TechnicalDocumentation` (stała zostanie dodana w fix-05; użyj enum value)
  - Preview: `contentDisposition: inline` dla PDF/JPEG
  - Download: `contentDisposition: attachment`
- Wzorzec SAS: `GetProjectCostsQueryHandler`

## 3. `GetTechnicalDocumentationCountQuery`

Katalog: `src/CQRS/TechnicalDocumentation/GetTechnicalDocumentationCount/`

```csharp
public sealed record GetTechnicalDocumentationCountQuery(Guid TenantId, Guid ProjectId)
    : IRequestQuery<int>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Handler:**
- `IReadRepository<ProjectTechnicalDocumentation>`
- `return await repo.CountAsync(d => d.TenantId == tenantId && d.ProjectId == projectId, ct);`
- Sprawdź dostępną metodę count w `IReadRepository` (np. `GetCountBySearch`)

## Zasady
- Handlery `sealed`, bez `var`
- Predykaty zawsze `TenantId` + `ProjectId`
- Nie zwracaj `AutoRetryCount` w response

## Weryfikacja
```powershell
dotnet build src/CQRS/CQRS.csproj
```

## Następny krok
Queries są używane przez kontroler w **api-fix-08**. Commands w **api-fix-04**.
