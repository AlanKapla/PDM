using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.Shared;
using Entities.Enums;
using Entities.Models.AI;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.AI.GetAICostImportItem
{
    public sealed record GetAICostImportItemQuery : IRequestQuery<AICostImportItemWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid ItemId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public sealed class GetAICostImportItemQueryHandler
        : IRequestHandler<GetAICostImportItemQuery, AICostImportItemWeb>
    {
        private readonly IReadRepository<AICostImportItem> itemRepo;
        private readonly IReadRepository<AICostImportBatch> batchRepo;
        private readonly IAICostImportBlobService blobService;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;

        public GetAICostImportItemQueryHandler(
            IReadRepository<AICostImportItem> itemRepo,
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
            GetAICostImportItemQuery request,
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

            AICostImportBatch? batch = await batchRepo.GetFirstBySearch(
                b => b.Id == item.BatchId
                     && b.TenantId == request.TenantId
                     && b.ProjectId == request.ProjectId);

            if (batch is null)
            {
                throw new NotFoundApiException(nameof(AICostImportBatch), item.BatchId.ToString());
            }

            await EnsureBatchAccessAsync(batch, request, cancellationToken);

            return AICostImportMapper.MapItemToWeb(item, batch, blobService);
        }

        private async Task EnsureBatchAccessAsync(
            AICostImportBatch batch,
            GetAICostImportItemQuery request,
            CancellationToken cancellationToken)
        {
            string permission = batch.CostDocumentType == Entities.Enums.CostDocumentType.ProjectCost
                ? PermissionCodes.ProjectCosts
                : PermissionCodes.ProjectDashboardTracker;

            bool authorized = await accessService.AuthorizeAsync(
                currentUser,
                permission,
                new ResourceRef(TenantId: request.TenantId, ProjectId: request.ProjectId),
                cancellationToken: cancellationToken);

            if (!authorized)
            {
                throw new ForbiddenApiException("You do not have permission to view this import item.");
            }
        }
    }
}
