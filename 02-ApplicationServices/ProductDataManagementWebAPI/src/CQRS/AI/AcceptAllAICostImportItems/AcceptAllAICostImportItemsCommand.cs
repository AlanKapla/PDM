using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.AcceptAICostImportItem;
using Entities.Enums;
using Entities.Models.AI;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.AI.AcceptAllAICostImportItems
{
    public sealed record AcceptAllAICostImportItemsCommand : IRequestCommand<AICostImportAcceptAllResultWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public sealed class AcceptAllAICostImportItemsCommandHandler
        : IRequestHandler<AcceptAllAICostImportItemsCommand, AICostImportAcceptAllResultWeb>
    {
        private readonly IReadRepository<AICostImportItem> itemRepo;
        private readonly IReadRepository<AICostImportBatch> batchRepo;
        private readonly IMediator mediator;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;

        public AcceptAllAICostImportItemsCommandHandler(
            IReadRepository<AICostImportItem> itemRepo,
            IReadRepository<AICostImportBatch> batchRepo,
            IMediator mediator,
            IAccessService accessService,
            ICurrentUser currentUser)
        {
            this.itemRepo = itemRepo;
            this.batchRepo = batchRepo;
            this.mediator = mediator;
            this.accessService = accessService;
            this.currentUser = currentUser;
        }

        public async Task<AICostImportAcceptAllResultWeb> Handle(
            AcceptAllAICostImportItemsCommand request,
            CancellationToken cancellationToken)
        {
            IEnumerable<AICostImportItem> items = await itemRepo.GetBySearch(
                i => i.TenantId == request.TenantId
                     && i.ProjectId == request.ProjectId
                     && i.Status == AICostImportItemStatus.Pending);

            int acceptedCount = 0;
            int failedCount = 0;
            List<string> errors = new List<string>();

            foreach (AICostImportItem item in items)
            {
                AICostImportBatch? batch = await batchRepo.GetFirstBySearch(
                    b => b.Id == item.BatchId
                         && b.TenantId == request.TenantId
                         && b.ProjectId == request.ProjectId);

                if (batch is null)
                {
                    failedCount++;
                    errors.Add($"Item {item.Id}: batch not found.");
                    continue;
                }

                if (!await CanAcceptBatchAsync(batch, request, cancellationToken))
                {
                    continue;
                }

                try
                {
                    AcceptAICostImportItemCommand acceptCommand =
                        new AcceptAICostImportItemCommand
                        {
                            TenantId = request.TenantId,
                            ProjectId = request.ProjectId,
                            ItemId = item.Id
                        };

                    await mediator.Send(acceptCommand, cancellationToken);
                    acceptedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    errors.Add($"Item {item.Id}: {ex.Message}");
                }
            }

            return new AICostImportAcceptAllResultWeb
            {
                AcceptedCount = acceptedCount,
                FailedCount = failedCount,
                Errors = errors
            };
        }

        private async Task<bool> CanAcceptBatchAsync(
            AICostImportBatch batch,
            AcceptAllAICostImportItemsCommand request,
            CancellationToken cancellationToken)
        {
            string permission = batch.CostDocumentType == Entities.Enums.CostDocumentType.ProjectCost
                ? PermissionCodes.ProjectCosts
                : PermissionCodes.ProjectDashboardTracker;

            return await accessService.AuthorizeAsync(
                currentUser,
                permission,
                new ResourceRef(TenantId: request.TenantId, ProjectId: request.ProjectId),
                cancellationToken: cancellationToken);
        }
    }
}
