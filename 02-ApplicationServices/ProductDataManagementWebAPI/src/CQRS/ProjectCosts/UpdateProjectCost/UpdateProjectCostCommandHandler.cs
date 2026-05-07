using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.ProjectCosts.Shared;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.Costs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.UpdateProjectCost
{
    public class UpdateProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<UpdateProjectCostCommand, Unit>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IReadRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateProjectCostCommandHandler> logger;

        public UpdateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IReadRepository<SharedProjectCost> sharedProjectCostRepo,
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            ICurrentUser currentUser,
            ILogger<UpdateProjectCostCommandHandler> logger)
            : base(blobStorageService, attachmentRepository)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateProjectCostCommand request, CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateProjectCostAsync(request, cancellationToken);
            bool canEditOnlyIsClosed = await ValidateAccessAndGetPermissionsAsync(request, projectCost, cancellationToken);

            if (canEditOnlyIsClosed)
            {
                await HandleSharedUserUpdateAsync(request, projectCost, cancellationToken);
                return Unit.Value;
            }

            ApplyFieldUpdates(request, projectCost);
            await HandleDocumentOperationsAsync(request, projectCost, cancellationToken);

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} fully updated in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return Unit.Value;
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

        private async Task<bool> ValidateAccessAndGetPermissionsAsync(
            UpdateProjectCostCommand request,
            ProjectCost projectCost,
            CancellationToken cancellationToken)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isCostOwner = projectCost.UserId == currentUser.Id;
            bool hasShareAccess = false;

            if (!isAdmin && !isCostOwner)
            {
                SharedProjectCost? share = await sharedProjectCostRepo.GetFirstBySearch(
                    spc => spc.ProjectCostId == request.CostId
                        && spc.SharedWithUserId == currentUser.Id);

                hasShareAccess = share is not null;

                if (!hasShareAccess)
                {
                    throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
                }
            }

            bool canEditAllFields = isAdmin || isCostOwner;
            return hasShareAccess && !canEditAllFields;
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
            projectCost.Place = request.Place;
            projectCost.Date = request.Date.Date;
            projectCost.Description = request.Description;
            projectCost.Net = request.NetAmount;
            projectCost.Gross = request.GrossAmount ?? request.NetAmount;
            projectCost.IsAccepted = request.IsAccepted;
            projectCost.UpdatedAt = DateTime.UtcNow;
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
