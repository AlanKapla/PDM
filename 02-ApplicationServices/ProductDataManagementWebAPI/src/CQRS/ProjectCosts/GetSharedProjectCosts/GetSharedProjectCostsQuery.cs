using Business.Interfaces.WebModels.ProjectCosts;

namespace CQRS.ProjectCosts.GetSharedProjectCosts
{
    /// <summary>
    /// Query do pobierania listy kosztów udostępnionych zalogowanemu użytkownikowi
    /// </summary>
    public record GetSharedProjectCostsQuery : IRequestQuery<IEnumerable<SharedProjectCostWeb>>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
    }
}
