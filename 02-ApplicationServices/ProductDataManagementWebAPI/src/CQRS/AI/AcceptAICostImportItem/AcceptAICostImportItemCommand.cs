using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.AI.ParseCostDocument;
using CQRS.AI.Shared;
using CQRS.CostTrackers.CreateTrackedCost;
using CQRS.ProjectCosts.CreateProjectCost;
using Entities.Enums;
using Entities.Models.AI;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using EntityCostDocumentType = Entities.Enums.CostDocumentType;

namespace CQRS.AI.AcceptAICostImportItem
{
    public sealed record AcceptAICostImportItemCommand : IRequestCommand<AICostImportItemWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid ItemId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public sealed class AcceptAICostImportItemCommandHandler
        : IRequestHandler<AcceptAICostImportItemCommand, AICostImportItemWeb>
    {
        private readonly IRepository<AICostImportItem> itemRepo;
        private readonly IReadRepository<AICostImportBatch> batchRepo;
        private readonly IRepository<BaseCost> costRepo;
        private readonly IAICostImportBlobService blobService;
        private readonly IMediator mediator;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<AcceptAICostImportItemCommandHandler> logger;

        public AcceptAICostImportItemCommandHandler(
            IRepository<AICostImportItem> itemRepo,
            IReadRepository<AICostImportBatch> batchRepo,
            IRepository<BaseCost> costRepo,
            IAICostImportBlobService blobService,
            IMediator mediator,
            IAccessService accessService,
            ICurrentUser currentUser,
            ILogger<AcceptAICostImportItemCommandHandler> logger)
        {
            this.itemRepo = itemRepo;
            this.batchRepo = batchRepo;
            this.costRepo = costRepo;
            this.blobService = blobService;
            this.mediator = mediator;
            this.accessService = accessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<AICostImportItemWeb> Handle(
            AcceptAICostImportItemCommand request,
            CancellationToken cancellationToken)
        {
            AICostImportItem? item = await itemRepo.GetFirstBySearch(
                i => i.Id == request.ItemId
                     && i.TenantId == request.TenantId
                     && i.ProjectId == request.ProjectId);

            if (item is null)
            {
                throw new NotFoundApiException(nameof(AICostImportItem), request.ItemId.ToString());
            }

            if (item.Status is not (
                AICostImportItemStatus.Pending
                or AICostImportItemStatus.ErrorNeedsReview
                or AICostImportItemStatus.DuplicateDetected))
            {
                throw new ConflictApiException(
                    nameof(AICostImportItem),
                    request.ItemId.ToString(),
                    "Only pending or error items can be accepted.");
            }

            AICostImportBatch? batch = await batchRepo.GetFirstBySearch(
                b => b.Id == item.BatchId
                     && b.TenantId == request.TenantId
                     && b.ProjectId == request.ProjectId);

            if (batch is null)
            {
                throw new NotFoundApiException(nameof(AICostImportBatch), item.BatchId.ToString());
            }

            await EnsureBatchAccessAsync(batch, request.TenantId, request.ProjectId, cancellationToken);

            ParsedCostDto? parsedData = DeserializeParsedData(item.ParsedDataJson);
            if (parsedData is null || string.IsNullOrWhiteSpace(parsedData.Name))
            {
                throw new ConflictApiException(
                    nameof(AICostImportItem),
                    request.ItemId.ToString(),
                    "Parsed cost data is missing or invalid.");
            }

            Guid costId = await CreateCostAsync(batch, parsedData, request, cancellationToken);

            BaseCost? cost = await costRepo.GetFirstBySearch(
                c => c.Id == costId
                     && c.TenantId == request.TenantId
                     && c.ProjectId == request.ProjectId);

            if (cost is not null)
            {
                cost.SourceFileHashSha256 = item.FileHashSha256;
                await costRepo.Update(cost);

                await blobService.MoveToCostAttachmentAsync(
                    cost,
                    item.BlobPath,
                    item.OriginalFileName,
                    item.ContentType,
                    item.FileSizeBytes,
                    request.TenantId,
                    request.ProjectId,
                    cancellationToken);
            }

            item.Status = AICostImportItemStatus.Accepted;
            item.AcceptedCostId = costId;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await itemRepo.Update(item);
            await itemRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Accepted AI cost import item {ItemId} as cost {CostId}",
                item.Id, costId);

            return AICostImportMapper.MapItemToWeb(item, batch, blobService);
        }

        private async Task<Guid> CreateCostAsync(
            AICostImportBatch batch,
            ParsedCostDto parsedData,
            AcceptAICostImportItemCommand request,
            CancellationToken cancellationToken)
        {
            if (batch.CostDocumentType == EntityCostDocumentType.ProjectCost)
            {
                CreateProjectCostCommand createProjectCostCommand = new CreateProjectCostCommand
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    Name = parsedData.Name,
                    ContractorId = parsedData.ContractorId,
                    CategoryId = parsedData.CategoryId,
                    Number = parsedData.Number,
                    Date = parsedData.Date,
                    Description = parsedData.Description,
                    Net = parsedData.Net,
                    Gross = parsedData.Gross
                };

                ProjectCostListItemWeb projectCostResult = await mediator.Send(createProjectCostCommand, cancellationToken);
                return projectCostResult.Id;
            }

            TrackedCostContextDto? context = DeserializeTrackedContext(batch.TrackedCostContextJson);

            CreateTrackedCostCommand createTrackedCostCommand = new CreateTrackedCostCommand
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                Name = parsedData.Name,
                ContractorId = parsedData.ContractorId,
                CategoryId = parsedData.CategoryId,
                Number = parsedData.Number,
                Date = parsedData.Date,
                Description = parsedData.Description,
                Net = parsedData.Net,
                Gross = parsedData.Gross,
                CostEstimateItemId = context?.CostEstimateItemId,
                WorkScheduleStageWorkId = context?.WorkScheduleStageWorkId
            };

            Business.Interfaces.WebModels.CostTrackers.TrackedCostWeb trackedCostResult =
                await mediator.Send(createTrackedCostCommand, cancellationToken);
            return trackedCostResult.Id;
        }

        private async Task EnsureBatchAccessAsync(
            AICostImportBatch batch,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            string permission = batch.CostDocumentType == EntityCostDocumentType.ProjectCost
                ? PermissionCodes.ProjectCosts
                : PermissionCodes.ProjectDashboardTracker;

            bool authorized = await accessService.AuthorizeAsync(
                currentUser,
                permission,
                new ResourceRef(TenantId: tenantId, ProjectId: projectId),
                cancellationToken: cancellationToken);

            if (!authorized)
            {
                throw new ForbiddenApiException("You do not have permission to accept this import item.");
            }
        }

        private static ParsedCostDto? DeserializeParsedData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ParsedCostDto>(json);
            }
            catch
            {
                return null;
            }
        }

        private static TrackedCostContextDto? DeserializeTrackedContext(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<TrackedCostContextDto>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
