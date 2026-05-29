# API Fix 04 — Nowe komendy: Submit, Withdraw, Approve, Reject

## Cel
Zaimplementować 4 nowe komendy zmieniające status `ApprovalStatus` kosztu:
1. `SubmitProjectCostForApprovalCommand` — właściciel lub admin: `Draft → PendingApproval`
2. `WithdrawProjectCostCommand` — właściciel lub admin: `PendingApproval → Draft`
3. `ApproveProjectCostCommand` — tylko admin: `PendingApproval → Approved`
4. `RejectProjectCostCommand` — tylko admin: `PendingApproval → Draft`

Przeczytaj skill `.github/skills/api/skill-api-cqrs.md` i `.github/skills/api/skill-api-validators.md`.

---

## Struktura folderów

```
src/CQRS/ProjectCosts/
  SubmitProjectCostForApproval/
    SubmitProjectCostForApprovalCommand.cs
    SubmitProjectCostForApprovalCommandHandler.cs
    SubmitProjectCostForApprovalCommandValidator.cs
  WithdrawProjectCost/
    WithdrawProjectCostCommand.cs
    WithdrawProjectCostCommandHandler.cs
    WithdrawProjectCostCommandValidator.cs
  ApproveProjectCost/
    ApproveProjectCostCommand.cs
    ApproveProjectCostCommandHandler.cs
    ApproveProjectCostCommandValidator.cs
  RejectProjectCost/
    RejectProjectCostCommand.cs
    RejectProjectCostCommandHandler.cs
    RejectProjectCostCommandValidator.cs
```

---

## Implementacja

### `SubmitProjectCostForApprovalCommand`

```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;

namespace CQRS.ProjectCosts.SubmitProjectCostForApproval
{
    public sealed record SubmitProjectCostForApprovalCommand : IRequestCommand<ProjectCostListItemWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid CostId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectCosts;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
```

### `SubmitProjectCostForApprovalCommandValidator`

```csharp
using FluentValidation;

namespace CQRS.ProjectCosts.SubmitProjectCostForApproval
{
    public sealed class SubmitProjectCostForApprovalCommandValidator : AbstractValidator<SubmitProjectCostForApprovalCommand>
    {
        public SubmitProjectCostForApprovalCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.CostId).NotEmpty();
        }
    }
}
```

### `SubmitProjectCostForApprovalCommandHandler`

```csharp
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.SubmitProjectCostForApproval
{
    public sealed class SubmitProjectCostForApprovalCommandHandler : IRequestHandler<SubmitProjectCostForApprovalCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IProjectCostAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<SubmitProjectCostForApprovalCommandHandler> logger;

        public SubmitProjectCostForApprovalCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IProjectCostAccessService accessService,
            ICurrentUser currentUser,
            ILogger<SubmitProjectCostForApprovalCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.accessService = accessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ProjectCostListItemWeb> Handle(
            SubmitProjectCostForApprovalCommand request,
            CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateAsync(request, cancellationToken);

            bool hasWriteAccess = await accessService.HasWriteAccessAsync(
                projectCost, currentUser.Id, cancellationToken);

            if (!hasWriteAccess)
            {
                throw new ForbiddenApiException("You do not have permission to submit this cost for approval.");
            }

            if (projectCost.ApprovalStatus != CostApprovalStatus.Draft)
            {
                throw new ValidationApiException("Only Draft costs can be submitted for approval.");
            }

            projectCost.ApprovalStatus = CostApprovalStatus.PendingApproval;
            projectCost.UpdatedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} submitted for approval in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return MapToWeb(projectCost);
        }

        private async Task<ProjectCost> GetAndValidateAsync(
            SubmitProjectCostForApprovalCommand request,
            CancellationToken cancellationToken)
        {
            return await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId
                    && pc.TenantId == request.TenantId
                    && pc.ProjectId == request.ProjectId
                    && !pc.IsDeleted)
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
        }

        private ProjectCostListItemWeb MapToWeb(ProjectCost pc)
        {
            return new ProjectCostListItemWeb
            {
                Id = pc.Id,
                UserId = pc.UserId,
                UserName = currentUser.FullName,
                Name = pc.Name,
                ContractorId = pc.ContractorId,
                ContractorName = null,
                Number = pc.Number,
                Date = pc.Date,
                Description = pc.Description,
                Net = pc.Net,
                Gross = pc.Gross,
                ApprovalStatus = pc.ApprovalStatus,
                ApprovedByUserId = pc.ApprovedByUserId,
                ApprovedAt = pc.ApprovedAt,
                HasDocument = false,
                DocumentFileName = null,
                PreviewSasUrl = null,
                DownloadSasUrl = null,
                CreatedAt = pc.CreatedAt
            };
        }
    }
}
```

---

### `WithdrawProjectCostCommand` + Validator + Handler

Analogicznie do Submit, ale:
- Sprawdza `ApprovalStatus == PendingApproval`
- Ustawia `ApprovalStatus = Draft`
- Czyści `ApprovedByUserId = null` i `ApprovedAt = null`
- Log: "Cost {CostId} withdrawn from approval"

```csharp
// WithdrawProjectCostCommand.cs — identyczna struktura jak Submit
// WithdrawProjectCostCommandValidator.cs — identyczna walidacja
// WithdrawProjectCostCommandHandler.cs — jak Submit ale:
//   if (projectCost.ApprovalStatus != CostApprovalStatus.PendingApproval)
//       throw new ValidationApiException("Only PendingApproval costs can be withdrawn.");
//   projectCost.ApprovalStatus = CostApprovalStatus.Draft;
//   projectCost.ApprovedByUserId = null;
//   projectCost.ApprovedAt = null;
```

---

### `ApproveProjectCostCommand` + Validator + Handler

```csharp
// ApproveProjectCostCommand — jak Submit
// ApproveProjectCostCommandValidator — jak Submit
// ApproveProjectCostCommandHandler:
//   bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(...)
//   if (!isAdmin) throw ForbiddenApiException
//   if (projectCost.ApprovalStatus != CostApprovalStatus.PendingApproval)
//       throw ValidationApiException("Only PendingApproval costs can be approved.")
//   projectCost.ApprovalStatus = CostApprovalStatus.Approved;
//   projectCost.ApprovedByUserId = currentUser.Id;
//   projectCost.ApprovedAt = DateTime.UtcNow;
//   projectCost.UpdatedAt = DateTime.UtcNow;
```

---

### `RejectProjectCostCommand` + Validator + Handler

```csharp
// RejectProjectCostCommand — jak Submit
// RejectProjectCostCommandValidator — jak Submit
// RejectProjectCostCommandHandler:
//   bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(...)
//   if (!isAdmin) throw ForbiddenApiException
//   if (projectCost.ApprovalStatus != CostApprovalStatus.PendingApproval)
//       throw ValidationApiException("Only PendingApproval costs can be rejected.")
//   projectCost.ApprovalStatus = CostApprovalStatus.Draft;
//   projectCost.ApprovedByUserId = null;
//   projectCost.ApprovedAt = null;
//   projectCost.UpdatedAt = DateTime.UtcNow;
```

---

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
```
Wszystkie 4 foldery powinny się kompilować.
