using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.Shared;
using CQRS.Projects.Shared;
using Entities.Models.Costs;
using Entities.Models.Projects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.UpdateProjectCost
{
    public sealed class UpdateProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<UpdateProjectCostCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IReadRepository<ProjectCostCategory> categoryRepo;
        private readonly IProjectCostAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly IContractorService contractorService;
        private readonly ILogger<UpdateProjectCostCommandHandler> logger;

        public UpdateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IReadRepository<ProjectCostCategory> categoryRepo,
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
            this.categoryRepo = categoryRepo;
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

            await ProjectCostCategoryValidation.ValidateCategoryBelongsToProjectAsync(
                request.CategoryId, request.ProjectId, categoryRepo, cancellationToken);

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

            string? categoryName = null;
            string? categoryColor = null;
            if (projectCost.CategoryId.HasValue)
            {
                ProjectCostCategory? category = await categoryRepo.GetFirstBySearch(
                    c => c.Id == projectCost.CategoryId.Value && c.ProjectId == request.ProjectId);
                if (category is not null)
                {
                    categoryName = category.Name;
                    categoryColor = category.Color;
                }
            }

            return MapToWeb(projectCost, contractorName, categoryName, categoryColor);
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
            projectCost.CategoryId = request.CategoryId;
            projectCost.Number = request.Number;
            projectCost.Date = request.Date?.Date;
            projectCost.Description = request.Description;
            projectCost.Net = request.Net;
            projectCost.Gross = request.Gross ?? request.Net;
            projectCost.UpdatedAt = DateTime.UtcNow;
        }

        private ProjectCostListItemWeb MapToWeb(
            ProjectCost projectCost,
            string? contractorName,
            string? categoryName,
            string? categoryColor)
        {
            return new ProjectCostListItemWeb
            {
                Id = projectCost.Id,
                UserId = projectCost.UserId,
                UserName = currentUser.FullName,
                Name = projectCost.Name,
                ContractorId = projectCost.ContractorId,
                ContractorName = contractorName,
                CategoryId = projectCost.CategoryId,
                CategoryName = categoryName,
                CategoryColor = categoryColor,
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

                BaseCostAttachment attachment = await UploadBlobAndBuildAttachmentAsync(
                    projectCost, fileToUpload, cancellationToken);

                await PersistAttachmentAsync(attachment, cancellationToken);

                logger.LogInformation(
                    "Document uploaded for cost {CostId} in project {ProjectId}",
                    request.CostId, request.ProjectId);
            }
        }
    }
}
