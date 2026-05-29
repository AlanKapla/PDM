using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.WithdrawProjectCost
{
    public sealed class WithdrawProjectCostCommandHandler : IRequestHandler<WithdrawProjectCostCommand, ProjectCostListItemWeb>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IProjectCostAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<WithdrawProjectCostCommandHandler> logger;

        public WithdrawProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IProjectCostAccessService accessService,
            ICurrentUser currentUser,
            ILogger<WithdrawProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.accessService = accessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ProjectCostListItemWeb> Handle(
            WithdrawProjectCostCommand request,
            CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateAsync(request);

            bool hasWriteAccess = await accessService.HasWriteAccessAsync(
                projectCost, currentUser.Id, cancellationToken);

            if (!hasWriteAccess)
            {
                throw new ForbiddenApiException("You do not have permission to withdraw this cost.");
            }

            if (projectCost.ApprovalStatus != CostApprovalStatus.PendingApproval)
            {
                throw new ValidationApiException("Only PendingApproval costs can be withdrawn.");
            }

            projectCost.ApprovalStatus = CostApprovalStatus.Draft;
            projectCost.ApprovedByUserId = null;
            projectCost.ApprovedAt = null;
            projectCost.UpdatedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} withdrawn from approval in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return MapToWeb(projectCost);
        }

        private async Task<ProjectCost> GetAndValidateAsync(
            WithdrawProjectCostCommand request)
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
