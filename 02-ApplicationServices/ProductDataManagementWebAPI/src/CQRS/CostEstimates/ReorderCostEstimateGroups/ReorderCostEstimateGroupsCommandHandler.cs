using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ReorderCostEstimateGroups
{
    public class ReorderCostEstimateGroupsCommandHandler : IRequestHandler<ReorderCostEstimateGroupsCommand, Unit>
    {
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public ReorderCostEstimateGroupsCommandHandler(
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

        public async Task<Unit> Handle(ReorderCostEstimateGroupsCommand request, CancellationToken cancellationToken)
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

            // Load all non-deleted groups from cache for validation
            var allGroupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            // Validate all requested groups exist in the cost estimate
            foreach (var dto in request.Groups)
            {
                if (!allGroupsDict.ContainsKey(dto.GroupId))
                {
                    throw new NotFoundApiException(nameof(CostEstimateGroup), dto.GroupId.ToString());
                }
            }

            // Validate parent references and template constraints
            var hasParentChanges = request.Groups.Any(dto =>
            {
                var group = allGroupsDict[dto.GroupId];
                return group.ParentGroupId != dto.ParentGroupId;
            });

            if (hasParentChanges)
            {
                foreach (var dto in request.Groups.Where(d => d.ParentGroupId.HasValue))
                {
                    if (!allGroupsDict.ContainsKey(dto.ParentGroupId!.Value))
                    {
                        throw new NotFoundApiException("ParentGroup", dto.ParentGroupId.Value.ToString());
                    }

                    if (!template.CanBranchGroups)
                    {
                        throw new ValidationApiException(
                            "Template does not allow branching groups (subgroups)");
                    }
                }
            }

            // Load tracked entities from DB for update
            var requestedGroupIds = request.Groups.Select(g => g.GroupId).ToHashSet();
            var groups = await groupRepository.GetBySearch(
                g => g.CostEstimateId == request.CostEstimateId &&
                     requestedGroupIds.Contains(g.Id));

            var trackedGroupsById = groups.ToDictionary(g => g.Id);
            var now = DateTime.UtcNow;

            foreach (var dto in request.Groups)
            {
                var group = trackedGroupsById[dto.GroupId];
                group.Order = dto.Order;
                group.ParentGroupId = dto.ParentGroupId;
                group.UpdatedAt = now;

                // Recalculate level
                int level = 0;
                if (dto.ParentGroupId.HasValue &&
                    allGroupsDict.TryGetValue(dto.ParentGroupId.Value, out var parent))
                {
                    level = parent.Level + 1;
                }

                if (template.MaxGroupLevel.HasValue && level > template.MaxGroupLevel.Value)
                {
                    throw new ValidationApiException(
                        $"Group {dto.GroupId}: Level {level} exceeds maximum allowed level {template.MaxGroupLevel.Value}");
                }

                group.Level = level;
            }

            await groupRepository.UpdateRange(groups);
            await groupRepository.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await cacheService.InvalidateGroupsAsync(request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
