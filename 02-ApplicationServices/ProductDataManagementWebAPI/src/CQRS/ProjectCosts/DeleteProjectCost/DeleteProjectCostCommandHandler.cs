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

namespace CQRS.ProjectCosts.DeleteProjectCost
{
    public class DeleteProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<DeleteProjectCostCommand, Unit>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteProjectCostCommandHandler> logger;

        public DeleteProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            ICurrentUser currentUser,
            ILogger<DeleteProjectCostCommandHandler> logger)
            : base(blobStorageService, attachmentRepository)
        {
            this.projectCostRepo = projectCostRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteProjectCostCommand request, CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateProjectCostAsync(request, cancellationToken);

            await ValidateDeleteAccessAsync(projectCost, request, cancellationToken);

            await RemoveAttachmentsAsync(projectCost.Id, cancellationToken);

            projectCost.IsDeleted = true;
            projectCost.DeletedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} deleted from project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }

        private async Task<ProjectCost> GetAndValidateProjectCostAsync(
            DeleteProjectCostCommand request,
            CancellationToken cancellationToken)
        {
            return await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId
                    && pc.TenantId == request.TenantId
                    && pc.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
        }

        private async Task ValidateDeleteAccessAsync(
            ProjectCost projectCost,
            DeleteProjectCostCommand request,
            CancellationToken cancellationToken)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isCostOwner = projectCost.UserId == currentUser.Id;

            if (!isAdmin && !isCostOwner)
            {
                throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
            }
        }
    }
}
