using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Handler to get cost estimates based on scope (All, Mine, Shared)
    /// Only returns cost estimates where template is NOT deleted
    /// </summary>
    public sealed class GetCostEstimatesQueryHandler : IRequestHandler<GetCostEstimatesQuery, List<CostEstimateListItemWeb>>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<SharedCostEstimate> sharedCeRepository;
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly IUserService userService;
        private readonly ICurrentUser currentUser;
        private readonly IReadRepository<ProjectCurrency> projectCurrencyRepository;

        public GetCostEstimatesQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<SharedCostEstimate> sharedCeRepository,
            ICostEstimateCacheService ceCacheService,
            IUserService userService,
            ICurrentUser currentUser,
            IReadRepository<ProjectCurrency> projectCurrencyRepository)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.sharedCeRepository = sharedCeRepository;
            this.ceCacheService = ceCacheService;
            this.userService = userService;
            this.currentUser = currentUser;
            this.projectCurrencyRepository = projectCurrencyRepository;
        }

        public async Task<List<CostEstimateListItemWeb>> Handle(GetCostEstimatesQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<CostEstimate> costEstimates;

            switch (request.Scope)
            {
                case ResourceScope.All:
                    costEstimates = await costEstimateRepository.GetBySearch(
                        c => c.ProjectId == request.ProjectId &&
                             c.TenantId == request.TenantId);
                    break;

                case ResourceScope.Mine:
                    costEstimates = await costEstimateRepository.GetBySearch(
                        c => c.ProjectId == request.ProjectId &&
                             c.TenantId == request.TenantId &&
                             c.OwnerId == currentUser.Id);
                    break;

                case ResourceScope.Shared:
                    HashSet<Guid> sharedCeIds = await sharedCeRepository.SelectToHashSetAsync(
                        s => s.ProjectId == request.ProjectId &&
                             s.TenantId == request.TenantId &&
                             s.SharedWithUserId == currentUser.Id,
                        s => s.CostEstimateId,
                        cancellationToken);

                    costEstimates = sharedCeIds.Count == 0
                        ? Enumerable.Empty<CostEstimate>()
                        : await costEstimateRepository.GetBySearch(
                            c => sharedCeIds.Contains(c.Id));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }

            List<CostEstimate> costEstimatesList = costEstimates.ToList();

            if (costEstimatesList.Count == 0)
            {
                return [];
            }

            ProjectCurrency? projectCurrency = await projectCurrencyRepository.GetFirstBySearch(
                c => c.ProjectId == request.ProjectId,
                cancellationToken);

            // Template logic removed - all cost estimates use schema-based structure

            var membersDict = (await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken))
                .ToDictionary(m => m.UserId);

            // For Mine scope: batch-load all shares grouped by CE ID
            Dictionary<Guid, List<CostEstimateShareWeb>> sharesByCeId = [];

            if (request.Scope != ResourceScope.Shared && costEstimatesList.Count > 0)
            {
                HashSet<Guid> mineIds = costEstimatesList.Select(c => c.Id).ToHashSet();

                List<SharedCostEstimate> allShares = (await sharedCeRepository.GetBySearch(
                    s => mineIds.Contains(s.CostEstimateId))).ToList();

                sharesByCeId = allShares
                    .GroupBy(s => s.CostEstimateId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(s =>
                        {
                            membersDict.TryGetValue(s.SharedWithUserId, out var m);
                            return new CostEstimateShareWeb(
                                UserId: s.SharedWithUserId,
                                FullName: m?.FullName ?? "Unknown",
                                Email: m?.Email ?? string.Empty,
                                SharedAt: s.SharedAt
                            );
                        }).OrderBy(sw => sw.FullName).ToList());
            }

            return costEstimatesList
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CostEstimateListItemWeb(
                    Id: c.Id,
                    TenantId: c.TenantId,
                    ProjectId: c.ProjectId,
                    Name: c.Name,
                    Description: c.Description,
                    Status: c.Status,
                    TotalNet: c.TotalNet,
                    TotalGross: c.TotalGross,
                    TotalVat: c.TotalVat,
                    CreatedAt: c.CreatedAt,
                    UpdatedAt: c.UpdatedAt,
                    OwnerId: c.OwnerId,
                    OwnerName: membersDict.TryGetValue(c.OwnerId, out var owner) ? owner.FullName : "Unknown",
                    IsSharedWithMe: request.Scope == ResourceScope.Shared,
                    IsSharedByMe: sharesByCeId.ContainsKey(c.Id),
                    SharedWithUsers: sharesByCeId.TryGetValue(c.Id, out var shares) ? shares : [],
                    CurrencyCode: projectCurrency?.Code,
                    CurrencySymbol: projectCurrency?.Symbol
                ))
                .ToList();
        }
    }
}


