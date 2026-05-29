using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.Shared;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    public sealed class CreateProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<CreateProjectCostCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly ICurrentUser currentUser;
        private readonly IContractorService contractorService;
        private readonly ILogger<CreateProjectCostCommandHandler> logger;

        public CreateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            IContractorService contractorService,
            ICurrentUser currentUser,
            ILogger<CreateProjectCostCommandHandler> logger,
            ILogger<ProjectCostHandlerBase> baseLogger)
            : base(blobStorageService, attachmentRepository, baseLogger)
        {
            this.projectCostRepo = projectCostRepo;
            this.contractorService = contractorService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ProjectCostListItemWeb> Handle(CreateProjectCostCommand request, CancellationToken cancellationToken)
        {
            ProjectCost projectCost = BuildProjectCostEntity(request);

            // Upload the blob BEFORE any DB write so a failed upload throws without
            // leaving an orphan ProjectCost row that would require compensating delete.
            BaseCostAttachment? pendingAttachment = null;
            if (request.Document is not null)
            {
                pendingAttachment = await UploadCostDocumentAsync(request, projectCost, cancellationToken);
            }

            await projectCostRepo.Insert(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            if (pendingAttachment is not null)
            {
                await PersistAttachmentAsync(pendingAttachment, cancellationToken);
            }

            logger.LogInformation(
                "Cost {CostId} created in project {ProjectId} by user {UserId}",
                projectCost.Id, request.ProjectId, currentUser.Id);

            string? contractorName = null;
            if (projectCost.ContractorId.HasValue)
            {
                Dictionary<Guid, string> names = await contractorService.GetNamesByIdsAsync(
                    new[] { projectCost.ContractorId.Value }, request.TenantId, cancellationToken);
                contractorName = names.GetValueOrDefault(projectCost.ContractorId.Value);
            }

            return MapToWeb(projectCost, pendingAttachment, contractorName);
        }

        private ProjectCost BuildProjectCostEntity(CreateProjectCostCommand request)
        {
            return new ProjectCost
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = currentUser.Id,
                Name = request.Name,
                ContractorId = request.ContractorId,
                Number = request.Number,
                Date = request.Date?.Date,
                Description = request.Description,
                Net = request.Net,
                Gross = request.Gross ?? request.Net,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        private ProjectCostListItemWeb MapToWeb(ProjectCost projectCost, BaseCostAttachment? attachment, string? contractorName)
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
                HasDocument = attachment is not null,
                DocumentFileName = attachment?.OriginalFileName,
                PreviewSasUrl = null,
                DownloadSasUrl = null,
                CreatedAt = projectCost.CreatedAt
            };
        }

        private async Task<BaseCostAttachment> UploadCostDocumentAsync(
            CreateProjectCostCommand request,
            ProjectCost projectCost,
            CancellationToken cancellationToken)
        {
            try
            {
                BaseCostAttachment attachment = await UploadBlobAndBuildAttachmentAsync(
                    projectCost, request.Document!, cancellationToken);

                logger.LogInformation(
                    "Document uploaded to blob storage for cost {CostId} in project {ProjectId}",
                    projectCost.Id, request.ProjectId);

                return attachment;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to upload document for cost {CostId} in project {ProjectId}",
                    projectCost.Id, request.ProjectId);

                throw new ValidationApiException("Document upload failed; cost was not created.");
            }
        }
    }
}
