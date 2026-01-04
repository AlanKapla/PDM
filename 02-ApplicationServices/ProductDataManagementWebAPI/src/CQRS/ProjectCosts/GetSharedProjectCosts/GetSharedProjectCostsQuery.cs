using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.Interfaces;

namespace CQRS.ProjectCosts.GetSharedProjectCosts
{
    /// <summary>
    /// Query do pobierania listy kosztów udostępnionych zalogowanemu użytkownikowi
    /// </summary>
    public sealed record GetSharedProjectCostsQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<IEnumerable<SharedProjectCostWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesReadShared;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
