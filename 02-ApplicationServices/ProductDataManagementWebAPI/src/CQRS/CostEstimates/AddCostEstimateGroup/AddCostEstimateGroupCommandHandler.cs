using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.AddCostEstimateGroup
{
    public sealed class AddCostEstimateGroupCommandHandler
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
            CostEstimate costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            accessLevel.EnsureCanModifyStructure();

            int level = 0;
            if (request.ParentGroupId.HasValue)
            {
                Dictionary<Guid, CostEstimateGroup> groupsDict = await cacheService.GetGroupsDictionaryAsync(
                    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

                if (!groupsDict.TryGetValue(request.ParentGroupId.Value, out CostEstimateGroup? parentGroup))
                {
                    throw new NotFoundApiException("ParentGroup", request.ParentGroupId.Value.ToString());
                }

                level = parentGroup.Level + 1;
            }

            CostEstimateGroup group = new CostEstimateGroup
            {
                CostEstimateId = costEstimate.Id,
                Name = string.Empty,
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


