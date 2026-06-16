using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteItemFile
{
    public sealed class DeleteItemFileCommandHandler : IRequestHandler<DeleteItemFileCommand, Unit>
    {
        private readonly IReadRepository<CostEstimateItemFile> itemFileReadRepo;
        private readonly IRepository<CostEstimateItemFile> itemFileRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteItemFileCommandHandler> logger;

        public DeleteItemFileCommandHandler(
            IReadRepository<CostEstimateItemFile> itemFileReadRepo,
            IRepository<CostEstimateItemFile> itemFileRepo,
            IBlobStorageService blobStorageService,
            ICostEstimateCacheService ceCacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser,
            ILogger<DeleteItemFileCommandHandler> logger)
        {
            this.itemFileReadRepo = itemFileReadRepo;
            this.itemFileRepo = itemFileRepo;
            this.blobStorageService = blobStorageService;
            this.ceCacheService = ceCacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteItemFileCommand request, CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request, cancellationToken);

            CostEstimateItemFile itemFile = await GetAndValidateFileAsync(request, cancellationToken);

            await SoftDeleteFileAsync(itemFile, cancellationToken);
            await DeleteBlobAsync(itemFile, cancellationToken);

            await ceCacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }

        private async Task ValidateAccessAsync(DeleteItemFileCommand request, CancellationToken cancellationToken)
        {
            CostEstimate? costEstimate = await ceCacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (costEstimate is null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
            {
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
            }

            if (accessLevel == CostEstimateAccessLevel.ReadOnly)
            {
                throw new ForbiddenApiException("Read-only access does not allow file deletions.");
            }
        }

        private async Task<CostEstimateItemFile> GetAndValidateFileAsync(
            DeleteItemFileCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateItemFile? itemFile = await itemFileReadRepo.GetFirstBySearch(
                f => f.Id == request.FileId &&
                     f.ItemId == request.ItemId &&
                     f.CostEstimateId == request.CostEstimateId,
                cancellationToken);

            if (itemFile is null)
            {
                throw new NotFoundApiException(nameof(CostEstimateItemFile), request.FileId.ToString());
            }

            return itemFile;
        }

        private async Task SoftDeleteFileAsync(CostEstimateItemFile itemFile, CancellationToken cancellationToken)
        {
            itemFile.IsDeleted = true;
            itemFile.DeletedAt = DateTime.UtcNow;

            await itemFileRepo.Update(itemFile);
            await itemFileRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Soft-deleted item file {FileId} (blob: {BlobName})",
                itemFile.Id, itemFile.BlobName);
        }

        private async Task DeleteBlobAsync(CostEstimateItemFile itemFile, CancellationToken cancellationToken)
        {
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);
            await blobStorageService.DeleteAsync(containerName, itemFile.BlobName, cancellationToken);
        }
    }
}
