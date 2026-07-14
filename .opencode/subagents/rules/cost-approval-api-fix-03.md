# API Fix 03 — Aktualizacja Create/Update komend, handlerów i GetProjectCosts

## Cel
Zaktualizować istniejące komendy/handlery/query po usunięciu `IsAccepted` i `SharedProjectCost`:
1. `CreateProjectCostCommand` — usunąć `IsAccepted`
2. `UpdateProjectCostCommand` — usunąć `IsAccepted`, usunąć `HandleSharedUserUpdateAsync`
3. `GetProjectCostsQuery` — dodać scope `PendingApproval`, usunąć scope `Shared`
4. `GetProjectCostsQueryHandler` — usunąć logikę Shared, dodać logikę PendingApproval

Przeczytaj skill `.opencode/skills/api/skill-api-cqrs.md`.

---

## Krok 1 — `CreateProjectCostCommand`

Plik: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs`

Usuń linię:
```csharp
public bool IsAccepted { get; init; }
```

---

## Krok 2 — `CreateProjectCostCommandHandler`

Plik: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs`

W metodzie `BuildProjectCostEntity` usuń linię:
```csharp
IsAccepted = request.IsAccepted,
```

W metodzie `MapToWeb` zastąp:
```csharp
IsAccepted = projectCost.IsAccepted,
HasDocument = attachment is not null,
DocumentFileName = attachment?.OriginalFileName,
PreviewSasUrl = null,
DownloadSasUrl = null,
SharedWithUserIds = new List<Guid>(),
CreatedAt = projectCost.CreatedAt
```
Na:
```csharp
ApprovalStatus = projectCost.ApprovalStatus,
ApprovedByUserId = projectCost.ApprovedByUserId,
ApprovedAt = projectCost.ApprovedAt,
HasDocument = attachment is not null,
DocumentFileName = attachment?.OriginalFileName,
PreviewSasUrl = null,
DownloadSasUrl = null,
CreatedAt = projectCost.CreatedAt
```

---

## Krok 3 — `UpdateProjectCostCommand`

Plik: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs`

Usuń linię:
```csharp
public bool IsAccepted { get; init; }
```

---

## Krok 4 — `UpdateProjectCostCommandHandler` — gruntowna przebudowa

Plik: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs`

**Zastąp całą zawartość pliku:**

```csharp
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.Shared;
using Entities.Models.Costs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.UpdateProjectCost
{
    public sealed class UpdateProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<UpdateProjectCostCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IProjectCostAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly IContractorService contractorService;
        private readonly ILogger<UpdateProjectCostCommandHandler> logger;

        public UpdateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IProjectCostAccessService accessService,
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            IContractorService contractorService,
            ICurrentUser currentUser,
            ILogger<UpdateProjectCostCommandHandler> logger,
            ILogger<ProjectCostHandlerBase> baseLogger)
            : base(blobStorageService, attachmentRepository, baseLogger)
        {
            this.projectCostRepo = projectCostRepo;
            this.accessService = accessService;
            this.contractorService = contractorService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ProjectCostListItemWeb> Handle(UpdateProjectCostCommand request, CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateProjectCostAsync(request, cancellationToken);

            bool hasWriteAccess = await accessService.HasWriteAccessAsync(
                projectCost, currentUser.Id, cancellationToken);

            if (!hasWriteAccess)
            {
                throw new ForbiddenApiException("You do not have permission to update this cost.");
            }

            ApplyFieldUpdates(request, projectCost);
            await HandleDocumentOperationsAsync(request, projectCost, cancellationToken);

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} updated in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            string? contractorName = null;
            if (projectCost.ContractorId.HasValue)
            {
                Dictionary<Guid, string> names = await contractorService.GetNamesByIdsAsync(
                    new[] { projectCost.ContractorId.Value }, request.TenantId, cancellationToken);
                contractorName = names.GetValueOrDefault(projectCost.ContractorId.Value);
            }

            return MapToWeb(projectCost, contractorName);
        }

        private async Task<ProjectCost> GetAndValidateProjectCostAsync(
            UpdateProjectCostCommand request,
            CancellationToken cancellationToken)
        {
            return await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId
                    && pc.TenantId == request.TenantId
                    && pc.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
        }

        private void ApplyFieldUpdates(UpdateProjectCostCommand request, ProjectCost projectCost)
        {
            projectCost.Name = request.Name;
            projectCost.ContractorId = request.ContractorId;
            projectCost.Number = request.Number;
            projectCost.Date = request.Date?.Date;
            projectCost.Description = request.Description;
            projectCost.Net = request.Net;
            projectCost.Gross = request.Gross ?? request.Net;
            projectCost.UpdatedAt = DateTime.UtcNow;
        }

        private ProjectCostListItemWeb MapToWeb(ProjectCost projectCost, string? contractorName)
        {
            return new ProjectCostListItemWeb
            {
                Id = projectCost.Id,
                UserId = projectCost.UserId,
                UserName = currentUser.FullName,
                Name = projectCost.Name,
                ContractorId = projectCost.ContractorId,
                ContractorName = contractorName,
                Number = projectCost.Number,
                Date = projectCost.Date,
                Description = projectCost.Description,
                Net = projectCost.Net,
                Gross = projectCost.Gross,
                ApprovalStatus = projectCost.ApprovalStatus,
                ApprovedByUserId = projectCost.ApprovedByUserId,
                ApprovedAt = projectCost.ApprovedAt,
                HasDocument = false,
                DocumentFileName = null,
                PreviewSasUrl = null,
                DownloadSasUrl = null,
                CreatedAt = projectCost.CreatedAt
            };
        }

        private async Task HandleDocumentOperationsAsync(
            UpdateProjectCostCommand request,
            ProjectCost projectCost,
            CancellationToken cancellationToken)
        {
            if (request.RemoveDocument)
            {
                await RemoveAttachmentsAsync(projectCost.Id, cancellationToken);

                logger.LogInformation(
                    "Document removed for cost {CostId} in project {ProjectId}",
                    request.CostId, request.ProjectId);
            }

            IFormFile? fileToUpload = request.UpdatedDocument ?? request.Document;

            if (fileToUpload is not null)
            {
                if (request.UpdatedDocument is not null)
                {
                    await RemoveAttachmentsAsync(projectCost.Id, cancellationToken);
                }

                BaseCostAttachment attachment = await UploadDocumentToCostAsync(
                    fileToUpload, projectCost, cancellationToken);

                await PersistAttachmentAsync(attachment, cancellationToken);

                logger.LogInformation(
                    "Document uploaded for cost {CostId} in project {ProjectId}",
                    request.CostId, request.ProjectId);
            }
        }
    }
}
```

