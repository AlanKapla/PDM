using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.AddCostEstimateGroup
{
    public class AddCostEstimateGroupCommandHandler
        : IRequestHandler<AddCostEstimateGroupCommand, Guid>
    {
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public AddCostEstimateGroupCommandHandler(
            IRepository<CostEstimateGroup> groupRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.groupRepository = groupRepository;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            AddCostEstimateGroupCommand request,
            CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());


            var accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");

            if (accessLevel == CostEstimateAccessLevel.Restricted)
                throw new ForbiddenApiException("Shared users cannot modify the cost estimate structure.");

            if (accessLevel == CostEstimateAccessLevel.ReadOnly)
                throw new ForbiddenApiException("Read-only access does not allow modifying the cost estimate structure.");

            var template = await cacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            if (!template.CanAddGroups)
            {
                throw new ValidationApiException("Template does not allow adding new groups");
            }

            int level = 0;
            if (request.ParentGroupId.HasValue)
            {
                var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

                if (!groupsDict.TryGetValue(request.ParentGroupId.Value, out var parentGroup))
                {
                    throw new NotFoundApiException("ParentGroup", request.ParentGroupId.Value.ToString());
                }

                level = parentGroup.Level + 1;

                if (!template.CanBranchGroups)
                {
                    throw new ValidationApiException("Template does not allow branching groups (subgroups)");
                }

                if (template.MaxGroupLevel.HasValue && level > template.MaxGroupLevel.Value)
                {
                    throw new ValidationApiException(
                        $"Group level {level} exceeds maximum allowed level {template.MaxGroupLevel.Value}");
                }
            }

            var group = new CostEstimateGroup
            {
                CostEstimateId = costEstimate.Id,
                ParentGroupId = request.ParentGroupId,
                Level = level,
                Order = request.Order,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await groupRepository.Insert(group);

            await cacheService.InvalidateGroupsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return group.Id;
        }
    }
}
