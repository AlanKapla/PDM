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
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    public class CreateProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<CreateProjectCostCommand, Guid>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<CreateProjectCostCommandHandler> logger;

        public CreateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            ICurrentUser currentUser,
            ILogger<CreateProjectCostCommandHandler> logger)
            : base(blobStorageService, attachmentRepository)
        {
            this.projectCostRepo = projectCostRepo;
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
                Net = request.NetAmount,
                Gross = request.GrossAmount ?? request.NetAmount,
                IsAccepted = request.IsAccepted,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        private async Task UploadCostDocumentAsync(
            CreateProjectCostCommand request,
            ProjectCost projectCost,
            CancellationToken cancellationToken)
        {
            try
            {
                await UploadDocumentToCostAsync(projectCost, request.Document!, cancellationToken);

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