---

## Krok 5 — `ResourceScope` — dodaj `PendingApproval`

Plik: `src/Business/Interfaces/Constants/ResourceScope.cs`

Dodaj wartość do enuma (po `Shared` lub zamiast — sprawdź aktualną zawartość):

```csharp
/// <summary>
/// Only resources pending approval (requires admin role)
/// </summary>
PendingApproval = 3
```

Jeśli `Shared = 2` nadal istnieje w enum (bo używają go inne miejsca jak WorkSchedule), pozostaw je. Dodaj tylko `PendingApproval = 3`.

---

## Krok 6 — `GetProjectCostsQueryHandler` — przebudowa

Plik: `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs`

**Zastąp całą zawartość:**

```csharp
using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Costs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.GetProjectCosts
{
    public sealed class GetProjectCostsQueryHandler : IRequestHandler<GetProjectCostsQuery, IEnumerable<ProjectCostListItemWeb>>
    {
        private readonly IReadRepository<ProjectCost> projectCostRepo;
        private readonly IReadRepository<BaseCostAttachment> attachmentRepository;
        private readonly IUserService userService;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly IContractorService contractorService;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public GetProjectCostsQueryHandler(
            IReadRepository<ProjectCost> projectCostRepo,
            IReadRepository<BaseCostAttachment> attachmentRepository,
            IUserService userService,
            IBlobStorageService blobStorageService,
            IContractorService contractorService,
            ICurrentUser currentUser)
        {
            this.projectCostRepo = projectCostRepo;
            this.attachmentRepository = attachmentRepository;
            this.userService = userService;
            this.blobStorageService = blobStorageService;
            this.contractorService = contractorService;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<ProjectCostListItemWeb>> Handle(
            GetProjectCostsQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<ProjectCost> costs = await LoadCostsAsync(request, cancellationToken);

            List<ProjectCost> costList = costs.ToList();
            HashSet<Guid> costIds = costList.Select(pc => pc.Id).ToHashSet();

            ILookup<Guid, BaseCostAttachment> attachmentsByCostId = await LoadAttachmentLookupAsync(costIds);

            Dictionary<Guid, ProjectMemberUserInfo> membersDict = (await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken))
                .ToDictionary(m => m.UserId);

            List<Guid> contractorIds = costList
                .Where(pc => pc.ContractorId.HasValue)
                .Select(pc => pc.ContractorId!.Value)
                .Distinct()
                .ToList();

            Dictionary<Guid, string> contractorNames = await contractorService.GetNamesByIdsAsync(
                contractorIds, request.TenantId, cancellationToken);

            return costList
                .Select(pc => MapToWeb(pc, attachmentsByCostId[pc.Id].FirstOrDefault(), membersDict, contractorNames))
                .OrderByDescending(c => c.Date)
                .ThenByDescending(c => c.CreatedAt)
                .ToList();
        }

        private async Task<IEnumerable<ProjectCost>> LoadCostsAsync(
            GetProjectCostsQuery request,
            CancellationToken cancellationToken)
        {
            return request.Scope switch
            {
                ResourceScope.All => await projectCostRepo.GetBySearch(
                    pc => pc.ProjectId == request.ProjectId
                        && pc.TenantId == request.TenantId
                        && !pc.IsDeleted),

                ResourceScope.Mine => await projectCostRepo.GetBySearch(
                    pc => pc.ProjectId == request.ProjectId
                        && pc.TenantId == request.TenantId
                        && pc.UserId == currentUser.Id
                        && !pc.IsDeleted),

                ResourceScope.PendingApproval => await projectCostRepo.GetBySearch(
                    pc => pc.ProjectId == request.ProjectId
                        && pc.TenantId == request.TenantId
                        && pc.ApprovalStatus == CostApprovalStatus.PendingApproval
                        && !pc.IsDeleted),

                _ => throw new ValidationApiException($"Unsupported scope value: {request.Scope}")
            };
        }

        private async Task<ILookup<Guid, BaseCostAttachment>> LoadAttachmentLookupAsync(HashSet<Guid> costIds)
        {
            if (costIds.Count == 0)
            {
                return Enumerable.Empty<BaseCostAttachment>().ToLookup(a => a.CostId);
            }

            List<BaseCostAttachment> attachments = (await attachmentRepository.GetBySearch(
                a => costIds.Contains(a.CostId))).ToList();

            return attachments.ToLookup(a => a.CostId);
        }

        private ProjectCostListItemWeb MapToWeb(
            ProjectCost pc,
            BaseCostAttachment? attachment,
            Dictionary<Guid, ProjectMemberUserInfo> membersDict,
            Dictionary<Guid, string> contractorNames)
        {
            string? previewSasUrl = null;
            string? downloadSasUrl = null;

            if (attachment is not null)
            {
                Uri sasUriPreview = blobStorageService.GenerateSasUri(
                    ContainerName,
                    attachment.BlobName,
                    attachment.OriginalFileName,
                    expiresInMinutes: 60,
                    contentDisposition: "inline");

                Uri sasUriDownload = blobStorageService.GenerateSasUri(
                    ContainerName,
                    attachment.BlobName,
                    attachment.OriginalFileName,
                    expiresInMinutes: 60,
                    contentDisposition: "attachment");

                previewSasUrl = sasUriPreview.ToString();
                downloadSasUrl = sasUriDownload.ToString();
            }

            string userName = membersDict.TryGetValue(pc.UserId, out ProjectMemberUserInfo? member)
                ? member.FullName
                : pc.UserId.ToString();

            string? contractorName = pc.ContractorId.HasValue
                ? contractorNames.GetValueOrDefault(pc.ContractorId.Value)
                : null;

            return new ProjectCostListItemWeb
            {
                Id = pc.Id,
                UserId = pc.UserId,
                UserName = userName,
                Name = pc.Name,
                ContractorId = pc.ContractorId,
                ContractorName = contractorName,
                Number = pc.Number,
                Date = pc.Date,
                Description = pc.Description,
                Net = pc.Net,
                Gross = pc.Gross,
                ApprovalStatus = pc.ApprovalStatus,
                ApprovedByUserId = pc.ApprovedByUserId,
                ApprovedAt = pc.ApprovedAt,
                HasDocument = attachment is not null,
                DocumentFileName = attachment?.OriginalFileName,
                PreviewSasUrl = previewSasUrl,
                DownloadSasUrl = downloadSasUrl,
                CreatedAt = pc.CreatedAt
            };
        }
    }
}
```

---

## Krok 7 — Usuń `IReadRepository<SharedProjectCost>` z konstruktora

Sprawdź czy `GetProjectCostsQueryHandler` konstruktor ma `IReadRepository<SharedProjectCost>` — jeśli tak, już jest usunięty w kroku 6.

---

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
```
Oczekiwany wynik: błędy tylko w kontrolerze (sharowanie) i `GetProjectCostsQueryValidator` jeśli waliduje scope `Shared`.
