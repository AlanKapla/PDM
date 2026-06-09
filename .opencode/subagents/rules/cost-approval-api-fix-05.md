# API Fix 05 — Aktualizacja kontrolera + walidatorów + CostTrackerHandlerBase

## Cel
1. Zaktualizować `ProjectCostController` — usunąć endpointy share, dodać 4 nowe endpointy
2. Zaktualizować `GetProjectCostsQueryValidator` — usunąć walidację scope `Shared`
3. Zaktualizować `CostTrackerHandlerBase.MapProjectCostToWeb` — nowe pola
4. Sprawdzić i zaktualizować walidator `UpdateProjectCostCommandValidator` — usunąć IsAccepted

Przeczytaj skill `.opencode/skills/api/skill-api-controllers.md`.

---

## Krok 1 — `ProjectCostController`

Plik: `src/WebApi/Controllers/ProjectCostController.cs`

**Zastąp całą zawartość:**

```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.ApproveProjectCost;
using CQRS.ProjectCosts.CreateProjectCost;
using CQRS.ProjectCosts.DeleteProjectCost;
using CQRS.ProjectCosts.GetProjectCosts;
using CQRS.ProjectCosts.RejectProjectCost;
using CQRS.ProjectCosts.SubmitProjectCostForApproval;
using CQRS.ProjectCosts.UpdateProjectCost;
using CQRS.ProjectCosts.WithdrawProjectCost;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId}/projects/{projectId}/cost")]
    [ApiController]
    public class ProjectCostController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet("{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> GetProjectCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] ResourceScope scope)
        {
            GetProjectCostsQuery query = new GetProjectCostsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Scope = scope
            };
            IEnumerable<ProjectCostListItemWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> CreateProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] CreateProjectCostCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            ProjectCostListItemWeb result = await Send(command);
            return Created(string.Empty, result);
        }

        [HttpPut("{costId}")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> UpdateProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId,
            [FromForm] UpdateProjectCostCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };

            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        [HttpDelete("{costId}")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> DeleteProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            DeleteProjectCostCommand command = new DeleteProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Skierowanie kosztu do akceptacji (właściciel lub admin, Draft → PendingApproval)
        /// </summary>
        [HttpPost("{costId}/submit")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> SubmitForApproval(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            SubmitProjectCostForApprovalCommand command = new SubmitProjectCostForApprovalCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Wycofanie kosztu z akceptacji (właściciel lub admin, PendingApproval → Draft)
        /// </summary>
        [HttpPost("{costId}/withdraw")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> WithdrawFromApproval(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            WithdrawProjectCostCommand command = new WithdrawProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Akceptacja kosztu (tylko admin, PendingApproval → Approved)
        /// </summary>
        [HttpPost("{costId}/approve")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> ApproveCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            ApproveProjectCostCommand command = new ApproveProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Odrzucenie kosztu (tylko admin, PendingApproval → Draft)
        /// </summary>
        [HttpPost("{costId}/reject")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> RejectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            RejectProjectCostCommand command = new RejectProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }
    }
}
```

---

## Krok 2 — `GetProjectCostsQueryValidator`

Plik: `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryValidator.cs`

Sprawdź czy waliduje zakres `Scope` przez `IsInEnum` — jeśli tak, to zadziała automatycznie po dodaniu `PendingApproval` do enuma. Nie wymaga zmian jeśli używa `IsInEnum()`.

---

## Krok 3 — `UpdateProjectCostCommandValidator`

Plik: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandValidator.cs`

Sprawdź czy waliduje pole `IsAccepted` — jeśli tak, usuń tę regułę. Pole już nie istnieje w komendzie.

---

## Krok 4 — `CostTrackerHandlerBase` — aktualizacja `MapProjectCostToWeb`

Plik: `src/CQRS/CostTrackers/Shared/CostTrackerHandlerBase.cs`

Znajdź metodę `MapProjectCostToWeb`. Sprawdź czy używa `IsAccepted` lub `SharedWith` — jeśli tak, zaktualizuj mapowanie do nowych pól (`ApprovalStatus` zamiast `IsAccepted`, brak `SharedWithUserIds`).

---

## Krok 5 — Dashboard/WorkScheduleSync — filter po Approved

Plik: `src/Business/Implementation/Services/WorkScheduleSyncService.cs`

Sprawdź czy w tym serwisie jest jakiekolwiek filtrowanie po `IsAccepted`. Jeśli tak, zaktualizuj na `ApprovalStatus == CostApprovalStatus.Approved`.

Sprawdź też `CostTrackerHandlerBase` — metoda pobierająca `ProjectCost` do trackera powinna filtrować tylko `Approved`:
```csharp
pc.ApprovalStatus == CostApprovalStatus.Approved
```

---

## Weryfikacja
```
dotnet build src/WebApi/WebApi.csproj --configuration Release 2>&1 | Where-Object { $_ -match " error " } | Select-Object -Last 20
```
Oczekiwany wynik: Build succeeded bez błędów.
