using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.RejectProjectCost
{
    public sealed class RejectProjectCostCommandHandler : IRequestHandler<RejectProjectCostCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<RejectProjectCostCommandHandler> logger;

        public RejectProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            ICurrentUser currentUser,
            ILogger<RejectProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ProjectCostListItemWeb> Handle(
            RejectProjectCostCommand request,
            CancellationToken cancellationToken)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            if (!isAdmin)
            {
                throw new ForbiddenApiException("Only administrators can reject costs.");
            }

            ProjectCost projectCost = await GetAndValidateAsync(request, cancellationToken);

            if (projectCost.ApprovalStatus != CostApprovalStatus.PendingApproval)
            {
                throw new ValidationApiException("Only PendingApproval costs can be rejected.");
            }

            projectCost.ApprovalStatus = CostApprovalStatus.Draft;
            projectCost.ApprovedByUserId = null;
            projectCost.ApprovedAt = null;
            projectCost.UpdatedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} rejected in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return MapToWeb(projectCost);
        }

        private async Task<ProjectCost> GetAndValidateAsync(
            RejectProjectCostCommand request,
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
