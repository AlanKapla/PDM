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
        private readonly ICurrentUser currentUser;

        public AddCostEstimateGroupCommandHandler(
            IRepository<CostEstimateGroup> groupRepository,
            ICostEstimateCacheService cacheService,
            ICurrentUser currentUser)
        {
            this.groupRepository = groupRepository;
            this.cacheService = cacheService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            AddCostEstimateGroupCommand request,
            CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, currentUser.Id, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

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
