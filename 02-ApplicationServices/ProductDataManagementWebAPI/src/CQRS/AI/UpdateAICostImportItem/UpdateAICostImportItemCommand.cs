using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.Shared;
using Entities.Enums;
using Entities.Models.AI;
using CQRS.Extensions;
using FluentValidation;
using MediatR;
using Repositories.Repository.Interfaces;
using System.Text.Json;

namespace CQRS.AI.UpdateAICostImportItem
{
    public sealed record UpdateAICostImportItemCommand : IRequestCommand<AICostImportItemWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid ItemId { get; init; }
        public required ParsedCostDto ParsedData { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public sealed class UpdateAICostImportItemCommandValidator
        : AbstractValidator<UpdateAICostImportItemCommand>
    {
        public UpdateAICostImportItemCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.ItemId).RequiredId();
            RuleFor(x => x.ParsedData).NotNull();
            RuleFor(x => x.ParsedData.Name).NotEmpty().MaximumLength(500);
        }
    }

    public sealed class UpdateAICostImportItemCommandHandler
        : IRequestHandler<UpdateAICostImportItemCommand, AICostImportItemWeb>
    {
        private readonly IRepository<AICostImportItem> itemRepo;
        private readonly IReadRepository<AICostImportBatch> batchRepo;
        private readonly IAICostImportBlobService blobService;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;

        public UpdateAICostImportItemCommandHandler(
            IRepository<AICostImportItem> itemRepo,
            IReadRepository<AICostImportBatch> batchRepo,
            IAICostImportBlobService blobService,
            IAccessService accessService,
            ICurrentUser currentUser)
        {
            this.itemRepo = itemRepo;
            this.batchRepo = batchRepo;
            this.blobService = blobService;
            this.accessService = accessService;
            this.currentUser = currentUser;
        }

        public async Task<AICostImportItemWeb> Handle(
            UpdateAICostImportItemCommand request,
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
                    "Only pending or error items can be updated.");
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

            item.ParsedDataJson = JsonSerializer.Serialize(request.ParsedData);
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await itemRepo.Update(item);
            await itemRepo.SaveChangesAsync(cancellationToken);

            return AICostImportMapper.MapItemToWeb(item, batch, blobService);
        }

        private async Task EnsureBatchAccessAsync(
            AICostImportBatch batch,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            string permission = batch.CostDocumentType == Entities.Enums.CostDocumentType.ProjectCost
                ? PermissionCodes.ProjectCosts
                : PermissionCodes.ProjectDashboardTracker;

            bool authorized = await accessService.AuthorizeAsync(
                currentUser,
                permission,
                new ResourceRef(TenantId: tenantId, ProjectId: projectId),
                cancellationToken: cancellationToken);

            if (!authorized)
            {
                throw new ForbiddenApiException("You do not have permission to update this import item.");
            }
        }
    }
}
