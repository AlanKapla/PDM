using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.ProjectCosts.Shared;
using Entities.Models;
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
            ICurrentUser currentUser,
            ILogger<DeleteProjectCostCommandHandler> logger)
            : base(blobStorageService)
        {
            this.projectCostRepo = projectCostRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteProjectCostCommand request, CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateProjectCostAsync(request, cancellationToken);

            await ValidateDeleteAccessAsync(projectCost, request, cancellationToken);

            projectCost.IsDeleted = true;
            projectCost.DeletedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);

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
                    && pc.ProjectId == request.ProjectId
                    && !pc.IsDeleted)
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
