using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.ProjectCosts.Shared;
using Entities.Models;
using Entities.Models.CostTrackers;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    public class CreateProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<CreateProjectCostCommand, Guid>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<CreateProjectCostCommandHandler> logger;

        public CreateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<CreateProjectCostCommandHandler> logger,
            IReadRepository<CostTracker> costTrackerRepository,
            IRepository<TrackedCost> trackedCostRepository,
            IRepository<ProjectCostTrackedCostLink> projectCostLinkRepository,
            IRepository<TrackedCostAttachment> attachmentRepository)
            : base(costTrackerRepository, trackedCostRepository, projectCostLinkRepository, blobStorageService, attachmentRepository)
        {
            this.projectCostRepo = projectCostRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Guid> Handle(CreateProjectCostCommand request, CancellationToken cancellationToken)
        {
            ProjectCost projectCost = BuildProjectCostEntity(request);

            await projectCostRepo.Insert(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            if (request.Document != null)
            {
                await UploadCostDocumentAsync(request, projectCost, cancellationToken);
            }

            if (request.IsClosed)
            {
                await CreateTrackerLinkAsync(projectCost, request.TenantId, request.ProjectId, cancellationToken);
                await projectCostRepo.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "Cost {CostId} created in project {ProjectId} by user {UserId}",
                projectCost.Id, request.ProjectId, currentUser.Id);

            return projectCost.Id;
        }

        private ProjectCost BuildProjectCostEntity(CreateProjectCostCommand request)
        {
            return new ProjectCost
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = currentUser.Id,
                Name = request.Name,
                Place = request.Place,
                Date = request.Date.Date,
                Description = request.Description,
                NetAmount = request.NetAmount,
                GrossAmount = request.GrossAmount ?? request.NetAmount!.Value,
                IsClosed = request.IsClosed,
                HasDocument = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        private async Task UploadCostDocumentAsync(CreateProjectCostCommand request, ProjectCost projectCost, CancellationToken cancellationToken)
        {
            try
            {
                await UploadDocumentToCostAsync(projectCost, request.Document!, request.TenantId, projectCost.Id, cancellationToken);

                await projectCostRepo.Update(projectCost);
                await projectCostRepo.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Document uploaded for cost {CostId} in project {ProjectId}",
                    projectCost.Id, request.ProjectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to upload document for cost {CostId} in project {ProjectId}",
                    projectCost.Id, request.ProjectId);

                throw new ValidationApiException("Cost created but document upload failed");
            }
        }
    }
}
