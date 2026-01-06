using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.DeleteProjectCost
{
    public class DeleteProjectCostCommandHandler : IRequestHandler<DeleteProjectCostCommand, Unit>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteProjectCostCommandHandler> logger;

        public DeleteProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            ICurrentUser currentUser,
            ILogger<DeleteProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteProjectCostCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify cost exists and belongs to the correct project/tenant
            var projectCost = await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId 
                    && pc.TenantId == request.TenantId 
                    && pc.ProjectId == request.ProjectId 
                    && !pc.IsDeleted)
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR cost owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isCostOwner = projectCost.UserId == currentUser.Id;
            
            if (!isAdmin && !isCostOwner)
            {
                throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
            }

            // 3. Soft delete
            projectCost.IsDeleted = true;
            projectCost.DeletedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);

            logger.LogInformation(
                "Cost {CostId} deleted from project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }
    }
}
