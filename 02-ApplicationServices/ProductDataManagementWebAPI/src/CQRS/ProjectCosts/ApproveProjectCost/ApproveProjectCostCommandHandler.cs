using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.ApproveProjectCost
{
    public sealed class ApproveProjectCostCommandHandler : IRequestHandler<ApproveProjectCostCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ApproveProjectCostCommandHandler> logger;

        public ApproveProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            ICurrentUser currentUser,
            ILogger<ApproveProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ProjectCostListItemWeb> Handle(
            ApproveProjectCostCommand request,
            CancellationToken cancellationToken)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            if (!isAdmin)
            {
                throw new ForbiddenApiException("Only administrators can approve costs.");
            }

            ProjectCost projectCost = await GetAndValidateAsync(request, cancellationToken);

            if (projectCost.ApprovalStatus != CostApprovalStatus.PendingApproval)
            {
                throw new ValidationApiException("Only PendingApproval costs can be approved.");
            }

            projectCost.ApprovalStatus = CostApprovalStatus.Approved;
            projectCost.ApprovedByUserId = currentUser.Id;
            projectCost.ApprovedAt = DateTime.UtcNow;
            projectCost.UpdatedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} approved in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return MapToWeb(projectCost);
        }

        private async Task<ProjectCost> GetAndValidateAsync(
            ApproveProjectCostCommand request,
            CancellationToken cancellationToken)
        {
            return await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId
                    && pc.TenantId == request.TenantId
                    && pc.ProjectId == request.ProjectId
                    && !pc.IsDeleted)
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
        }

        private ProjectCostListItemWeb MapToWeb(ProjectCost pc)
        {
            return new ProjectCostListItemWeb
            {
                Id = pc.Id,
                UserId = pc.UserId,
                UserName = currentUser.FullName,
                Name = pc.Name,
                ContractorId = pc.ContractorId,
                ContractorName = null,
                Number = pc.Number,
                Date = pc.Date,
                Description = pc.Description,
                Net = pc.Net,
                Gross = pc.Gross,
                ApprovalStatus = pc.ApprovalStatus,
                ApprovedByUserId = pc.ApprovedByUserId,
                ApprovedAt = pc.ApprovedAt,
                HasDocument = false,
                DocumentFileName = null,
                PreviewSasUrl = null,
                DownloadSasUrl = null,
                CreatedAt = pc.CreatedAt
            };
        }
    }
}
