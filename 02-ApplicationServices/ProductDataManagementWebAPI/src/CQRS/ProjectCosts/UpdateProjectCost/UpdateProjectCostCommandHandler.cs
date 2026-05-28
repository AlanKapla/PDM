using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.Shared;
using Entities.Models.Costs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
            bool canEditOnlyIsClosed = await ValidateAccessAndGetPermissionsAsync(request, projectCost, cancellationToken);

            if (canEditOnlyIsClosed)
            {
                await HandleSharedUserUpdateAsync(request, projectCost, cancellationToken);
            }
            else
            {
                ApplyFieldUpdates(request, projectCost);
                await HandleDocumentOperationsAsync(request, projectCost, cancellationToken);

                await projectCostRepo.Update(projectCost);
                await projectCostRepo.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Cost {CostId} fully updated in project {ProjectId} by user {UserId}",
                    request.CostId, request.ProjectId, currentUser.Id);
            }

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
                    && pc.ProjectId == request.ProjectId,
                q => q.Include(pc => pc.SharedWith))
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
        }

        private async Task<bool> ValidateAccessAndGetPermissionsAsync(
            UpdateProjectCostCommand request,
            ProjectCost projectCost,
            CancellationToken cancellationToken)
        {
            bool hasWriteAccess = await accessService.HasWriteAccessAsync(
                projectCost, currentUser.Id, cancellationToken);

            if (hasWriteAccess)
            {
                return false; // full edit allowed
            }

            // Check if user can accept/reject: needs COSTS.ACCEPT permission + share access to this specific cost
            ProjectCtxSnapshot? projectSnapshot = await currentUser.GetProjectSnapshotAsync(
                projectCost.ProjectId, cancellationToken);

            bool hasAcceptPermission = projectSnapshot is not null
                && projectSnapshot.ProjectPermissionCodes.Contains(PermissionCodes.ProjectCosts);

            bool hasShareAccess = await accessService.HasShareAccessAsync(
                projectCost, currentUser.Id, cancellationToken);

            if (hasAcceptPermission && hasShareAccess)
            {
                return true; // limited edit: only IsAccepted
            }

            throw new ForbiddenApiException("You do not have permission to update this cost.");
        }

        private async Task HandleSharedUserUpdateAsync(
            UpdateProjectCostCommand request,
            ProjectCost projectCost,
            CancellationToken cancellationToken)
        {
            projectCost.IsAccepted = request.IsAccepted;
            projectCost.UpdatedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} IsAccepted updated to {IsAccepted} in project {ProjectId} by shared user {UserId}",
                request.CostId, request.IsAccepted, request.ProjectId, currentUser.Id);
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
            projectCost.IsAccepted = request.IsAccepted;
            projectCost.UpdatedAt = DateTime.UtcNow;
        }

        private ProjectCostListItemWeb MapToWeb(ProjectCost projectCost, string? contractorName)
        {
            List<Guid> sharedWithUserIds = projectCost.SharedWith
                ?.Select(spc => spc.SharedWithUserId)
                .ToList() ?? new List<Guid>();

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
                IsAccepted = projectCost.IsAccepted,
                HasDocument = false,
                DocumentFileName = null,
                PreviewSasUrl = null,
                DownloadSasUrl = null,
                SharedWithUserIds = sharedWithUserIds,
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

                try
                {
                    await UploadDocumentToCostAsync(projectCost, fileToUpload, cancellationToken);

                    logger.LogInformation(
                        "Document uploaded for cost {CostId} in project {ProjectId}",
                        request.CostId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to upload document for cost {CostId}",
                        request.CostId);

                    throw new ValidationApiException("Cost updated but document upload failed");
                }
            }
        }
    }
}
