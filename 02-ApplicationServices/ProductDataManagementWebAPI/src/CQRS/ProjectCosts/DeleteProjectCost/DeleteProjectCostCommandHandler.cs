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
            // Get existing cost
            var projectCost = await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId 
                    && pc.TenantId == request.TenantId 
                    && pc.ProjectId == request.ProjectId 
                    && !pc.IsDeleted);

            if (projectCost == null)
            {
                throw new NotFoundApiException("ProjectCost", request.CostId.ToString());
            }

            // Verify ownership - only the user who created the cost can delete it
            if (projectCost.UserId != currentUser.Id)
            {
                throw new ForbiddenApiException("Only the cost owner can delete it");
            }

            // Soft delete
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
