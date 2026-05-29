using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.SubmitProjectCostForApproval
{
    public sealed class SubmitProjectCostForApprovalCommandHandler : IRequestHandler<SubmitProjectCostForApprovalCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IProjectCostAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<SubmitProjectCostForApprovalCommandHandler> logger;

        public SubmitProjectCostForApprovalCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IProjectCostAccessService accessService,
            ICurrentUser currentUser,
            ILogger<SubmitProjectCostForApprovalCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.accessService = accessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ProjectCostListItemWeb> Handle(
            SubmitProjectCostForApprovalCommand request,
            CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateAsync(request);

            bool hasWriteAccess = await accessService.HasWriteAccessAsync(
                projectCost, currentUser.Id, cancellationToken);

            if (!hasWriteAccess)
            {
                throw new ForbiddenApiException("You do not have permission to submit this cost for approval.");
            }

            if (projectCost.ApprovalStatus != CostApprovalStatus.Draft)
            {
                throw new ValidationApiException("Only Draft costs can be submitted for approval.");
            }

            projectCost.ApprovalStatus = CostApprovalStatus.PendingApproval;
            projectCost.UpdatedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} submitted for approval in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return MapToWeb(projectCost);
        }

        private async Task<ProjectCost> GetAndValidateAsync(
            SubmitProjectCostForApprovalCommand request)
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
